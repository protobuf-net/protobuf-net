# Findings from the AOT generator work

Things turned up while building `ProtoModelGenerator` that are **about protobuf-net itself**, not
about the generator. Kept here so they can become issues rather than being lost in commit messages.

Each was found by deriving the generator's expected output from ref-emit (`src/AotRefGen`) or by the
native-AOT smoke test (`src/AotSmoke`) — i.e. by comparison, not by reading the code and guessing.

## Open

### 1. Merging incompatible sibling sub-types overflows the stack

**Severity: high** — it is an unrecoverable process kill, reachable from untrusted input.

A payload carrying the same field twice with two *different* sub-type markers (a `Dog` then a `Cat`
under a common `[ProtoInclude]` base) sends `SubTypeState<T>.Cast` into `Merge`, which calls
`Model.Serialize<object>` / `Deserialize<T>` and recurses without bound. `StackOverflowException`
cannot be caught, so the process dies.

Reproduces with `RuntimeTypeModel` alone — no generated code involved:

```
Dog then Dog:   ok -> Dog
Dog then Puppy: ok -> Puppy
Puppy then Dog: ok -> Puppy
Dog then Cat:   *** exit -1073741571 (0xC00000FD, stack overflow)
```

Only incompatible siblings do it; same-branch merges are fine in either direction. Anyone
deserializing untrusted protobuf into a type with `[ProtoInclude]` is exposed. `Inherit.input.cs`
keeps its `Holder` samples on one branch because of this.

### 2. `Extensible.AppendValue` silently does nothing under AOT

**Severity: medium** — silent data loss, on an API whose whole purpose is round-trip fidelity.

`ExtensibleUtil.AppendExtendValue` serializes via `model.TrySerializeAuxiliaryType(..., type: null,
...)` — the reflective auxiliary path — and **discards the `bool` result**. Once trimmed, the value
is never stored and nothing is reported; a later `GetValue` returns the default.

Note the serializer's own extension path (`state.AppendExtensionData`) is fine: it only copies raw
bytes. It is just this "poke a value in by hand" convenience that is reflective.

At minimum the discarded `Try...` result should be checked and throw. `AotSmoke` works around it by
manufacturing an unknown field with a wider contract instead.

### 3. `SubTypeState<T>.Cast` → `Merge` keeps one trim warning alive

`IL2091`: `Merge` calls `TypeModel.Deserialize<T>`, which wants `DynamicAccess.ContractType` on `T`.
Fixing it means annotating the **class**'s `T`, which pushes the requirement onto every consumer of
`SubTypeState<T>` — including the generated path, which never reflects. Left alone deliberately;
it needs the same restructuring as the other reflective fallbacks (see AGENTS.md).

### 4. The level-200 `Guid` path costs four AOT warnings

`BclHelpers.WriteGuid`/`ReadGuid` (the `bcl.proto` form, i.e. compatibility level 200) go through
`state.WriteMessage<Guid>(..., SerializerCache<PrimaryTypeProvider>.InstanceField)` and read
`PrimaryTypeProvider.s_guidOptimized`, both of which force that type's static constructor. ILC then
attributes its dependencies — including an `Enum.GetValues(Type)` — to `WriteGuid`/`ReadGuid`.

Warning-only: the round-trip is verified correct in `AotSmoke`. The level-240/300 forms
(`GuidString`, `GuidBytes`, `Timestamp`, `DecimalString`) do **not** pay this, since they go through
`GuidHelper` and direct writes instead.

The same `Enum.GetValues` dependency also reaches **generated code** through the null-wrapped enum
path (`ReadAny<TEnum?>`/`WriteAny<TEnum?>` inline into the generated `Read`/`Write`), so an
`IL3050` is reported against the generator's own output. Again warning-only, and again correct at
runtime.

Not an obvious fix — it is the same entanglement as the other reflective fallbacks, and `WriteGuid`
genuinely needs the `PrimaryTypeProvider` serializer. Verified by experiment, not assumed: patching
the `ThrowEnumException` enum formatting (the first suspect) changed nothing.

### 5. `Issue1232` tests fail on `main`

