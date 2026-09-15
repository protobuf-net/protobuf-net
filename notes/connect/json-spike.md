# Code-first JSON: the spike

A working end-to-end spike of the canonical protobuf JSON mapping emitted by `ProtoModelGenerator`,
built to answer "how messy does this get?" rather than to ship. It works; the mess is real but it is
in specific, nameable places rather than spread everywhere. This file records what was found, in the
order it was found, so the next person does not rediscover it.

Read `findings.md` §46 (the design) and §48 (the name rule) first; this is the sequel to both.

## What exists

| piece | where |
| --- | --- |
| the seam | `src/protobuf-net.Connect/IJsonSerializer.cs` — `IJsonSerializer<T>`, `IJsonModel` |
| the planning pass | `ProtoModelGenerator.ParseJson.cs` — what has a mapping, and the enum name tables |
| the emitter | `ProtoModelGenerator.EmitJson.cs` |
| plan additions | `ProtoMemberPlan.SchemaName`, `ProtoJsonEnumPlan`, `ProtoModelPlan.JsonContracts`/`JsonEnums` |
| the oracle | `src/ConnectJsonDifferential` — our JSON vs Google's `JsonFormatter`, 26 cases |
| native proof | `src/AotConnectJsonSmoke` — `PublishAot`, **0 new IL warnings**, runs |

Measured: 26/26 differential cases agree with Google.Protobuf; 552 BuildTools tests unchanged; the
binary corpus differential still reads 100% on 3090 contracts; no golden moved.

## The seam, and why it is not in Core

`IJsonSerializer<T>` lives in **protobuf-net.Connect**, not protobuf-net.Core, and the generator
**probes for it by metadata name** — `compilation.GetTypeByMetadataName("ProtoBuf.Connect.IJsonSerializer`1")` —
exactly as it already probes for `UnsafeAccessorAttribute` and `BclHelpers.ReadDateOnly`.

Two consequences, both wanted: `System.Text.Json` never reaches Core's dependency graph, and a model
in a project that has never heard of Connect emits no JSON half at all. The generated model picks up
`IJsonModel` as an added base interface on the consumer's `partial`, so the consumer names nothing.

Nothing about the emitted code is Connect-specific, so the seam can move to a better home later
without touching the emitter. It is where it is because that is the only assembly on this side that a
generated model already has a reason to reference.

## Presence: where the two codecs deliberately disagree

**JSON presence follows protojson, not the binary write guard.** The peer is the reason JSON exists,
so matching the peer beats matching our own other codec. Two differences follow:

- an **empty string or collection** is written in binary (the guard is `!= null`) and **omitted**
  here, because protobuf cannot distinguish empty from absent and canonical JSON omits;
- **`[DefaultValue]` is ignored** here. It is a protobuf-net write guard, not a schema default. A
  `[DefaultValue(5)]` member holding 0 is written in binary and omitted in JSON — which is lossy, and
  is the same lossiness `[DefaultValue]` already has across a round trip (PBN0020/PBN0021 exist to
  nag about it), so it is consistent rather than new.

## The thing the spike found that no amount of reading would have

**protobuf-net's code-first nullable has no representation in the schema protobuf-net itself
generates.** `public int? Maybe` emits

```proto
int32 Maybe = 43;
```

— *not* `optional int32`. So a peer generating from our `.proto` gets an implicit-presence field,
whose canonical JSON omits a zero, while our reader can tell null from zero and would like to keep
the distinction.

We write the zero. Every conformant reader accepts an explicitly-stated default, so it costs nothing
in interop and keeps null-vs-zero between two protobuf-net ends; omitting would be canonical and
lossy. It is pinned as a **known divergence** in the differential (`KnownDivergences`) rather than
hidden, because it is a real difference from what a canonical writer emits for the same schema.

The wider point, which is not about JSON: this is a place where `GetProto` is not faithful enough to
be an interop contract, and §12 predicted exactly that. It is worth a separate look at the schema
generator.

## The JSON surface is a *subset*, and needs its own cascade

Several shapes protobuf-net serializes perfectly well have **no canonical JSON at all** — not "not
yet", but nothing to emit:

