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

### 7. Assorted API surprises

Not bugs exactly, but each cost time and each is a trap for callers:

- **A `[ProtoContract]` that resolves as a collection is serialized as one, and its own members are
  ignored entirely** — silently. `IgnoreListHandling = true` is the opt-out. Nothing warns.
- **A derived `[ProtoContract]` whose base does not `[ProtoInclude]` it is an independent contract
  that silently ignores every inherited member.** Also nothing warns.
- `IProducerConsumerCollection<T>` resolves to a provider that can be written but **not read** —
  deserialize throws, because there is no concrete type to construct.
- `RepeatedSerializer.CreateReadOnySet` is missing an "l" — public API, so presumably stuck.
- `AnalyzerReleases.Unshipped.md` has drifted: `PBN0020`–`PBN0022` are missing from it.

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

### B. The coverage sweep undercounts generics

`src/AotCoverage` reports "not seedable, generic: 19" — open generic definitions it cannot name with
a `typeof(...)` in the generated seed list. That was accurate when the generator refused all
generics, but closed constructions are supported now, so those 19 are unmeasured rather than
unsupported.

The tool could substitute a plausible argument (`int`, or the first type satisfying the constraints)
and seed the construction, giving a truer denominator. Care needed: constraint satisfaction is not
guaranteed, and a construction the sweep invents is not evidence that a *consumer* has one.

### C. `System.Type` members are deliberately not supported

Five contracts in the sweep have a `System.Type` member, which ref-emit serializes with
`SystemTypeSerializer`. Refused on purpose rather than not yet done: it round-trips assembly-qualified
names through `Type.GetType`, which is exactly the reflection AOT cannot do, so emitting it would
produce a serializer that compiles and then fails at runtime. The honest options are a diagnostic
(what happens today) or a surrogate the consumer supplies.

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