Four `StreamSerializer_NonRootStream` cases (`trySkipWritingWhenMeasuring: True`) fail. Pre-existing
and unrelated to this work — they fail with the AOT branch stashed.

### 6. `PBN0015` is a false positive on a surrogated type — and it is an **error**

**Fixed on this branch**, but worth calling out separately because it affects the *shipped analyzer*,
not the AOT work.

`DataContractAnalyzer.ConstructorMissing` fires when a `[ProtoContract]` has constructors but no
parameterless one, and `SkipConstructor` is not set. It did not consider `Surrogate`. But with a
surrogate protobuf-net constructs the **surrogate** and converts — the type itself never needs a
constructor, and surrogating an immutable type is precisely the canonical use.

Severity is `DiagnosticSeverity.Error`, so this **fails the build** for anyone doing that. Found by
adding an immutable surrogated type to a fixture and watching `AotConformanceTests` refuse to
compile. The fix is a new `DataContractContextFlags.HasSurrogate`, set from the attribute and
checked alongside `SkipConstructor`.

### 7. `[ProtoPartialMember(OverwriteList = ...)]` is silently ignored

**Severity: low** — one option on one attribute, but silent, and it changes read-merge behaviour.

`MetaType.NormalizeProtoMember`'s partial-member branch reads every option off `ppma` (the
`[ProtoPartialMember]` being examined) *except* two, which it reads off `attrib`:

```csharp
GetFieldBoolean(ref isPacked, ppma, "IsPacked");
GetFieldBoolean(ref overwriteList, attrib, "OverwriteList");   // <-- attrib, not ppma
GetDataFormat(ref dataFormat, ppma, "DataFormat");
GetFieldBoolean(ref asReferenceHasValue, attrib, "AsReferenceHasValue", false);   // <-- and here
```

`attrib` is the member's own `[ProtoMember]`, and that branch only runs when the member has **no**
`[ProtoMember]` that pinned a tag — so `attrib` is null every time it is read there and the option is
discarded. `IsPacked` and `DataFormat` on the same attribute work fine, which is what makes it read
as a copy-paste slip rather than a decision.

Consequences are confined to reading: `OverwriteList` selects replace-over-append for a collection or
a `byte[]`, so a caller who set it still gets append. The generator refuses the option there rather
than honouring it, since honouring it would disagree with ref-emit.

### 8. A `[ProtoMember]` private field is lost when the contract is read from *metadata*

**Resolved: a harness bug, and it was masking a real generator bug.** Kept in full because the
shape of the mistake is worth remembering, and because the resolution changed the sweep's numbers.

`MetadataImportOptions` defaults to **`Public`**, so private members of a *metadata reference* are
never imported: `GetMembers()` does not return them and nothing reports their absence. Both sweeps
built their compilations with the default, so every contract whose members are non-public emitted a
serializer with no members and still counted as "emitted". Both now set
`MetadataImportOptions.All`.

Two consequences, and the second is why this was worth chasing rather than caveating:

- **The corpus numbers were overstated.** Dropped went 87 → 93 once the members were visible, mostly
  as `member '…' is not public` — a legitimate refusal that had simply never been reached. Worse
  numbers, truer ones.
- **It was hiding a generator bug that does not compile.** With private members visible,
  `Examples.Issues.Issue295.Asset` produced `CS1503`: `ReadSubType` hoists the instance into a
  per-case local, because reading `value.Value` is what constructs it — but `Assign` hard-coded the
  literal `value` for the `[UnsafeAccessor]` target, which *there* is the `SubTypeState<T>` wrapper
  rather than the contract. Any non-public member inside a hierarchy broke the consumer's build.
  Nothing covered that combination; `InheritAccessor.input.cs` does now.

The original write-up follows, since the reasoning that led here is the useful part.

---

**Severity: high** — silent data loss, and the generated code compiles.

`Examples.ProtoWithFields` (a `[ProtoContract]` whose only members are two `[ProtoMember]`-attributed
**private fields**, each wrapped by a public property) produces a serializer whose `Write` body is
*empty*:

