# External serializers and cross-cutting DataFormat defaults for the AOT generator

**Date:** 2026-08-12
**Status:** Approved design, pre-implementation
**Scope:** Serialization only. The `[ServiceContract]`/`[OperationContract]` layer is owned by the
consumer's own tooling (or protobuf-net.Grpc, a different repo) and is out of scope.

This spec is fork-local working material; it does not travel with the upstream PRs.

## Motivation

A consumer keeps WCF-style declarations — `[DataContract]`, `[DataMember(Order = N)]` — in its
contracts assemblies and wants the AOT source generator that shipped with 3.3.8. The generator
already treats `[DataContract]`/`[DataMember(Order)]` as first-class (contract markers and
field-number sources, as seeds and as member types), and assembly-level
`[CompatibilityLevel(Level300)]` already resolves identically at runtime and compile time. Two
gaps remain, both established by external research
(NorseArchitecture/Glitnir, `docs/Midgard/specs/2026-08-12-protobuf-net-aot-upstream-serializer-dataformat-brainstorm.md`):

1. **No external hand-written serializer declaration.** `Serializer = typeof(X)` exists only as a
   `[ProtoContract]` argument on the type itself. A type that cannot carry it — because the
   serializer lives in an assembly that references the type's assembly, so the reverse reference
   would be circular — has no compile-time route. The motivating case is a scalar-union
   `readonly record struct Result<T>` whose wire form is the underlying scalar's native
   representation and whose conversion functions throw on default instances, which rules out the
   surrogate path twice over: `[ProtoSurrogate]` produces a *sub-message* wire shape, and the
   generated surrogate read primes the merge target and calls `ToSurrogate(default)`. The runtime
   half already exists as `MetaType.SerializerType`; only the declarative half is missing.

2. **No cross-cutting `DataFormat` default.** `DataFormat.FixedSize` is per-`[ProtoMember]` only,
   so a `[DataMember]` Guid at compatibility level 300 always serializes as `GuidString`, never
   the 16-byte `GuidBytes` — and there is no way to state the preference once per assembly, the
   way `CompatibilityLevelAttribute` does for levels.

## Decisions taken (with the consumer)

- **Scope:** serialization only.
- **Runtime parity, split:** the DataFormat default is honored by *both* the reflection-based
  runtime model and the generator (its precedent, `CompatibilityLevelAttribute`, is honored by
  both; a generator-only format default would let a JIT and an AOT service silently disagree on
  the wire). The external-serializer declaration is **generator-only**, mirroring
  `[ProtoSurrogate]` — runtime users already have `MetaType.SerializerType`.
- **Open-generic mapping supported:** one declaration covers every closed instantiation the model
  meets. A per-instantiation-only design reproduces the consumer's known failure mode (a missing
  registration line surfacing only at runtime, on one transport).
- **General per-type mechanism** for the format default, not a Guid-only special case: honored
  wherever an explicit per-member format would be honored, with identical semantics.
- **Done means:** repo-convention coverage (golden, differential, native AOT smoke) with
  Norse-shaped fixtures; the consumer validates against their own platform afterward.

## Proposal 1: `ProtoSerializerAttribute`