| refused | why |
| --- | --- |
| `[ProtoInclude]` hierarchies | sub-type framing is a protobuf-net extension to protobuf |
| extensible contracts | retained unknown fields are raw bytes with no schema |
| null-wrapped members | a protobuf-net extension |
| `DateTime`/`TimeSpan` below level 240, `Guid`/`decimal` below 300 | at those levels they are protobuf-net messages, not well-known types |
| `DateOnly`/`TimeOnly` | a `BclHelpers` form with no counterpart |
| groups | a wire framing |
| a dictionary protobuf cannot express as a `map` | canonical JSON has a form only for a map |
| hand-written serializers, surrogates | the JSON form is not knowable here (surrogates are *unbuilt*, not impossible) |

...and that needs a **fixed-point cascade of its own**, parallel to `DropUnsatisfiable`: a contract
whose member type has no JSON serializer cannot have one either, or the emitted call names an
`IJsonSerializer<T>` the services type does not implement — CS-whatever in the consumer's build.

Each refusal reports **PBN3005**, at **Info** severity. The severity is deliberate and is the one
place this differs from PBN3001–3004: those leave a caller with a "no serializer for type" throw much
later, whereas this leaves `GetJsonSerializer<T>()` returning **null** — an answer the caller can
test. It is also information a binary-only consumer does not want, and every contract with
inheritance would produce one.

## Three bugs, all found by building rather than by thinking

**1. Collections that the reader cannot construct.** The reader builds a `List<T>`; assigning that to
a `HashSet<int>` or a `Queue<int>` member is CS0029 **in the consumer's project** — the worst failure
mode available, since it breaks a build that was working. Found by probing, not predicted: the first
version had a comment referring to a `JsonCollectionRefusal` that had never been written.

The fix is a **whitelist** on the declared type: array, `List<T>`, and the interfaces a `List<T>`
satisfies. Note the *factory* cannot decide this — `CreateEnumerable` serves `IEnumerable<T>` **and**
anything that matched nothing else, `class MySet : HashSet<int>` included, so trusting it re-admits
exactly what the whitelist excludes.

**2. Getter-only collections read into nothing.** A getter-only member routes through `Assign`, which
for `IsReadOnly` returns `expression;` — so the list was built and discarded, and for a map the
emitted `tmp32;` is not even a legal statement (CS0201). Now: appendable collections are **appended
to** (matching the binary path, and protojson's merge), and only the shapes with no `Add` — an array,
a read-only interface — are replaced wholesale. A getter-only member of *those* is refused, since
there is nowhere to put what was read.

**3. Fractional seconds, wrong in both directions at once.** `Timestamp` and `Duration` share one
rule: 0, 3, 6 or 9 digits, the fewest that represent the value exactly. The first cut had no 6-digit
case, so `0.123456s` went out as `0.123456000s` — disagreeing with every other implementation's text.
The timestamp half was worse: it used `ffffff`, which **truncates**, and .NET's 100ns tick needs
*seven* digits, so only the nine-digit form can carry a tick-precision value at all. Both are now one
`JsonFraction` helper, with cases pinning microseconds and ticks separately.

## The oracle is the load-bearing part

`src/ConnectJsonDifferential` compares three ways per case, and the second and third are the ones
that matter:

1. our bytes vs `JsonFormatter.Default.Format` over protoc's C#, compared as **parsed trees** (key
   order and whitespace are not part of the mapping);
2. our **reader** over *Google's* JSON, re-emitted and compared — catches a writer and reader that
   agree with each other about a spelling nobody else uses;
3. `JsonParser` over **our** JSON — proves our output is not merely equivalent but actually parses.

`Grpc.Tools` supplies protoc at build time, so there is no external dependency; and
`SchemaMatchesContracts` re-derives `Serializer.GetProto<Shapes>()` on every run and fails on drift,
which is what stops the comparison quietly becoming a comparison against a different schema. It has
already fired twice during this spike, both times correctly.

**The gate was verified able to fail**, not merely observed to pass. Injecting the §48 trap —
`char.ToLowerInvariant(name[0]) + name[1..]` — failed in all three directions. Worth seeing the
output, because it shows why the trap is so easy to miss:

