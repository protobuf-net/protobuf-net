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
- ~~**Nothing is measured for throughput.**~~ Measured: findings §56. The prediction held and then
  some - code-first JSON writes **2.6x faster with 11x less garbage** than the contract-first path,
  and reads 2.2x faster with 5.7x less. Against binary it costs 1.43x to write and 1.81x to read.

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

## The breadth sweep

`src/ConnectJsonDifferential` grew a second half: `Wide`, a fixture carrying **every scalar kind in
every container**, driven by 80 cases.

**Breadth beats a corpus here, and that is the opposite of what the binary path concluded.** Binary's
risk lives in what people actually write — odd attribute combinations, generated DTOs, shapes nobody
would choose — so a 3090-contract sweep of real code is the right instrument. The JSON mapping's risk
is different in kind: it is a *specified transformation*, so its surface is the cross-product of
scalar kind and container, and real code covers that cross-product sparsely and by accident. Walking
it deliberately covers more of what can be wrong, with far less machinery.

### The pairing is through the binary codec

`Cases` builds two instances by hand. That does not scale, and it cannot check anything protobuf-net
*derives* — a level-300 `Guid` is a `string` in the schema, so a hand-written oracle holds whatever
the author typed and confirms our JSON against a guess.

So the sweep serializes the protobuf-net instance to **binary protobuf** and parses it with protoc's
parser. The oracle then holds exactly what protobuf-net thinks the message contains, including every
derived string form, at the cost of one filler instead of two. It leans on the binary codec, which is
fair: that one is differentially verified against ref-emit across 3090 contracts.

### Five bugs, none of which the narrow fixture could have found

1. **Map key and value BCL types were refused for being "level 0".** `MemberJsonRefusal` called
   `JsonKindRefusal` without passing the member for a map, so the kind tests read `CompatibilityLevel`
   off a `default(ProtoMemberPlan)`. Every `Dictionary<string, TimeSpan>` was refused whatever level it
   was actually reached at.
2. **`Guid.ToString(IFormatProvider)` does not exist** — only `()`, `(string)` and
   `(string, IFormatProvider)`. Pairing Guid with decimal was CS1503 in the consumer's build.
3. **`Guid.Empty` is `""`, not `"00000000-0000-0000-0000-000000000000"`.** `GuidHelper.Write`'s first
   branch writes an *empty payload* for it, so that is what the schema's `string` carries and what
   every peer sees; a plain `ToString()` disagreed with our own binary codec about the same instance.
   The reader had to learn `""` → `Guid.Empty` too, which it was throwing on.
4. **`DateTime` must be written unconditionally**, matching the binary path — zero is a legitimate
   date, so protobuf-net always puts the `Timestamp` message on the wire and a peer sees the field as
   *present*. Guarding on `!= default` omitted it and disagreed with our own binary output.
5. **`ParseJsonEnumName_` was referenced and never emitted.** An enum-keyed map produced code the
   consumer's build rejects — the same class as the `HashSet` bug, invisible until the sweep tried to
   add the cell. (It is now deleted rather than fixed; see below.)

### ...and a second protobuf-net schema bug

Adding that enum-key cell turned up something better than a JSON bug. **protobuf-net believes an enum
is a valid map key** — `IsValidProtobufMap` accepts one, and `GetProto` duly emits
`map<Shade,int32>`. protoc rejects that outright:

```
Key in map fields cannot be enum types.
```

The spec allows any integral or string type and nothing else. So protobuf-net generates a schema no
protobuf tool will compile. That is a schema-generator bug in its own right, unrelated to JSON, and it
is the second one this spike has turned up — after `int?` emitting `int32` rather than `optional
int32`. Both say the same thing: **`GetProto` has never had to be an interop contract, and it shows.**

The JSON pass now refuses an enum map key, and the emitter's enum-key branches are **deleted rather
than left unreachable** — an `Enum.ToString()` fallback is *accidentally plausible* (it produces the
member name), which is exactly the kind of quietly-wrong output that survives a self-test.

Also measured rather than assumed, and guessed wrong first: **`bool` is not a legal protobuf map key**
either, alongside float, double and bytes. protobuf-net models those as a `repeated KeyValuePair_...`,
which has no `map` form and so no canonical JSON.

### Two harness lessons

**Refusals mask each other.** A contract bails at its first bad member, so one holds-everything
`Awkward` type reported one reason and silently hid three others — including the enum-key one. Probe
is now **one refusal reason per contract**.