```csharp
void ISerializer<Examples.ProtoWithFields>.Write(ref ProtoWriter.State state, Examples.ProtoWithFields value)
{
    TypeModel.ThrowUnexpectedSubtype(value);
}
```

`RuntimeTypeModel` writes `08-01-12-02-73-31` for the same instance. Nothing is reported — the
members are *skipped*, not refused, which means they fall out at `if (fieldNumber is null) continue;`
rather than at any of the accessibility checks below it.

**It does not reproduce from source.** A fixture with byte-identical declarations emits both members
correctly, so this is specific to reading the contract from a *metadata* reference, which is how
`AotCoverage` and `AotDifferential` drive the generator and is **not** how a real consumer does. That
makes it plausibly a harness artefact rather than a generator bug — but it is not yet established
which, and the failure mode (an empty serializer, no diagnostic) is bad enough either way that it
should not be assumed benign.

*(It was the harness — `MetadataImportOptions`. `PBN_MEMBERS=<type>` on `src/AotDifferential`, added
to settle it, prints the members and attributes the generator actually sees for a contract; the
answer was two properties and no fields at all.)*

Reproduce with `PBN_DUMP='ISerializer<global::Examples.ProtoWithFields>.Write' dotnet run --project
src/AotDifferential`, which prints the generated source around any matching line.

### 9. `PBN0003` is a false positive on a generic hierarchy root — and it is an **error**

**Fixed.** The include numbering space is now split per closed construction, so two includes may
share a tag when they belong to different constructions, while a member-versus-include clash and two
includes on the *same* construction still report. Three tests pin those directions; note the includes
are walked before the members, so the two spaces cannot be maintained by snapshotting one into the
other — the first attempt did exactly that and broke the member case.

The fourth of this family, after `PBN0015` (surrogates) and `PBN0012` (interfaces, then generics).

A generic base declares its `[ProtoInclude]` list once, and that list is shared by every closed
construction — but each construction only ever matches the includes that actually derive from it. So

```csharp
[ProtoContract, ProtoInclude(1, typeof(ShipHolder)), ProtoInclude(1, typeof(CrateHolder))]
public class Holder<T> : Node { }
public class ShipHolder : Holder<Ship> { }
public class CrateHolder : Holder<Crate> { }
```

is unambiguous — `Holder<Ship>` sees exactly one sub-type at tag 1 — and ref-emit serializes it.
`PBN0003` counts tags across the whole list and reports a build **error**. `Examples/Issues/SO9408133.cs`
is the shape from a real report, so this is not contrived.

`GenericHierarchy.input.cs` no longer needs its `#pragma`.

### 10. Assorted API surprises

Not bugs exactly, but each cost time and each is a trap for callers:

- **A `[ProtoContract]` that resolves as a collection is serialized as one, and its own members are
  ignored entirely** — silently. `IgnoreListHandling = true` is the opt-out. Nothing warns.
- **A derived `[ProtoContract]` whose base does not `[ProtoInclude]` it is an independent contract
  that silently ignores every inherited member.** Also nothing warns.
- **An interface root writes its own declared members *in addition to* the implementation's**, so a
  property declared on both goes on the wire twice. `[ProtoContract] interface IAnimal` declaring
  `[ProtoMember(1)] Name`, with `Dog` implementing it and declaring `[ProtoMember(1)] Name` itself,
  serializes `Name` once inside the `Dog` sub-type layer and again in the `IAnimal` layer:
  `0A-0E · 52-07(0A-03 "rex" · 10-03) · 0A-03 "rex"`. Consistent with "a layer only sees its own
  declared members" — the interface property and the implementing property genuinely *are* different
  members — but it is not what anyone writing that contract intends, and nothing warns.
- `IProducerConsumerCollection<T>` resolves to a provider that can be written but **not read** —
  deserialize throws, because there is no concrete type to construct.
- `RepeatedSerializer.CreateReadOnySet` is missing an "l" — public API, so presumably stuck.

### 11. A compiled model throws on a map whose key or value is a collection

**Severity: medium** — the model compiles clean and fails on first use, so it is a deployment-time
failure rather than a build-time one. Confirmed on both halves of `Compile(name, path)`.

