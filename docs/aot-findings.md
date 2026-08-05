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

### 8. Assorted API surprises

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

## Retracted

### The persisted-dll path does *not* drop a map-of-map member

Recorded here as a silent-data-loss bug: a `Dictionary<string, Dictionary<string, string>>` member
was said to round-trip through `RuntimeTypeModel` while the compiled model emitted no code at all
for it. **It is not true**, and no issue should be raised for it.

Regenerating `MapNested.reference.cs` puts field 3 in both the read and the write, stably across
repeated runs. The original observation came from a **stale reference file**, and the git history
shows exactly how:

- `c4fe9fa3` committed a reference that already contained the `Maps` member while the *input* did
  not — the file was generated from a working tree that was ahead of what got committed;
- `895100f2` regenerated it, correctly dropping field 3, because at that point no such member existed;
- `0ef7af2c` added `Maps` to the input and **did not re-run `AotRefGen`**, so the file still showed
  no field 3 — which was then read as ref-emit refusing to emit it.

The lesson is about the harness, not about protobuf-net: `*.reference.cs` is only evidence if it was
generated from the input beside it. A reference that is *missing* something is the easy way to be
fooled, because an un-run generator and a generator that emitted nothing look identical. Regenerate
before concluding anything from an absence.

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