```
ours:   {"bigNumber":"...","count":42,...,"pinnedName":"pinned!"}
google: {"BigNumber":"...","Count":42,...,"pinnedName":"pinned!"}
```

The **pinned** field is the one that stayed correct — `pinned_name` → `pinnedName` either way. A test
suite whose fields are all snake_case, which is every `.proto`-shaped fixture anyone would reach for,
sees nothing wrong. Injecting int64-as-number was also caught, and its third check showed the actual
harm: `18446744073709551615` came back as `1.8446744073709552E+19`.

## Native AOT

`src/AotConnectJsonSmoke` publishes and runs — 3.0 MB, **20 IL warnings, none of them ours**. Every
one names `TypeModel`, `DynamicStub` or `TypeHelper`, i.e. the pre-existing runtime-model fallbacks
`AotSmoke` already reports; **zero** name generated JSON code or `System.Text.Json`. The surface is
generated code over `Utf8JsonWriter`/`Utf8JsonReader` with no reflection anywhere. It deliberately
does **not** reference protobuf-net (only Core), so there is no reflective path to fall back to even
by accident.

**The first version of this smoke reported zero warnings, and that number was worthless**: it only
called the JSON serializer, so ILC trimmed the binary codec entirely and the count measured an app
that never used it. Exercising *both* codecs is what makes "none of them ours" mean something — the
same trap AGENTS.md records for maps ("whatever it does not cover is not fine, it is unmeasured").

## What is not done

- **`[ProtoPartialMember(Name = ...)]`** is still discarded; only `[ProtoMember(Name = ...)]` is
  captured. Same one-line shape, not yet threaded.
- **`[DataMember(Name = ...)]`** likewise — worth checking what `MetaType` does with it first.
- **Surrogates** are refused rather than mapped. The members on the plan are the *surrogate's*, so
  the enum-table lookup needs the surrogate's symbol; that is the whole of the work.
- **Auto-tuples** are untested; a tuple's JSON read would need the construct-at-end shape.
- **`Any`, `Struct`, `FieldMask`, the wrapper types** — deliberately absent. A code-first contract has
  none of them, which is the single largest simplification against implementing protojson wholesale.
- **Unknown fields are ignored**, where Google's parser rejects by default. A deliberate choice (a
  peer adding a field should not break us), worth revisiting if a conformance mode ever needs strict.
- **Nothing is measured for throughput.** There is no transcode here — unlike the contract-first path,
  which goes through `string` — so it should be the faster of the two, but that is untested.

## The codec (done)

`JsonConnectCodec` serves `application/json` from an `IJsonModel`, and `ProtoConnectGenerator`
registers it alongside the proto one. Two details worth keeping:

- **The registration is a runtime type test**, `Instance is IJsonModel`, not a compile-time one. It
  has to be: `IJsonModel` is put onto the model by `ProtoModelGenerator`, and **no generator sees
  another's output** — so the Connect generator cannot tell whether the model ended up with a JSON
  half. One branch at startup, no reflection, as AOT-safe as naming the type would have been.
- **The emit is gated on probing for `ProtoBuf.Connect.JsonConnectCodec`** in the consumer's
  compilation. BuildTools and protobuf-net.Connect are separate packages that version independently,
  so emitting the registration unconditionally is a build break in the project of anyone with a newer
  one and an older other. The golden fixture caught this immediately — its stubbed compilation has
  the older shape, and `Basic.output.txt` appeared carrying two CS0234s.

`Measure` returns `null`, so JSON states no `Content-Length` and every enveloped message is buffered
— the same cost the contract-first JSON codec pays, and a real difference from binary.

`AotConnectSmoke` gained two checks (now 25, native, 33 IL warnings — its existing baseline, none
naming JSON): a **hand-written** raw JSON POST getting hand-readable JSON back, and a typed client
negotiating JSON for both a unary and a server-streaming call. The first is the one that matters — it
asserts the wire rather than our writer agreeing with our reader.

Its "the codec is registered once" check had to become "**each** codec is registered once": it
counted codecs, and JSON arriving read as a regression. It counts distinct names now, which is what
it always meant.