### API (protobuf-net.Core, namespace `ProtoBuf`)

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
[Experimental(ProtoModelAttribute.DiagnosticId)]   // PBN9001, like ProtoModel/ProtoSurrogate
public sealed class ProtoSerializerAttribute : Attribute
{
    public ProtoSerializerAttribute(Type type, Type serializer);
    public Type Type { get; }          // the type being serialized, e.g. typeof(Result<>)
    public Type Serializer { get; }    // the hand-written serializer, e.g. typeof(ResultSerializer<>)
    public bool IsScalar { get; set; } // explicit category, for serializers reached via metadata
}
```

Usage, in the assembly that ships the serializers (not the contracts assembly, not the domain
assembly):

```csharp
[assembly: ProtoSerializer(typeof(Result<>), typeof(ResultSerializer<>), IsScalar = true)]
```

### Semantics

- **Generator-only**, documented in xml-doc the same way `[ProtoSurrogate]` documents it. The
  runtime equivalent is `MetaType.SerializerType`.
- **Open-generic rule:** `Type` and `Serializer` are both closed or both open with the same arity.
  An open pair is closed at each use site with the use site's type arguments. A closed declaration
  beats the open mapping for the same closed type.
  - **Stated v1 scope boundary:** closing the mapping (`INamedTypeSymbol.Construct`) validates arity
    only, not the open serializer's own generic constraints. If a use-site type argument violates a
    constraint the serializer declares on itself (e.g. `where T : class`), every downstream check
    still passes — `Construct` succeeds and substitutes cleanly — and the emitted
    `SerializerCache.Get<ClosedSerializer, T>()` then fails to *compile* in the consumer's build.
    Full constraint validation (checking `HasReferenceTypeConstraint` /
    `HasValueTypeConstraint` / `HasConstructorConstraint` / `ConstraintTypes` per type parameter
    against each use-site argument) is deliberately deferred as its own follow-up rather than folded
    into this pass; documented as a known limitation on `ProtoSerializerAttribute`'s xml-doc.
- **Scopes and precedence**, mirroring `[ProtoSurrogate]`: referenced assemblies → this assembly →
  the model; most specific wins. A type's own `[ProtoContract(Serializer = ...)]` beats any
  assembly-level declaration, but a model-level declaration beats even that — the model is the
  final authority on itself. (Carried to upstream review as an open question.)
- **Matching is by full name**, never symbol identity — repo doctrine; the test harnesses and the
  reflectively-loaded generator in AotDifferential cannot share symbol identity with consumers.
- **Validation** (each failure a warning-level diagnostic naming the fix, reported through the
  existing PBN2002/PBN2003 kinds with explanatory strings, matching every existing surrogate
  refusal; no new IDs, so `AnalyzerReleases.Unshipped.md` is untouched):
  - arity mismatch between `Type` and `Serializer`, or one open and one closed;
  - the closed serializer does not implement `ISerializer<T>` for the closed `T`;
  - the serializer is not a class, is inaccessible from the generated code, or has no
    parameterless constructor (the same demands `MetaType.SerializerType` makes at runtime);
  - two declarations for the same type at the same scope.

### Generator internals

- **Gathering** clones `GetSurrogates` (`ProtoModelGenerator.Parse.cs`): walk referenced
  assemblies' attributes, this assembly's, then the model's. Output: a closed map keyed by type
  full name plus an open map keyed by generic-definition full name.
- **Resolution:** wherever the parse currently honors `[ProtoContract(Serializer = ...)]`, consult
  the external maps per the precedence above; an open-map hit constructs the closed serializer
  symbol from the use site's type arguments before validation.
- **Emit reuses the existing hand-written-serializer path unchanged:** no body for the type; the
  services type implements `ISerializerProxy<T>` returning `SerializerCache.Get<ClosedSerializer, T>()`;
  members frame through the existing three-route category resolution — `IsScalar` on the attribute
  is route 1; unstated falls through to `Features` folding (source-visible serializers only) and
  then the `WriteAny`/`ReadAny` runtime deferral. Stated-versus-unstated is read off the
  attribute's named-argument list (attribute data records which named arguments were actually
  written), so `IsScalar = false` is an explicit message-category declaration, distinct from
  omitting it. No merge-priming exists on this path, so the
  `ToSurrogate(default)` hazard is structurally absent.
- **Existing refusals stand:** a scalar serializer as a collection element or map side remains
  refused (today's rule). The motivating usage is scalar-per-property, unary members only.
- **Nullable members are a named obligation:** `Nullable<TStruct>` where the struct has an
  external scalar serializer must round-trip (the consumer's cardinality convention makes
  `Result<T>?` members routine). Expected shape is the `ISerializerProxy<T?>` route the enum path
  already uses; probe against ref-emit rather than assume.

### Harness obligations

- `AotDifferential`, `DifferentialTests.CreateReference`, and `AotRefGen` replay declarations onto
  the reference model via `MetaType.SerializerType`, exactly as they replay `[ProtoSurrogate]`
  via `SetSurrogate`; open declarations are closed per contract met.

## Proposal 2: `ProtoDataFormatAttribute`

### API (protobuf-net.Core, namespace `ProtoBuf`)

```csharp
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module |
                AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
                AllowMultiple = true, Inherited = true)]