**Numbers had to be compared numerically.** `Utf8JsonWriter` emits `5E-324` where Google emits
`5e-324`; JSON does not distinguish them, and comparing raw text would pin an exponent's
capitalisation as if it were the protocol.

`Boundary` now asserts the subset line in **both** directions — each refused shape has no JSON
serializer, each supported shape does, and every one of them still has its **binary** serializer,
since narrowing the JSON surface must never narrow the other.

### Where it stands

```
26 paired + 80 breadth cases agree with Google.Protobuf; the subset boundary holds
```

Gate re-verified able to fail after widening, by emitting a `char` as a character rather than a
number — caught in all three directions, including Google refusing to parse it.

## `IsValidProtobufMap`: verified against plain protoc, and it cuts both ways

The enum finding holds, and it is the **only** one. Every key type protobuf-net supports, run through
plain `protoc 35.1` (`grpc.tools` 2.83.0) on the schema `GetProto` actually emits:

| C# key | protobuf-net emits | protoc |
| --- | --- | --- |
| `int32`/`int64`/`uint32`/`uint64`/`byte`/`sbyte`/`short`/`ushort`/`string` | `map<…>` | OK |
| `bool`, `char`, `nint`, `nuint` | `repeated KeyValuePair_…` | OK |
| `float`, `double`, `DateTime`, `Guid`(L200) | `repeated KeyValuePair_…` | OK |
| **`enum`** | **`map<Shade,int32>`** | **"Key in map fields cannot be enum types."** |

And the spec side, tested directly by writing `map<K, int32>` by hand for each candidate: every
integral type (including the `sint`/`fixed`/`sfixed` variants), **`bool`** and `string` are accepted;
`float`, `double`, `bytes`, message **and enum** are refused.

So `IsValidKey` is wrong in **both directions**:

- **too lax**: `if (type.IsEnum) return true;` produces a schema no protobuf tool will compile;
- **too strict**: `TypeCode.Boolean` is absent from its switch, and so are `char`, `nint` and `nuint`
  — all of which have perfectly legal map-key schema types (`bool`, `uint32`, `int64`, `uint64`).

**I reported the bool half backwards earlier** — I inferred "bool is not a legal map key" from
protobuf-net emitting a repeated pair for it, which is protobuf-net's limitation and not the spec's.
protoc accepts `map<bool, V>`.

## What consumes it, and what changing it would cost

Three call sites, and it is *nearly* schema-only:

| site | what it decides |
| --- | --- |
| `RuntimeTypeModel.CascadeRepeated` | whether to also emit the `KeyValuePair_K_V` message — **schema only** |
| `MetaType` ~line 1993 | `map<K,V>` versus `repeated KeyValuePair_K_V` — **schema only** |
| `MetaType.ApplyDefaultBehaviour` ~line 1320 | sets `vm.IsMap`, which becomes `OptionFailOnDuplicateKey`'s *absence* |

That third one is the only behavioural reach, and it is narrower than it looks. **The bytes are
identical either way** — measured, not reasoned: a `Dictionary<int,int>` with and without
`[ProtoMap(DisableMap = true)]` (which toggles exactly this flag) both serialize to
`0A040801100A0A0408021014`. That is the protobuf spec working as intended, since a `map` *is* a
repeated message of `{key = 1, value = 2}`.

What the flag actually changes is **duplicate-key handling on read**, in `MapSerializer`:

```
as map  -> 99          (SetValues: last wins)
as list -> ArgumentException: An item with the same key has already been added. Key: 1
```

So a fix is a schema change plus a duplicate-key behaviour change, and **not a wire change**:

- **dropping the enum case** takes a `Dictionary<SomeEnum, V>` from an uncompilable schema to a
  compilable one, and from last-wins to throw-on-duplicate. Nobody doing cross-language interop can
  be relying on today's behaviour, because today's schema does not compile.
- **adding `bool`/`char`/`nint`/`nuint`** takes them from `repeated KeyValuePair_…` to `map<…>`, and
  from throw-on-duplicate to last-wins. Strictly more forgiving, and it removes a synthetic message
  from the schema — but it *is* a visible schema change for users whose peers have generated code
  from the current one. Safe at the wire, since the bytes do not move.

Both are protobuf-net library changes rather than Connect ones, so they are not on this branch.