| shape | `RuntimeTypeModel` | persisted dll |
| --- | --- | --- |
| `Dictionary<string, Dictionary<string, string>>` | ok, 13 bytes | **compiles, then throws on use** |
| `Dictionary<string, List<int>>` | ok, 9 bytes | **compiles, then throws on use** |
| `Dictionary<List<int>, List<string>>` | ok, 12 bytes | **compiles, then throws on use** |

```
InvalidOperationException: No serializer for type
  System.Collections.Generic.Dictionary`2[System.String,System.String] is available for model X
```

The compiled serializer emits the member and passes `this as ISerializer<Dictionary<string,string>>`
for the value serializer. The generated services type does not implement that interface — it
implements `ISerializer<KeyValuePair<string, string>>` — so the cast yields null, resolution falls
back to the model, and the model has no entry for it.

**This entry supersedes two earlier wrong ones, and the way both went wrong is the point:**

- `0ef7af2c` recorded it as *silent data loss* — "the compiled model emits no code at all for it".
  That was read off a `MapNested.reference.cs` that had never been regenerated after the member was
  added to the fixture, so the absence was the harness, not ref-emit.
- The correction to that then claimed the persisted path **handles** the shape, on the strength of
  regenerating the reference and seeing field 3 appear in the read and the write. Also wrong, and
  wrong in a more instructive way: `AotRefGen` only *compiles and decompiles*, it never runs the
  model. Emitted code is not working code, and here the difference is exactly the bug.

So the rule that "a reference is only evidence if it was generated from the input beside it" is
necessary but not sufficient. `*.reference.cs` answers *what ref-emit emits*; it cannot answer
whether the result runs. For anything where the two could differ, run it.

For the generator this means: our support for a repeated or nested map **value** matches the
reflection path and exceeds the compiled one, and our refusal of a nested map **key** matches the
compiled path while falling short of the reflection one.

### 12. A `CategoryScalar` hand-written serializer is emitted as a sub-message

**Fixed.** `ProtoContractAttribute.IsScalar` was added as the metadata-readable escape hatch, the
`Features` declaration is folded from source where it is available, and the contract is refused with
advice where neither settles it. See "Hand-written serializers" in AGENTS.md; the original analysis
follows because the *reason* it could not simply be looked up is the durable part.

**Severity: high** — wrong framing on the wire, and the two known cases *throw* rather than
silently disagreeing, which is the only reason they were noticed.

`[ProtoContract(Serializer = typeof(X))]` says a type has a hand-written serializer. We assume every
such contract is a **message** and frame a member of it with `WriteMessage`/`ReadMessage`. But the
serializer declares its own category, and it may be a *scalar*:

```csharp
[ProtoContract(Serializer = typeof(Serializer))]
public readonly struct CustomType
{
    public class Serializer : ISerializer<CustomType>
    {
        public SerializerFeatures Features
            => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;
        ...
    }
}
```

Ref-emit frames that as the serializer asks — taken from a derived reference, not guessed:

```csharp
// scalar category                          // message category, for contrast
state.WriteFieldHeader(2, WireType.Variant);
SerializerCache.Get<S, T>().Write(ref state, v);   state.WriteMessage(1, CategoryRepeated, v, SerializerCache.Get<S2, T2>());
v = SerializerCache.Get<S, T>().Read(ref state, v); v = state.ReadMessage(CategoryRepeated, v, SerializerCache.Get<S2, T2>());
```

Two corpus contracts hit it — `ProtoBuf.Test.Issues.Issue598+Item` and
`ProtoBuf.Issues.Issue1083+WithWrapping` — and both throw *"Invalid wire-type"* on serialize, so the
damage is loud rather than silent. A scalar serializer whose wire type happened to be `String` would
disagree quietly instead.

**Why it is not fixed here.** The category lives in the serializer's `Features` property, which
ref-emit obtains by *instantiating* the serializer at model-build time. A source generator cannot.
The options are a design decision rather than a bug fix:

- **Read it from source.** When the serializer is in the same compilation its `Features` body is
  usually a constant expression (`CategoryScalar | WireTypeVarint`), and `GetConstantValue` folds it.
  That covers both corpus cases and any consumer writing their own — but not a serializer arriving
  through a metadata reference, which would still have to be refused.
- **Refuse whenever the category cannot be established.** Safe and honest, but it withdraws support
  from the message case that works today (`ExternalSerializer.input.cs`) whenever the serializer is
  not in source.

Either way the current behaviour — assume message, emit, and let it throw at runtime — is the one
option that should not survive.

### 13. The corpus differential's remaining disagreements

**This entry exists because `docs/aot-differential.md` is generated and overwritten on every run.**
It is a snapshot, not a backlog. Read the snapshot for current numbers; the *causes* here are the
open work.

**No byte mismatches remain** — all 1286 contracts compared serialize identically to
`RuntimeTypeModel`, and CI now fails if that regresses. The rest, audited rather than assumed:

- **6 where one model threw** (was 19). None are ours any more — item 12 closed the two, the
  `[ProtoReserved]` port six more, and the surrogate replay the NodaTime pair. What is left — the only cases where *we* throw
  and protobuf-net does not. The other 8 all go the same way, and all are contracts protobuf-net
  refuses to build a serializer for while we emit one:
  - ~~**6 × `[ProtoReserved]`**~~ — **fixed**: `ValidateReservations` is now ported, so these are
    refused with protobuf-net's own message rather than emitted.
  - **3 × ambient compatibility level on an auto-tuple** (`CompatibilityLevelAmbientAutoTupleTests`):
    *"the tuple-like type must use a single compatibility level"*.
  - **3 × invalid `[NullWrappedValue]`** (`NullWrappedValueTests+HazInvalid*`): packed, required and
    bad-`DataFormat` combinations that `ValueMember` throws on.
  - ~~**2 × NodaTime**~~ — **the harness, and the generator was right.** The suspicion was that
    `Instant`/`Duration` were falling through to the auto-tuple predicate, as `ArraySegment<byte>`
    had; they are not. We emit the correct surrogate delegation from `protobuf-net.NodaTime`'s
    assembly-level `[ProtoSurrogate]` declarations, and it was the *reference* that had never heard
    of them. `AotDifferential` now replays those declarations onto the reference model, and the pair
    compares and matches.
  - **3 × assorted** invalid shapes.

  These are all "we accept configuration protobuf-net rejects". Individually low-priority, since the
  contracts do not work in protobuf-net either — but the `[ProtoReserved]` group is a documented
  claim that turns out to be false, so it should not just sit here.
- **9 contracts no instance could be built for** — the `Filler` cannot box a `Span<byte>` or
  `ReadOnlySpan<byte>` member, and a few types have no construction route. *Unmeasured*, not
  known-good, which is why the report says "of the N actually compared".
- **2 the reference model refused outright**, both `[CompatibilityLevel(42)]`-style invalid fixtures.

`PBN_DUMP=<substring>` prints the generated source around a match; `PBN_MEMBERS=<type>` prints what
the generator sees for a contract. Between them they settle "ours or the harness's" fastest.

## Future ideas

Not defects — things worth doing that were scoped out, with the measurements that were taken at the
time so the next person does not have to retake them.

### A. A UTF-8 fast path for string-shaped members (`IUtf8SpanFormattable`)

Every string-shaped member goes through a `string`: parseable types do `ToString()` on the way out
and `Parse(string)` on the way back, and `WriteString` takes a `string`. For types implementing
`IUtf8SpanFormattable` the write could format straight into the output buffer, skipping the
intermediate `string` allocation entirely.

**This needs a library-side addition first.** `ProtoWriter.State` has no `WriteString`-equivalent
taking UTF-8 bytes, so there is nothing for the generator to call. That is the blocking item, not
the generator work — which is easy, because a source generator can test the interface at *compile*
time and pick per-type, where ref-emit would need a runtime check.

The two halves are **not symmetric**, which is the thing to know before designing around this.
Probed against the actual ref assemblies rather than recalled:

| | `IUtf8SpanFormattable` | `IUtf8SpanParsable<TSelf>` |
| --- | :-: | :-: |
| `IPAddress`, `Version`, `BigInteger`, `Guid`, `decimal`, `int`, `Half`, `Int128`, `UInt128` | yes | yes |
| `DateOnly`, `TimeOnly`, `TimeSpan`, `DateTime`, `DateTimeOffset` | yes | **no** |
| `IPEndPoint`, `EntityTagHeaderValue` | no | no |

- Both interfaces exist from **net8.0**, so the generator would probe for them exactly as it does for
  `UnsafeAccessorAttribute` and `CreateReadOnySet`.
- Implementations **move between versions**: `IPAddress` implements only the formattable half on
  net8.0 and both by net10. So this must be decided from the compilation's own reference set, never
  from a hard-coded list.
- The read side is the weak half — the date/time family formats to UTF-8 but does not parse from it —
  so a design gated on "implements both" would cover almost nothing. Format-via-UTF-8,
  parse-via-`string` is the realistic shape.
- **The utf8 pair is not a superset of parseable**, so it cannot be used as the gate on its own:
  `Complex` and `Rune` implement both interfaces but have no `static Parse(string)`, and accepting
  them would *widen* the set of types protobuf-net serializes — a wire-compat change, not an
  optimisation.

Worth noting the win is allocation, not correctness: the wire bytes are identical either way, so this
is measurable rather than observable, and the differential suite would pass unchanged.

### A2. The 36 native-AOT warnings, grouped

Measured from `AotSmoke` (`dotnet publish -c Release -r win-x64`), deduplicated. Recorded so the
warnings pass can start from data rather than re-measuring, and because the shape of the list is the
argument for *restructuring* over annotating:

| count | id | where |
| ---: | --- | --- |
| 14 | IL3050 | `MakeGenericType`/`MakeArrayType`/`Enum.GetValues` on reflective paths |
| 11 | IL2091 | `TypeHelper<T>` and the `RepeatedSerializer` chain |
| 5 | IL2067 | `DynamicStub.TryCreateConcrete`, `TypeHelper.CreateNonTrivialDefault` |
| 3 | IL2070 | `Helpers.GetConstructor`, `DynamicStub.ResolveProxies`, `ResolveUniqueEnumerableT` |
| 3 | IL2057 / IL2087 / IL2055 | one each, same paths |

Four distinct sources account for nearly all of it:

- **`DynamicStub.SlowGet`** — `MakeGenericType` to build a concrete stub. The reflective fallback.
- **`TypeHelper.ResolveUniqueEnumerableT`** — the old is-it-a-list heuristic, `[Obsolete]` but still
  called by `TypeModel.CanSerialize` and the auxiliary flow.
- **`BclHelpers.WriteGuid`/`ReadGuid`** — the level-200 `Guid` path; see item 4.
- **`TypeHelper<T>`'s static constructor** — reaches the reflective factory.

None of these is reachable from a *generated* model doing ordinary work; they are entangled inside
the shared generic paths, which is exactly why `Requires*` annotations were tried and reverted. The
fix is to make the generic path not reference the fallback at all.

### B. The coverage sweep undercounts generics

`src/AotCoverage` reports "not seedable, generic: 19" — open generic definitions it cannot name with
a `typeof(...)` in the generated seed list. That was accurate when the generator refused all
generics, but closed constructions are supported now, so those 19 are unmeasured rather than
unsupported.

The tool could substitute a plausible argument (`int`, or the first type satisfying the constraints)
and seed the construction, giving a truer denominator. Care needed: constraint satisfaction is not
guaranteed, and a construction the sweep invents is not evidence that a *consumer* has one.

### C. `System.Type` members are deliberately not supported

Seven contracts in the sweep have a `System.Type` member, which ref-emit serializes with
`SystemTypeSerializer`. Refused on purpose rather than not yet done: it round-trips assembly-qualified
names through `Type.GetType`, which is exactly the reflection AOT cannot do, so emitting it would
produce a serializer that compiles and then fails at runtime. The honest options are a diagnostic
(what happens today) or a surrogate the consumer supplies.

The diagnostic now **says so**, and so does the one for a parseable type, so the sweep's
"unsupported type" row splits three ways rather than reading as one undifferentiated backlog:
7 `System.Type`, 4 parseable-but-not-opted-in, 13 genuinely unclassified.

Same reasoning applies to `System.IO.Stream` (1), which is not serializable in any case.

### D. Generated accessor names could collide in principle

`AccessorName` builds an identifier by replacing every non-alphanumeric character in the contract's
full name with `_`. Distinct types can therefore sanitise to the same identifier — `Ns.A<B>` and
`Ns.A_B_` both give `Ns_A_B_`. It needs two such types in one model, both needing `[UnsafeAccessor]`,
so it has not been hit; a uniqueness pass (suffix on clash) would close it cheaply.

Worth doing before generics see real use, since closed constructions make contrived-looking names
much more likely.

### E. Nested and generic *model* types are refused

`ProtoModelGenerator.Parse` bails on `model.ContainingType is not null || model.IsGenericType`, so
`[ProtoModel]` must be on a top-level non-generic class. Nested would be straightforward — the emit
needs to reopen the enclosing types as `partial`. A generic model is a different question and
probably not worth it: the services type is what makes the closed-world guarantee work.

## Fixed on this branch

- **`PBN0012` reported a build error for a *generic* hierarchy root** — the same descriptor as the
  interface bug below, and found the same way. The attribute lives on the open `IFoo<T>` while the
  sub-type implements the closed `IFoo<int>`, so `SymbolEqualityComparer` finds no link and the
  check reports an **error** for a shape ref-emit resolves on both its paths. It now compares
  `OriginalDefinition` as well, with tests for a generic interface root, a generic base class, and a
  genuinely unrelated generic — so the relaxation is not blanket.

- **`PBN0012` reported a build error for every interface hierarchy.** "The type '{0}' is declared as
  an include, but is not a direct sub-type" compared `include.Type.BaseType` against the contract, so
  an interface root — where the link is *implementing*, not deriving — could never satisfy it. The
  severity is `Error`, so `[ProtoContract] interface IAnimal` with `[ProtoInclude(10, typeof(Dog))]`
  failed the build despite round-tripping correctly at runtime. Same class of bug as the `PBN0015`
  false positive on surrogated types, and found the same way: by adding the pattern to a fixture and
  watching the build refuse it. The check now also accepts an implemented interface, and there is a
  test for each direction, so a genuinely unrelated include still errors.

- **`SerializerCache.Get<TProvider, T>` had no trim annotations**, while the
  `SerializerCache<TProvider>` it forwards to needs `DynamicAccess.Serializer` to preserve the
  constructor `Activator.CreateInstance` uses. ILC trimmed it and the first serialize threw
  `MissingMethodException`. This affected **any** hand-written `TypeModel` under AOT, not just
  generated ones.
- **`TypeHelper<T>.ValueChecker` reached `StructValueChecker<TStruct>` through `MakeGenericType`**,
  so ILC never generated it: the first serialize of any **struct contract member** threw *"missing
  native code or metadata"*, and later the same for any **nullable enum or struct** reached through
  the null-wrapping paths. The reflective lookup is now gone entirely, replaced by two unconstrained
  checkers `TypeHelper<T>` can name statically — `NonNullValueChecker<T>` (a plain value type is
  always present) and `NullableValueChecker<T>` (both answers are `HasValue`; the `is null` test
  looks like it boxes, but box-of-nullable followed by a null comparison is a JIT peephole).
  Anything reaching those branches is a value type, because `IValueChecker<in T>` is contravariant
  and so a reference type is already taken by `ReferenceValueChecker`.
- **`[DynamicallyAccessedMembers]` was declared on `ISerializer<T>` and its siblings**, so every
  consumer paid the reflection-based model's cost — including generated models, which never reflect.
  `PrimaryTypeProvider` implements `ISerializer<Type>`, and `System.Type` is saturated with
  `RequiresDynamicCode` members, so that one instantiation produced ~180 warnings. Moving the
  annotations to the reflection entry points took `AotSmoke` from **200 warnings to 33**.
- `TypeModel.GetSubTypeSerializer<T>` and `SubTypeState<T>.ReadSubType<TSubType>` were missing
  `DynamicAccess.ContractType`; both now terminate at the generated call sites, which pass concrete
  types.