public sealed class ProtoDataFormatAttribute : Attribute
{
    public ProtoDataFormatAttribute(Type type, DataFormat dataFormat);
    public Type Type { get; }
    public DataFormat DataFormat { get; }
}
```

Usage, in a contracts assembly:

```csharp
[assembly: CompatibilityLevel(CompatibilityLevel.Level300)]   // works today
[assembly: ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
```

Not `[Experimental]`: it is a runtime feature in its own right and its precedent is not either.
(Carried to upstream review as an open question.)

### Semantics

- **Honored by both** the runtime model and the generator.
- **Resolution** for a member whose effective format is `DataFormat.Default`: declaring contract
  type (walking base types) → module → assembly → none. Explicit `[ProtoMember(DataFormat = ...)]`
  always wins. `DataFormat.Default` is the enum's zero, so "explicitly Default" and "unstated" are
  indistinguishable and both take the cross-cutting default — consistent with the enum's existing
  "no preference" meaning.
- **Type matching:** exact, plus `Nullable<X>` matches a declaration for `X`. For repeated
  members, the *element* type is matched — where an explicit member format already lands.
  **Maps are excluded in v1**: per-side formats belong to `[ProtoMap]`, a separate mechanism.
- Everywhere else, semantics are identical to the explicit per-member format: ignored where that
  is ignored (e.g. `FixedSize` on a level-200 Guid, any format on `decimal`), refused where that
  is refused (e.g. `ZigZag` where model building throws).

### Internals

- **Runtime:** a resolution helper beside `TypeCompatibilityHelper` — same walk, same per-scope
  caching. Applied in `MetaType.ApplyDefaultBehaviour` when a member's effective format is
  `Default`: look up the member's scalar type (after `Nullable<>` unwrap; element type for
  repeated members) and substitute the format before the `ValueMember` is built. Everything
  downstream (`GuidBytes` at level 300, fixed widths, `BclWireType` behaviors, ZigZag refusals) is
  existing machinery receiving a format it already understands. Attribute-driven only in v1 — no
  new imperative `RuntimeTypeModel` API.
- **Generator:** a second lookup in the already-ported CompatibilityLevel walk, feeding the
  existing `member.DataFormat` plumbing. Conversion goes through `GetDataFormat`, never a cast
  (`DataFormat` and the internal `ProtoDataFormat` enum do not share ordinals — a recorded trap).
- **Fixture constraint both halves inherit:** the differential/conformance fixtures link into a
  single assembly, so an assembly-scoped fixture attribute would silently re-format every fixture
  (the recorded `[module: CompatibilityLevel]` trap). Differential fixtures use **type-scoped**
  declarations; assembly/module scope is proven by isolated-compilation tests.

## Test plan

### Proposal 1

- **Golden fixtures** (`BuildToolsUnitTests/Aot/Data/`): `ModelSerializer.input.cs` — a
  `[DataContract]`/`[DataMember(Order)]` contract with a Norse-shaped
  `readonly record struct Wrapped<T>` member and a `Wrapped<T>?` member, served by a model-level
  open declaration; a closed declaration overriding the open mapping; `Diagnostics/` fixtures per
  refusal.
- **Differential:** samples on the fixture; reference model replay via `MetaType.SerializerType`;
  bytes and cross-deserialization against ref-emit.
- **Cross-assembly test** mirroring `ProtoSurrogateReferenceTests`: serializers + assembly-level
  declaration in one compilation, WCF-attributed contracts in a second, a model in a third that
  declares nothing.
- **`AotSmoke`:** a generic member resolved through the open mapping, natively published — ILC
  must generate the closed `SerializerCache<Provider, T>` instantiation and the
  `DynamicAccess.Serializer` chain must hold. Re-measure the warning/size baseline (the count
  tracks fixtures).

### Proposal 2

- **Runtime unit tests** (protobuf-net.Test): scope precedence (type > module > assembly),
  explicit member format winning, `Guid?` and `List<Guid>` matching, and a wire-literal assertion
  that a `[DataMember]` Guid at level 300 under the default goes out as tag + exactly 16 bytes.
- **Assembly-scope proof** via the `protobuf-net.TestCompatibilityLevel` precedent: a small
  isolated test assembly carrying the assembly-level attribute.
- **Golden + differential fixture** with a type-scoped declaration — the runtime honors the
  attribute too, so the differential asserts JIT/AOT wire parity directly, not via replay.
- **`AotSmoke`:** a level-300 `[DataMember]` Guid under a type-scoped default, payload checked as
  16 bytes.

### Error handling philosophy (both)

Every refusal is a warning-level diagnostic naming the fix, never a silent drop or an error — an
incomplete model still builds, and the runtime "no serializer" throw is the backstop.

## Delivery

Two independent PRs, in this order:

1. **PR 1 — `ProtoSerializerAttribute`:** Core attribute, generator gathering/resolution/validation,
   harness replays (AotRefGen, DifferentialTests, AotDifferential), golden + differential +
   cross-assembly + AotSmoke coverage, diagnostics + `AnalyzerReleases.Unshipped.md`,
   `docs/releasenotes.md` entry, AGENTS.md/aot-findings updates.
2. **PR 2 — `ProtoDataFormatAttribute`:** Core attribute, runtime helper + `MetaType` hook,
   generator port, runtime unit tests + isolated assembly-scope test + golden/differential/AotSmoke
   coverage, release notes and docs updates.

Prototype and green-through-CI on this fork first, then upstream PRs to mgravell/protobuf-net,
each preceded by an issue laying out the rationale (the circular-reference argument for why
`[ProtoContract(Serializer=)]` cannot serve; the `CompatibilityLevelAttribute` precedent for the
format default).

## Open questions carried to upstream review (non-blocking)

- Should `ProtoDataFormatAttribute` be `[Experimental]` alongside the other new AOT-era API, or
  plain like its `CompatibilityLevelAttribute` precedent?
- Is "model-level `[ProtoSerializer]` beats the type's own `[ProtoContract(Serializer=)]`" the
  wanted precedence?

## Non-goals

- No service-layer (`[ServiceContract]`/`[OperationContract]`) work.
- No runtime honoring of `ProtoSerializerAttribute` (the declarative form stays generator-only;
  `MetaType.SerializerType` remains the runtime route).
- No map key/value formats in the DataFormat default (v1).
- No widening of the existing "scalar serializer as collection element / map side" refusal.
- No imperative `RuntimeTypeModel` API for format defaults.
- No commitment that upstream accepts either proposal as designed.

## Alternatives considered and rejected

- **Fix surrogate merge-priming instead of new API:** even fixed, a surrogate is a sub-message on
  the wire; the motivating union must travel as the bare scalar. Also touches merge semantics for
  every existing surrogate user.
- **`[ProtoContract(Serializer = typeof(S<>))]` on the union itself, with open-generic support:**
  structurally impossible for the motivating consumer — the domain assembly would have to
  reference the serializer assembly that references it back.
- **`[assembly: ProtoSurrogate(typeof(Guid), ...)]` as a fixed-Guid workaround:** `Guid` is an
  inbuilt type; `SetSurrogate` on inbuilt types throws by design, so runtime parity is
  unreachable and the wire shape is wrong anyway.
- **Guid-only format attribute:** same implementation cost as the general mechanism, and invites
  the "why special-case Guid?" review objection.
