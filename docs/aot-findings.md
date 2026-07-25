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

### 6. Assorted API surprises

Not bugs exactly, but each cost time and each is a trap for callers:

- **A `[ProtoContract]` that resolves as a collection is serialized as one, and its own members are
  ignored entirely** — silently. `IgnoreListHandling = true` is the opt-out. Nothing warns.
- **A derived `[ProtoContract]` whose base does not `[ProtoInclude]` it is an independent contract
  that silently ignores every inherited member.** Also nothing warns.
- `IProducerConsumerCollection<T>` resolves to a provider that can be written but **not read** —
  deserialize throws, because there is no concrete type to construct.
- `RepeatedSerializer.CreateReadOnySet` is missing an "l" — public API, so presumably stuck.
- `AnalyzerReleases.Unshipped.md` has drifted: `PBN0020`–`PBN0022` are missing from it.

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
