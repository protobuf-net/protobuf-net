# ProtoSerializerAttribute Implementation Plan (PR 1)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `[ProtoSerializer(Type, Serializer, IsScalar = …)]` — an assembly/model-scoped declaration binding a hand-written serializer to a type that cannot carry `[ProtoContract(Serializer = …)]` itself, with open-generic mapping, honored by the AOT source generator (generator-only; the runtime twin is the existing `MetaType.SerializerType`).

**Architecture:** Clone the `[ProtoSurrogate]` mechanism end to end: a Core attribute, a gathering pass in `ProtoModelGenerator.Parse.cs` (referenced assemblies → this assembly → the model, keyed by `Qualified` full name), resolution threaded into contract and member parsing, and replays onto reference models in the three harnesses (DifferentialTests, AotRefGen, AotDifferential). The emit side already exists (`ExternalSerializerTypeName` plan fields, `ISerializerProxy<T>` proxy, three-route category resolution) — no new emit shapes.

**Tech Stack:** C# / Roslyn 4.3.1 API surface (netstandard2.0 generator), xUnit, the repo's golden-fixture + differential + native-AOT harnesses.

**Spec:** `docs/superpowers/specs/2026-08-12-aot-external-serializer-dataformat-design.md` (Proposal 1).

## Global Constraints

- Work on a branch cut from `design/aot-external-serializer-dataformat` (the spec lives there): `git checkout -b feat/proto-serializer-attribute design/aot-external-serializer-dataformat`.
- **Match types by full name, never `SymbolEqualityComparer` across assemblies** — repo doctrine; harnesses load the generator reflectively.
- **The generator compiles against Roslyn 4.3.1** — no newer Roslyn APIs (e.g. no `LanguageVersion.CSharp12` constant, no `Construct` overloads newer than `Construct(params ITypeSymbol[])`).
- **Nothing in `Internal/Aot/` may hold a Roslyn reference** (`ProtoModelPlanShapeTests` enforces it). Any new plan field must be added to that type's `Equals` (incremental caching breaks silently otherwise).
- **Golden tests rewrite `Data/*.output.cs`/`*.txt` in the source tree on every run**: a new fixture fails its first run (nothing to compare), then re-run and review `git diff`. Never hand-edit a golden.
- Diagnostics reuse the existing kinds — `Contract(…)` → PBN2002, `Option(…)` → PBN2003 — with explanatory strings, matching every existing surrogate refusal. **No new diagnostic IDs**, so `AnalyzerReleases.Unshipped.md` is untouched. (This refines the spec, which said "new IDs in the PBN2xxx block"; Task 1 amends the spec.)
- `PBN2003` renders as `"Contract '{0}' is omitted from the AOT model: {1} is not supported yet."` — every `Option(…)` string must read as a noun phrase that ends before "is not supported yet".
- `[ProtoSerializer]` is `[Experimental("PBN9001")]`; the golden harness already suppresses PBN9001, `AotSmoke`/`AotConformanceTests`/`AotRefGen` csproj `NoWarn` it, and standalone in-memory compilations need `#pragma warning disable PBN9001`.
- Commit messages: repo-style narrative one-liners, ending with `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- `AotRefGen` is net472 and **cannot run on Linux**; fixtures added here carry the "no `.reference.cs` yet — added on Linux" header (copy the exact wording from `Data/DynamicCategory.input.cs:1-5`).
- Test commands used throughout:
  - Golden: `dotnet test src/BuildToolsUnitTests/BuildToolsUnitTests.csproj --filter "FullyQualifiedName~ProtoModelGeneratorTests"`
  - Differential: `dotnet test src/AotConformanceTests/AotConformanceTests.csproj`
  - Corpus: `dotnet build src/protobuf-net.Test/protobuf-net.Test.csproj src/Examples/Examples.csproj src/protobuf-net.Reflection.Test/protobuf-net.Reflection.Test.csproj && PBN_NO_SCHEMAS=1 dotnet run --project src/AotDifferential/AotDifferential.csproj`
  - Smoke (trim analysis at build): `dotnet build src/AotSmoke/AotSmoke.csproj` then `dotnet run --project src/AotSmoke -c Debug`

---

### Task 1: Core attribute + spec sync

**Files:**
- Create: `src/protobuf-net.Core/ProtoSerializerAttribute.cs`
- Modify: `docs/superpowers/specs/2026-08-12-aot-external-serializer-dataformat-design.md` (two refinements)

**Interfaces:**
- Produces: `ProtoBuf.ProtoSerializerAttribute` — ctor `(Type type, Type serializer)`, properties `Type Type { get; }`, `Type Serializer { get; }`, `bool IsScalar { get; set; }`. Every later task matches it by the full name `"ProtoBuf.ProtoSerializerAttribute"`.

- [ ] **Step 1: Write the attribute** (model: `src/protobuf-net.Core/ProtoSurrogateAttribute.cs`)

```csharp
using System;
using System.Diagnostics.CodeAnalysis;

namespace ProtoBuf
{
    /// <summary>
    /// Declares that a type is serialized by a hand-written serializer, for types that cannot carry
    /// <see cref="ProtoContractAttribute.Serializer"/> themselves — a BCL type, or anything else you
    /// do not own or cannot couple to protobuf-net.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the compile-time equivalent of <c>MetaType.SerializerType</c>, and is read by the
    /// protobuf-net source generator; it has no effect on the reflection-based model.
    /// </para>
    /// <para>
    /// Apply it to a generated model to configure that model alone, or to an <b>assembly</b> to
    /// offer the pairing to every model that references it — which is how a library ships
    /// serializers for the types it supports, without each consumer restating them. A model's own
    /// declaration wins over one it merely references, and over the type's own
    /// <see cref="ProtoContractAttribute.Serializer"/>; an assembly's does not.
    /// </para>
    /// <para>
    /// <see cref="Type"/> and <see cref="Serializer"/> may both be open generic definitions of the
    /// same arity, in which case the serializer is closed with the type arguments of each use site;
    /// a closed declaration wins over the open mapping for that one type.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    [Experimental(ProtoModelAttribute.DiagnosticId)]
    public sealed class ProtoSerializerAttribute : Attribute
    {
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="type">The type being serialized.</param>
        /// <param name="serializer">The hand-written serializer that carries its wire shape.</param>
        public ProtoSerializerAttribute(Type type, Type serializer)
        {
            Type = type;
            Serializer = serializer;
        }

        /// <summary>
        /// The type being serialized.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The hand-written serializer; a concrete class with a parameterless constructor,
        /// implementing <c>ISerializer&lt;T&gt;</c> for <see cref="Type"/>.
        /// </summary>
        public Type Serializer { get; }

        /// <summary>
        /// States the serializer's category outright — the only route that survives into metadata,
        /// and so the only one available when the serializer lives in a compiled reference. Setting
        /// it to <c>false</c> is an explicit message-category declaration, distinct from omitting it
        /// (which defers the framing to the serializer's own <c>Features</c>).
        /// </summary>
        public bool IsScalar { get; set; }
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build src/protobuf-net.Core/protobuf-net.Core.csproj`
Expected: success, no new warnings.

- [ ] **Step 3: Amend the spec** — two edits so the documents stay consistent:
  1. In the "Validation" bullet list under Proposal 1, replace "new IDs in the PBN2xxx block, recorded in `AnalyzerReleases.Unshipped.md`" with "reported through the existing PBN2002/PBN2003 kinds with explanatory strings, matching every existing surrogate refusal; no new IDs, so `AnalyzerReleases.Unshipped.md` is untouched".
  2. In "Test plan" Proposal 1, rename the golden fixture from `ExternalSerializer.input.cs` to `ModelSerializer.input.cs` — `Data/ExternalSerializer.input.cs` already exists (it covers `[ProtoContract(Serializer = …)]`), and the new fixture mirrors `ModelSurrogate.input.cs` instead.

- [ ] **Step 4: Commit**

```bash
git add src/protobuf-net.Core/ProtoSerializerAttribute.cs docs/superpowers/specs/2026-08-12-aot-external-serializer-dataformat-design.md
git commit -m "[ProtoSerializer]: the declarative twin of MetaType.SerializerType, as an attribute

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: Gathering + structural validation of declarations

**Files:**
- Modify: `src/protobuf-net.BuildTools/Generators/ProtoModelGenerator.Parse.cs` (constant block ~L16-39; new types + `GetExternalSerializers` beside `GetSurrogates` ~L1992; call site in `Parse` beside L60; thread into `ParseContract`'s parameter list ~L253)
- Create: `src/BuildToolsUnitTests/Aot/Data/Diagnostics/SerializerDeclaration.input.cs` (goldens `.output.cs`/`.output.txt` are written by the test)

**Interfaces:**
- Produces (all `private` in `ProtoModelGenerator`):
  - `enum SerializerScope { Referenced, Assembly, Model }`
  - `sealed class SerializerDeclaration { INamedTypeSymbol Serializer; bool? IsScalar; SerializerScope Scope; }`
  - `sealed class ExternalSerializers { Dictionary<string, SerializerDeclaration> Closed; Dictionary<string, SerializerDeclaration> Open; bool IsEmpty; }` — `Closed` keyed by `Qualified(compilation, type)`, `Open` keyed by `Qualified(compilation, type.OriginalDefinition)`, both `StringComparer.Ordinal`
  - `static ExternalSerializers GetExternalSerializers(Compilation, INamedTypeSymbol model, List<PlanDiagnostic>)`
- Consumes: `Qualified`, `Simplify`, `Contract(…)`, `PlanLocation.From` — all existing.

- [ ] **Step 1: Write the failing golden fixture** — malformed declarations that must produce gathering-time diagnostics:

```csharp
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

// Structural validation of [ProtoSerializer] declarations, reported at gathering time: an open/
// closed mismatch and an arity mismatch are both mistakes in the declaration itself, before any
// contract is parsed. Under Diagnostics/ because the point is the .txt, not working output.
namespace AotFixtures.SerializerDeclaration;

public readonly struct Pair<TKey, TValue> { }

public sealed class PairSerializer<TKey, TValue> : ISerializer<Pair<TKey, TValue>>
{
    SerializerFeatures ISerializer<Pair<TKey, TValue>>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;
    Pair<TKey, TValue> ISerializer<Pair<TKey, TValue>>.Read(ref ProtoReader.State state, Pair<TKey, TValue> value)
        => default;
    void ISerializer<Pair<TKey, TValue>>.Write(ref ProtoWriter.State state, Pair<TKey, TValue> value)
        => state.WriteInt32(0);
}

public sealed class OneArgSerializer<T> : ISerializer<Pair<T, int>>
{
    SerializerFeatures ISerializer<Pair<T, int>>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;
    Pair<T, int> ISerializer<Pair<T, int>>.Read(ref ProtoReader.State state, Pair<T, int> value) => default;
    void ISerializer<Pair<T, int>>.Write(ref ProtoWriter.State state, Pair<T, int> value) => state.WriteInt32(0);
}

[ProtoContract]
public class Untouched
{
    [ProtoMember(1)] public int Value { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Untouched))]
// open type, closed serializer: mismatch
[ProtoSerializer(typeof(Pair<,>), typeof(PairSerializer<int, int>))]
// open both, arities differ
[ProtoSerializer(typeof(Pair<,>), typeof(OneArgSerializer<>))]
// declared twice at the same scope: no defined winner, so the duplicate is reported
[ProtoSerializer(typeof(Pair<int, int>), typeof(PairSerializer<int, int>))]
[ProtoSerializer(typeof(Pair<int, int>), typeof(PairSerializer<int, int>))]
public partial class SerializerDeclarationModel : TypeModel
{
}
```

- [ ] **Step 2: Run the golden test to verify it fails** (no goldens exist yet)

Run: `dotnet test src/BuildToolsUnitTests/BuildToolsUnitTests.csproj --filter "FullyQualifiedName~ProtoModelGeneratorTests"`
Expected: FAIL for `Diagnostics/SerializerDeclaration.input.cs` (first-run golden mismatch).

- [ ] **Step 3: Implement gathering.** Add the constant `private const string ProtoSerializerAttributeName = "ProtoBuf.ProtoSerializerAttribute";` beside `ProtoSurrogateAttributeName` (L21). Then, beside `GetSurrogates`:

```csharp
private enum SerializerScope { Referenced, Assembly, Model }

/// <summary>
/// A [ProtoSerializer] declaration: a hand-written serializer for a type that cannot carry
/// [ProtoContract(Serializer = ...)] itself.
/// </summary>
private sealed class SerializerDeclaration
{
    public SerializerDeclaration(INamedTypeSymbol serializer, bool? isScalar, SerializerScope scope)
    {
        Serializer = serializer;
        IsScalar = isScalar;
        Scope = scope;
    }

    public INamedTypeSymbol Serializer { get; }

    /// <summary>Null when the declaration did not state a category.</summary>
    public bool? IsScalar { get; }

    public SerializerScope Scope { get; }
}

/// <summary>
/// The [ProtoSerializer] declarations visible to this model, closed instantiations and open
/// generic mappings separately; a closed declaration wins over the open mapping for its type.
/// </summary>
private sealed class ExternalSerializers
{
    public Dictionary<string, SerializerDeclaration> Closed { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, SerializerDeclaration> Open { get; } = new(StringComparer.Ordinal);
    public bool IsEmpty => Closed.Count == 0 && Open.Count == 0;
}

/// <summary>
/// Gather [ProtoSerializer] declarations: referenced assemblies, then this assembly, then the
/// model, so the most specific wins — the identical walk GetSurrogates does, and assembly
/// attributes only, for the same cost reason.
/// </summary>
private static ExternalSerializers GetExternalSerializers(
    Compilation compilation, INamedTypeSymbol model, List<PlanDiagnostic> diagnostics)
{
    var result = new ExternalSerializers();
    foreach (var reference in compilation.SourceModule.ReferencedAssemblySymbols)
    {
        Collect(reference.GetAttributes(), SerializerScope.Referenced);
    }
    Collect(compilation.Assembly.GetAttributes(), SerializerScope.Assembly);
    Collect(model.GetAttributes(), SerializerScope.Model);
    return result;

    void Collect(IEnumerable<AttributeData> attributes, SerializerScope scope)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass?.ToDisplayString() != ProtoSerializerAttributeName) continue;
            if (attribute.ConstructorArguments.Length != 2) continue;
            if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol type) continue;
            if (attribute.ConstructorArguments[1].Value is not INamedTypeSymbol serializer) continue;

            bool? isScalar = null;
            foreach (var argument in attribute.NamedArguments)
            {
                // only named arguments actually written appear here, so "omitted" and
                // "IsScalar = false" are distinguishable
                if (argument.Key == "IsScalar" && argument.Value.Value is bool scalar)
                {
                    isScalar = scalar;
                }
            }

            var at = PlanLocation.From(model);
            var name = Simplify(Qualified(compilation, type));
            if (type.IsUnboundGenericType != serializer.IsUnboundGenericType)
            {
                Contract(diagnostics, at, name,
                    "[ProtoSerializer] pairs an open generic definition with a closed type; "
                    + "declare both open with the same arity, or both closed");
                continue;
            }
            if (type.IsUnboundGenericType && type.Arity != serializer.Arity)
            {
                Contract(diagnostics, at, name,
                    $"[ProtoSerializer] pairs {name} (arity {type.Arity}) with "
                    + $"{Simplify(Qualified(compilation, serializer))} (arity {serializer.Arity}); "
                    + "an open mapping needs the same arity on both sides");
                continue;
            }

            var map = type.IsUnboundGenericType ? result.Open : result.Closed;
            var key = type.IsUnboundGenericType
                ? Qualified(compilation, type.OriginalDefinition)
                : Qualified(compilation, type);
            if (map.TryGetValue(key, out var existing) && existing.Scope == scope)
            {
                // across scopes the more specific wins silently; *within* a scope there is no
                // defined winner, so say so and keep the first
                Contract(diagnostics, at, name,
                    "[ProtoSerializer] is declared more than once for the same type at the same scope");
                continue;
            }
            map[key] = new SerializerDeclaration(serializer, isScalar, scope);
        }
    }
}
```

Call it from `Parse` beside the `GetSurrogates` call site (L60): `var serializers = GetExternalSerializers(compilation, model, diagnostics);` and thread `serializers` into `ParseContract` as a new parameter after `surrogates` (declarations are inert until Task 3 resolves them — the parameter is added now so the signature only changes once).

- [ ] **Step 4: Re-run, review, verify**

Run: `dotnet test src/BuildToolsUnitTests/BuildToolsUnitTests.csproj --filter "FullyQualifiedName~ProtoModelGeneratorTests"`
Expected: PASS on second run. `git diff` shows the new `SerializerDeclaration.output.txt` containing three PBN2002 lines (open/closed mismatch, arity mismatch, same-scope duplicate) quoting the strings above, and an `.output.cs` where `Untouched` still emits normally.

- [ ] **Step 5: Commit**

```bash
git add src/protobuf-net.BuildTools/Generators/ProtoModelGenerator.Parse.cs src/BuildToolsUnitTests/Aot/Data/Diagnostics/SerializerDeclaration.*
git commit -m "[ProtoSerializer] declarations are gathered like surrogates: three scopes, most specific wins

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: Closed-declaration resolution, contract and member level

**Files:**
- Modify: `src/protobuf-net.BuildTools/Generators/ProtoModelGenerator.Parse.cs`:
  - `ParseContract` — lookup beside the surrogate lookup (L280-284), widen the `!HasContractFamily` refusal (L289), return-block after the open-generic refusal (~L314)
  - `GetSubSerializer` (L2085), `ResolveExternalScalar` (L1486), `HasExternalSerializer` (L1533) — each gains `(Compilation, ExternalSerializers)` awareness
  - `GetMemberShape` (L2521), `AsMap` (L2838), the repeated/element path (~L2953), `GetMessageKind` (L2973) — thread `serializers` beside the existing `surrogates` parameter
- Create: `src/BuildToolsUnitTests/Aot/Data/ModelSerializer.input.cs` (v1: closed declaration only)

**Interfaces:**
- Produces:
  - `static SerializerDeclaration? ResolveSerializerDeclaration(Compilation, ExternalSerializers, INamedTypeSymbol type, out INamedTypeSymbol? closedSerializer)` — closed map first, then (Task 4) the open map. Task 4 extends this method; nothing else changes shape.
  - `static ProtoContractPlan? ExternalContract(Compilation, List<PlanDiagnostic>, PlanLocation, string name, INamedTypeSymbol type, SerializerDeclaration, INamedTypeSymbol serializer)` — validation + category + plan.
- Consumes: `ReadCategoryFromSource` (L1552), `ProtoContractPlan(externalSerializerTypeName:, externalSerializerIsScalar:, externalSerializerCategoryKnown:)` (AotPlans.cs L668), `Option(…)`, `HasExternalSerializer` (the existing own-attribute test).

- [ ] **Step 1: Write the failing fixture (v1 — closed declaration)**

`src/BuildToolsUnitTests/Aot/Data/ModelSerializer.input.cs`:

```csharp
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System.Runtime.Serialization;

// NOTE: no .reference.cs yet - added on Linux, and AotRefGen is net472 so it could not be run.
// Nothing here is refused by ref-emit once the harness replays the declarations, so this fixture
// *should* have one. Differentially covered in the meantime by AotConformanceTests, which replays
// [ProtoSerializer] onto the reference model through MetaType.SerializerType. Run AotRefGen on
// Windows and commit the result.
//
// [ProtoSerializer] on the model is the compile-time equivalent of MetaType.SerializerType: a
// hand-written serializer for a type that cannot carry [ProtoContract(Serializer = ...)] itself -
// because you do not own it, or because the serializer lives in an assembly the type cannot
// reference back (a domain type whose serializer ships in an infrastructure assembly).
namespace AotFixtures.ModelSerializer;

// a scalar union shape: the wire form is the payload's own, with no message wrapper. The type
// carries no protobuf-net attribute at all - the declaration stands in for the contract.
public readonly struct Wrapped<T>
{
    public Wrapped(long tag) => Tag = tag;
    public long Tag { get; }
}

// the closed pairing: Wrapped<byte> is framed fixed32 where the generic form (Task 4) is varint,
// so the two are distinguishable on the wire
public sealed class WrappedByteSerializer : ISerializer<Wrapped<byte>>
{
    SerializerFeatures ISerializer<Wrapped<byte>>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeFixed32;

    Wrapped<byte> ISerializer<Wrapped<byte>>.Read(ref ProtoReader.State state, Wrapped<byte> value)
        => new Wrapped<byte>(state.ReadInt32());

    void ISerializer<Wrapped<byte>>.Write(ref ProtoWriter.State state, Wrapped<byte> value)
        => state.WriteInt32((int)value.Tag);
}

// WCF-style contract: [DataContract]/[DataMember(Order)] supply the family and the field numbers
[DataContract]
public class Request
{
    [DataMember(Order = 1)] public Wrapped<byte> Special { get; set; }
    [DataMember(Order = 2)] public int Plain { get; set; }
}

public static class ModelSerializerSamples
{
    public static object[] Values =>
    [
        new Request(),
        new Request { Special = new Wrapped<byte>(4) },
        new Request { Special = new Wrapped<byte>(200), Plain = 7 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Request))]
[ProtoSerializer(typeof(Wrapped<byte>), typeof(WrappedByteSerializer), IsScalar = true)]
public partial class ModelSerializerModel : TypeModel
{
}
```

- [ ] **Step 2: Run golden test to verify current behavior** — expect FAIL (first run) and, after re-run, an `.output.txt` showing `Request` dropped ("has unsupported member" or "not a contract") because nothing resolves `Wrapped<byte>` yet. Do **not** commit these goldens; they exist to prove the feature is off.

- [ ] **Step 3: Implement resolution.**

3a. The resolver (Task 4 adds the open-map arm):

```csharp
/// <summary>
/// The [ProtoSerializer] declaration serving a type, if any: an exact closed declaration first,
/// then the open mapping for its generic definition, closed with the use site's arguments.
/// </summary>
private static SerializerDeclaration? ResolveSerializerDeclaration(
    Compilation compilation, ExternalSerializers serializers, INamedTypeSymbol type,
    out INamedTypeSymbol? closedSerializer)
{
    closedSerializer = null;
    if (serializers.IsEmpty) return null;
    if (serializers.Closed.TryGetValue(Qualified(compilation, type), out var declaration))
    {
        closedSerializer = declaration.Serializer;
        return declaration;
    }
    return null;
}
```

3b. In `ParseContract`, immediately after the `surrogates.TryGetValue` lookup (L280-284):

```csharp
var externalDeclaration = ResolveSerializerDeclaration(compilation, serializers, type,
    out var declaredSerializer);
if (externalDeclaration is not null && externalDeclaration.Scope != SerializerScope.Model
    && HasExternalSerializer(type))
{
    // the type's own [ProtoContract(Serializer = ...)] wins over anything short of the model
    externalDeclaration = null;
    declaredSerializer = null;
}
if (externalDeclaration is not null && declaredSurrogate is not null)
{
    return Contract(diagnostics, at, name,
        "the type is declared with both [ProtoSurrogate] and [ProtoSerializer]; remove one");
}
```

Widen L289 to `if (declaredSurrogate is null && externalDeclaration is null && !HasContractFamily(type))`.

After the open-generic refusal block (L311-314) — so an open *seed* is still refused before this fires:

```csharp
// a declared serializer replaces the body entirely, exactly as [ProtoContract(Serializer = ...)]
// does: the type contributes nothing but its identity, so nothing further is parsed
if (externalDeclaration is not null)
{
    return ExternalContract(compilation, diagnostics, at, name, type,
        externalDeclaration, declaredSerializer!);
}
```

3c. The validation + plan helper (mirror the wording of the existing L471-488 accessibility refusal; the category logic is the L1321-1349 block re-targeted at the declaration):

```csharp
/// <summary>
/// The plan for a type served by a [ProtoSerializer] declaration: validation the runtime twin
/// (MetaType.SerializerType) performs by throwing, then the same three-route category resolution
/// [ProtoContract(Serializer = ...)] gets.
/// </summary>
private static ProtoContractPlan? ExternalContract(
    Compilation compilation, List<PlanDiagnostic> diagnostics, PlanLocation at, string name,
    INamedTypeSymbol type, SerializerDeclaration declaration, INamedTypeSymbol serializer)
{
    var display = Simplify(serializer.ToDisplayString());
    if (!compilation.IsSymbolAccessibleWithin(serializer, compilation.Assembly))
    {
        return Option(diagnostics, at, name,
            $"[ProtoSerializer(..., typeof({display}))], because that serializer is not accessible here");
    }
    if (serializer.TypeKind != TypeKind.Class || serializer.IsAbstract)
    {
        return Option(diagnostics, at, name,
            $"[ProtoSerializer(..., typeof({display}))], because a custom serializer must be a concrete class");
    }
    if (!serializer.InstanceConstructors.Any(static ctor => ctor.Parameters.Length == 0))
    {
        // SerializerCache activates it with nonPublic: true, so any parameterless constructor will do
        return Option(diagnostics, at, name,
            $"[ProtoSerializer(..., typeof({display}))], because that serializer has no parameterless constructor");
    }
    if (!ImplementsSerializerFor(compilation, serializer, type))
    {
        return Option(diagnostics, at, name,
            $"[ProtoSerializer(..., typeof({display}))], because that serializer does not implement "
            + $"ISerializer<{Simplify(Qualified(compilation, type))}>");
    }

    var fromSource = ReadCategoryFromSource(compilation, serializer);
    if (declaration.IsScalar is { } stated && fromSource is { } observed && stated != observed)
    {
        return Option(diagnostics, at, name,
            $"[ProtoSerializer(IsScalar = {(stated ? "true" : "false")})], which contradicts "
            + $"the serializer: {display}.Features declares Category{(observed ? "Scalar" : "Message")}");
    }
    var isScalar = declaration.IsScalar ?? fromSource;
    return new ProtoContractPlan(
        Qualified(compilation, type),
        default, type.IsValueType,
        externalSerializerTypeName: Qualified(compilation, serializer),
        externalSerializerIsScalar: isScalar == true,
        externalSerializerCategoryKnown: isScalar is not null);
}

private static bool ImplementsSerializerFor(
    Compilation compilation, INamedTypeSymbol serializer, INamedTypeSymbol type)
{
    var wanted = Qualified(compilation, type);
    foreach (var iface in serializer.AllInterfaces)
    {
        if (iface.Arity != 1) continue;
        if (iface.OriginalDefinition.ToDisplayString() != "ProtoBuf.Serializers.ISerializer<T>") continue;
        if (Qualified(compilation, iface.TypeArguments[0]) == wanted) return true;
    }
    return false;
}
```

3d. Member level — each helper gains the maps, with the same "own attribute wins below model scope" mediation:

- `GetSubSerializer(Compilation compilation, INamedTypeSymbol type, ExternalSerializers serializers)`: before the existing own-attribute loop, resolve the declaration; if it applies (model scope, or no own `Serializer=`), return `$"global::ProtoBuf.Serializers.SerializerCache.Get<{Qualified(compilation, closedSerializer)}, {Qualified(compilation, type)}>()"`. The existing loop is otherwise unchanged (including the inaccessible-means-inbuilt `"null"` return, which stays own-attribute-only — a declared-but-inaccessible serializer is a refusal from `ExternalContract`, and the member cascades).
- `ResolveExternalScalar(Compilation, INamedTypeSymbol, ExternalSerializers)`: when the declaration applies, return `declaration.IsScalar ?? ReadCategoryFromSource(compilation, closedSerializer)`; otherwise the existing own-attribute path.
- `HasExternalSerializer(Compilation, INamedTypeSymbol, ExternalSerializers)` (new overload; keep the old one for the mediation test itself): own attribute *or* declaration resolves.
- `GetMessageKind` — beside the surrogate clause (L2991-2998), add:

```csharp
// ...and so does a type a [ProtoSerializer] declaration serves, the same way
if (serializers is not null
    && ResolveSerializerDeclaration(compilation, serializers, named, out _) is not null)
{
    message = named;
    return ProtoMemberKind.Message;
}
```

- Thread `ExternalSerializers? serializers = null` through `GetMemberShape` / `AsMap` / the repeated-element path, beside the existing `surrogates` parameter, updating every call site the compiler flags.

- [ ] **Step 4: Run golden test twice, review diff**

Expected after re-run: `ModelSerializer.output.cs` shows `ISerializerProxy<global::AotFixtures.ModelSerializer.Wrapped<byte>>` handing out `SerializerCache.Get<global::AotFixtures.ModelSerializer.WrappedByteSerializer, …>()`, `Request` emitting `Special` via `state.WriteAny<…>(1, …)` on the write and `.Read(ref state,` on the read (scalar framing), and the services constructor carrying the `Debug.Assert` for the stated category. Empty `.output.txt` (file absent).

- [ ] **Step 5: Run the full BuildToolsUnitTests suite** (not just golden) to catch signature-threading fallout:

Run: `dotnet test src/BuildToolsUnitTests/BuildToolsUnitTests.csproj`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add -A src/protobuf-net.BuildTools src/BuildToolsUnitTests
git commit -m "A closed [ProtoSerializer] declaration serves a type the way its own Serializer= would

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: Open-generic mapping

**Files:**
- Modify: `src/protobuf-net.BuildTools/Generators/ProtoModelGenerator.Parse.cs` (`ResolveSerializerDeclaration` only)
- Modify: `src/BuildToolsUnitTests/Aot/Data/ModelSerializer.input.cs` (v2: add the open mapping)

**Interfaces:**
- Produces: the open-map arm of `ResolveSerializerDeclaration` — closed declarations win; the serializer is closed via `Construct`.

- [ ] **Step 1: Extend the fixture** — add to `ModelSerializer.input.cs`:

The generic serializer (after `Wrapped<T>`):

```csharp
// the open mapping: one declaration serves every instantiation the model meets. Varint framing,
// distinguishable from the closed override's fixed32
public sealed class WrappedSerializer<T> : ISerializer<Wrapped<T>>
{
    SerializerFeatures ISerializer<Wrapped<T>>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;

    Wrapped<T> ISerializer<Wrapped<T>>.Read(ref ProtoReader.State state, Wrapped<T> value)
        => new Wrapped<T>(state.ReadInt64());

    void ISerializer<Wrapped<T>>.Write(ref ProtoWriter.State state, Wrapped<T> value)
        => state.WriteInt64(value.Tag);
}
```

Two members on `Request`:

```csharp
    [DataMember(Order = 3)] public Wrapped<int> Id { get; set; }
    [DataMember(Order = 4)] public Wrapped<string> Label { get; set; }
```

The open declaration on the model, above the closed one so the fixture reads "open mapping, closed override":

```csharp
[ProtoSerializer(typeof(Wrapped<>), typeof(WrappedSerializer<>), IsScalar = true)]
```

New samples (replace the `Values` list — every scalar differs, per differential doctrine):

```csharp
    public static object[] Values =>
    [
        new Request(),
        new Request { Special = new Wrapped<byte>(4) },
        new Request { Id = new Wrapped<int>(11), Label = new Wrapped<string>(12) },
        new Request { Special = new Wrapped<byte>(200), Plain = 7, Id = new Wrapped<int>(-13) },
    ];
```

- [ ] **Step 2: Run golden test** — expect FAIL then, on re-run, `Id`/`Label` still *dropping* the contract (open map not consulted yet). Don't commit.

- [ ] **Step 3: Implement** — add to `ResolveSerializerDeclaration`, after the closed lookup:

```csharp
    if (type.IsGenericType && !type.IsUnboundGenericType
        && serializers.Open.TryGetValue(Qualified(compilation, type.OriginalDefinition), out declaration))
    {
        // close the serializer over the use site's arguments; arity was validated at gathering
        closedSerializer = declaration.Serializer.OriginalDefinition
            .Construct(type.TypeArguments.ToArray());
        return declaration;
    }
```

Note `ReadCategoryFromSource` runs against the *constructed* serializer symbol; its members' `DeclaringSyntaxReferences` still point at the definition's source, so route 2 folding keeps working — the fixture proves it, since the declaration also states `IsScalar` and the contradiction check would fire if the two disagreed.

- [ ] **Step 4: Run golden test twice, review diff** — `Wrapped<int>` and `Wrapped<string>` each get their own `ISerializerProxy<…>` over `SerializerCache.Get<global::…WrappedSerializer<int>, …>()` / `…<string>…`; `Wrapped<byte>` still resolves to `WrappedByteSerializer` (closed beats open).

- [ ] **Step 5: Commit**

```bash
git add -A src/protobuf-net.BuildTools src/BuildToolsUnitTests
git commit -m "An open [ProtoSerializer] mapping closes over each instantiation the model meets

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: Per-use validation diagnostics + the Debug.Assert message

**Files:**
- Create: `src/BuildToolsUnitTests/Aot/Data/Diagnostics/SerializerValidation.input.cs`
- Modify: `src/protobuf-net.BuildTools/Generators/ProtoModelGenerator.Emit.cs` (L1126, the assert message in `EmitExternalCategoryAsserts`)

- [ ] **Step 1: Write the failing fixture** — one declaration per refusal `ExternalContract` performs:

```csharp
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

// Per-use validation of [ProtoSerializer]: each declaration here is structurally well-formed but
// names a serializer the runtime twin (MetaType.SerializerType / SerializerCache.Get) would reject
// at run time - reported as a warning naming the defect instead.
namespace AotFixtures.SerializerValidation;

public readonly struct Alpha { }
public readonly struct Beta { }
public readonly struct Gamma { }
public readonly struct Delta { }

// not a class
public struct AlphaSerializer : ISerializer<Alpha>
{
    SerializerFeatures ISerializer<Alpha>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;
    Alpha ISerializer<Alpha>.Read(ref ProtoReader.State state, Alpha value) => default;
    void ISerializer<Alpha>.Write(ref ProtoWriter.State state, Alpha value) => state.WriteInt32(0);
}

// no parameterless constructor
public sealed class BetaSerializer : ISerializer<Beta>
{
    public BetaSerializer(int seed) { }
    SerializerFeatures ISerializer<Beta>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;
    Beta ISerializer<Beta>.Read(ref ProtoReader.State state, Beta value) => default;
    void ISerializer<Beta>.Write(ref ProtoWriter.State state, Beta value) => state.WriteInt32(0);
}

// does not implement ISerializer<Gamma>
public sealed class GammaSerializer
{
}

// states a category its Features contradicts
public sealed class DeltaSerializer : ISerializer<Delta>
{
    SerializerFeatures ISerializer<Delta>.Features
        => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;
    Delta ISerializer<Delta>.Read(ref ProtoReader.State state, Delta value) => default;
    void ISerializer<Delta>.Write(ref ProtoWriter.State state, Delta value) { }
}

[ProtoContract]
public class Carrier
{
    [ProtoMember(1)] public Alpha Alpha { get; set; }
    [ProtoMember(2)] public Beta Beta { get; set; }
    [ProtoMember(3)] public Gamma Gamma { get; set; }
    [ProtoMember(4)] public Delta Delta { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Carrier))]
[ProtoSerializer(typeof(Alpha), typeof(AlphaSerializer))]
[ProtoSerializer(typeof(Beta), typeof(BetaSerializer))]
[ProtoSerializer(typeof(Gamma), typeof(GammaSerializer))]
[ProtoSerializer(typeof(Delta), typeof(DeltaSerializer), IsScalar = true)]
public partial class SerializerValidationModel : TypeModel
{
}
```

- [ ] **Step 2: Run golden test twice, review** — the `.output.txt` should carry four PBN2003 warnings (concrete class / parameterless constructor / does not implement / contradicts), plus PBN2004 cascades: `Carrier` drops because all four members reference dropped types. Verify each message matches the `ExternalContract` strings from Task 3 — this fixture *is* the test for them; if any refusal doesn't fire, fix `ExternalContract`, not the golden.

- [ ] **Step 3: Reword the assert message** in `EmitExternalCategoryAsserts` (Emit.cs L1126). It currently hard-codes a fix that doesn't exist for declaration-served types:

```csharp
Line(sb, indent + 2, $"\"{Simplify(contract.TypeName)} is generated as {expected}, but its "
    + $"serializer disagrees; \"");
Line(sb, indent + 3, $"+ \"set IsScalar = {fix} on its [ProtoContract] or [ProtoSerializer] "
    + $"declaration, or correct the serializer.\");");
```

- [ ] **Step 4: Run golden test twice** — expect rewritten `.output.cs` goldens for every fixture carrying the assert (`ExternalSerializer`, `ModelSerializer`, others the diff reveals). Review that only the assert string changed.

- [ ] **Step 5: Run full suite + commit**

```bash
dotnet test src/BuildToolsUnitTests/BuildToolsUnitTests.csproj
git add -A src/protobuf-net.BuildTools src/BuildToolsUnitTests
git commit -m "[ProtoSerializer] refusals say what the runtime would have thrown; the assert names both routes

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 6: Nullable members of declaration-served structs — probe, then match

A `Wrapped<long>?` member is routine under the consumer's cardinality conventions. Whether ref-emit supports `Nullable<T>` where `T` has a custom serializer is **not recorded anywhere in the repo** — this task establishes it empirically and matches it, whichever way it goes.

**Files:**
- Modify: `src/BuildToolsUnitTests/Aot/Data/ModelSerializer.input.cs` (add the member + samples)
- Possibly modify: `src/protobuf-net.BuildTools/Generators/ProtoModelGenerator.Parse.cs`

- [ ] **Step 1: Probe ref-emit first** — a scratch xUnit fact in `src/protobuf-net.Test` (delete before commit), using the existing in-repo shape `[ProtoContract(Serializer = …, IsScalar = true)]` (AotSmoke's `Batch` is the pattern):

```csharp
[Fact]
public void NullableOfCustomSerializerStruct_Probe()
{
    var model = RuntimeTypeModel.Create();
    model.AutoCompile = false;
    var holder = new NullableHolder { Value = new Stamp(42) };
    using var ms = new MemoryStream();
    model.Serialize(ms, holder);   // does this throw, or write field 1?
    ms.Position = 0;
    var clone = model.Deserialize<NullableHolder>(ms);
    Assert.Equal(42, clone.Value!.Value.Value);
}
[ProtoContract] public class NullableHolder { [ProtoMember(1)] public Stamp? Value { get; set; } }
// Stamp/StampSerializer copied from src/BuildToolsUnitTests/Aot/Data/ExternalSerializer.input.cs
```

Record the outcome (works / throws, and the exception text if it throws).

- [ ] **Step 2a: If ref-emit supports it** — add to the fixture: `[DataMember(Order = 5)] public Wrapped<long>? Optional { get; set; }` plus samples `new Request { Optional = new Wrapped<long>(21) }` and one leaving it null. Run golden + (after Task 7) differential; fix the generator until bytes match. The expected emit shape: the member is `ProtoMemberKind.Message` with `SubSerializerIsScalar`, nullable handling via the existing `HasValue`/`GetValueOrDefault` struct-member pattern — but **the differential is the arbiter**, not this prediction; adjust to whatever ref-emit produces.
- [ ] **Step 2b: If ref-emit throws** — the correct outcome is a refusal that *matches*: in the member path, refuse `Nullable<T>` where `T` resolves through a serializer declaration, via `Member(diagnostics, atMember, name, symbol.Name, "a nullable member of a type served by a hand-written serializer; protobuf-net throws for this too (<quote the exception>)")`. Add the member to a `Diagnostics/` fixture instead of `ModelSerializer.input.cs`, and drop the `Wrapped<string>?` smoke member from Task 11.

- [ ] **Step 3: Delete the probe test, run golden suite, commit**

```bash
git add -A src/protobuf-net.BuildTools src/BuildToolsUnitTests
git commit -m "Nullable members of declaration-served structs: <matched ref-emit / refused as ref-emit throws>

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 7: Differential replay (AotConformanceTests)

**Files:**
- Modify: `src/AotConformanceTests/DifferentialTests.cs` — constant beside L26, `ApplySerializers` beside `ApplySurrogates` (L212), call in `CreateReference` after L207's `ApplySurrogates(runtime, modelType);`

**Interfaces:**
- Produces: `static void ApplySerializers(RuntimeTypeModel runtime, Type modelType, Type contractType)` and `static IEnumerable<Type> ReachableTypes(Type root)` — Task 8 and Task 10 copy this shape (the harnesses deliberately duplicate, as they already do for `ApplySurrogates`).
- Consumes: `MetaType.SerializerType` (settable; throws unless the type is a class — `src/protobuf-net/Meta/MetaType.cs:1917`).

- [ ] **Step 1: Run the differential suite to verify current failure**

Run: `dotnet test src/AotConformanceTests/AotConformanceTests.csproj`
Expected: FAIL for `AotFixtures.ModelSerializer.ModelSerializerModel` cases — the reference model has never heard of the declarations, so it throws "No serializer defined for type Wrapped<…>".

- [ ] **Step 2: Implement the replay**

```csharp
private const string ProtoSerializerAttribute = "ProtoBuf.ProtoSerializerAttribute";
```

```csharp
/// <summary>
/// Replay the model's [ProtoSerializer] declarations, the compile-time equivalent of
/// <c>MetaType.SerializerType</c>. Open declarations are closed over every matching instantiation
/// reachable from the contract's member graph, which is exactly the set the generator closes over.
/// </summary>
private static void ApplySerializers(RuntimeTypeModel runtime, Type modelType, Type contractType)
{
    var declarations = modelType.Assembly.GetCustomAttributes()
        .Concat(modelType.GetCustomAttributes())
        .Where(static x => x.GetType().FullName == ProtoSerializerAttribute)
        .ToList();
    if (declarations.Count == 0) return;

    var closed = new List<(Type Type, Type Serializer)>();
    var open = new List<(Type Definition, Type Serializer)>();
    foreach (var declaration in declarations)
    {
        var type = declaration.GetType();
        var underlying = (Type)type.GetProperty("Type")!.GetValue(declaration)!;
        var serializer = (Type)type.GetProperty("Serializer")!.GetValue(declaration)!;
        if (underlying.IsGenericTypeDefinition) open.Add((underlying, serializer));
        else closed.Add((underlying, serializer));
    }

    foreach (var reached in ReachableTypes(contractType))
    {
        if (!reached.IsConstructedGenericType) continue;
        foreach (var (definition, serializer) in open)
        {
            if (reached.GetGenericTypeDefinition() != definition) continue;
            if (closed.Any(x => x.Type == reached)) continue; // a closed declaration wins
            closed.Add((reached, serializer.MakeGenericType(reached.GenericTypeArguments)));
        }
    }

    foreach (var (underlying, serializer) in closed)
    {
        runtime.Add(underlying, applyDefaultBehaviour: false).SerializerType = serializer;
    }
}

/// <summary>
/// Every type reachable from a contract's public members. Member recursion stays inside the
/// fixture assembly; generic arguments and element types are always followed.
/// </summary>
private static IEnumerable<Type> ReachableTypes(Type root)
{
    var seen = new HashSet<Type>();
    var stack = new Stack<Type>();
    stack.Push(root);
    while (stack.Count > 0)
    {
        var current = stack.Pop();
        if (!seen.Add(current)) continue;
        yield return current;
        if (Nullable.GetUnderlyingType(current) is { } wrapped) stack.Push(wrapped);
        if (current.IsConstructedGenericType)
        {
            foreach (var argument in current.GenericTypeArguments) stack.Push(argument);
        }
        if (current.IsArray) stack.Push(current.GetElementType()!);
        if (current.Assembly != root.Assembly) continue;
        foreach (var property in current.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            stack.Push(property.PropertyType);
        }
        foreach (var field in current.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            stack.Push(field.FieldType);
        }
    }
}
```

In `CreateReference` (L207), after `ApplySurrogates(runtime, modelType);` add `ApplySerializers(runtime, modelType, contractType);` — before `runtime.Add(contractType, …)`, since the model must be fully configured before any serializer is built.

- [ ] **Step 3: Run the differential suite**

Run: `dotnet test src/AotConformanceTests/AotConformanceTests.csproj`
Expected: PASS — bytes match, cross-deserialization holds, and `RepeatedFieldOccurrencesMergeIdentically` is satisfied (the fixture has ≥2 `Request` samples).

- [ ] **Step 4: Commit**

```bash
git add src/AotConformanceTests/DifferentialTests.cs
git commit -m "The differential replays [ProtoSerializer] onto the reference model, closing open mappings per contract

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 8: AotRefGen replay + handover note

**Files:**
- Modify: `src/AotRefGen/Program.cs` — `ApplySerializers` after `ApplySurrogates` (called at L163, before the seed loop)
- Modify: `docs/aot-findings.md` — Handover section

- [ ] **Step 1: Implement the typed replay** (AotRefGen links Core, so no reflection needed; `ReachableTypes` copied from Task 7, rooted at each seed):

```csharp
/// <summary>
/// Replay the model's [ProtoSerializer] declarations onto the reference model, which is what
/// MetaType.SerializerType exists for. Open declarations are closed over every matching
/// instantiation reachable from the seeds' member graphs.
/// </summary>
private static void ApplySerializers(RuntimeTypeModel model, Type modelType, List<Type> seeds)
{
    var declarations = modelType.Assembly
        .GetCustomAttributes(typeof(ProtoSerializerAttribute), inherit: false)
        .Cast<ProtoSerializerAttribute>()
        .Concat(modelType.GetCustomAttributes(typeof(ProtoSerializerAttribute), inherit: false)
            .Cast<ProtoSerializerAttribute>())
        .ToList();
    if (declarations.Count == 0) return;

    var closed = new List<(Type Type, Type Serializer)>();
    var open = new List<(Type Definition, Type Serializer)>();
    foreach (var declaration in declarations)
    {
        if (declaration.Type.IsGenericTypeDefinition) open.Add((declaration.Type, declaration.Serializer));
        else closed.Add((declaration.Type, declaration.Serializer));
    }

    foreach (var seed in seeds)
    foreach (var reached in ReachableTypes(seed))
    {
        if (!reached.IsConstructedGenericType) continue;
        foreach (var (definition, serializer) in open)
        {
            if (reached.GetGenericTypeDefinition() != definition) continue;
            if (closed.Any(x => x.Type == reached)) continue;
            closed.Add((reached, serializer.MakeGenericType(reached.GetGenericArguments())));
        }
    }

    foreach (var (underlying, serializer) in closed)
    {
        model.Add(underlying, applyDefaultBehaviour: false).SerializerType = serializer;
    }
}
```

Call it in `Emit` after `ApplySurrogates(model, modelType);` (L163): `ApplySerializers(model, modelType, seeds);`. Note net472/C# 7.3-compatible syntax may be needed — match the file's existing style (it uses `new object[] { … }`, not collection expressions).

- [ ] **Step 2: Verify it compiles.** `dotnet build src/AotRefGen/AotRefGen.csproj` — on Linux this may fail for lack of net472 reference assemblies; if so, verify by eye against the file's existing patterns and rely on the Windows CI build. Record which happened.

- [ ] **Step 3: Add the handover line** to the Handover section of `docs/aot-findings.md`: `ModelSerializer.input.cs` (and `SerializerValidation` if applicable) need an `AotRefGen` run on Windows to produce `.reference.cs`; the fixture headers say so.

- [ ] **Step 4: Commit**

```bash
git add src/AotRefGen/Program.cs docs/aot-findings.md
git commit -m "AotRefGen replays [ProtoSerializer] so the reference output covers declaration-served types

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 9: Cross-assembly hand-off test

**Files:**
- Create: `src/BuildToolsUnitTests/Aot/ProtoSerializerReferenceTests.cs` (mirror `ProtoSurrogateReferenceTests.cs`, including its `Compile` helper verbatim)

The three-assembly shape from the spec: domain types in one compilation, serializers + assembly-level declaration in a second, WCF-attributed contracts + a model that declares nothing in a third. This is also the only place the **metadata-only** routes are honest: the serializer's `Features` cannot fold across a compiled reference, so `IsScalar` on the declaration (route 1) and the `WriteAny`/`ReadAny` deferral (route 3) are both load-bearing here.

- [ ] **Step 1: Write the failing tests**

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Generators;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace BuildToolsUnitTests.Aot
{
    /// <summary>
    /// [ProtoSerializer] across real assembly boundaries: the domain type, the serializer package
    /// that declares the pairing, and a consumer that says nothing - the NodaTime-style hand-off,
    /// for serializers instead of surrogates.
    /// </summary>
    public class ProtoSerializerReferenceTests : AotGeneratorTestBase
    {
        public ProtoSerializerReferenceTests(ITestOutputHelper log) : base(log) { }

        // plain domain types, no protobuf-net awareness anywhere
        private const string DomainSource = """
            namespace Norse
            {
                public readonly struct Token<T>
                {
                    public Token(long tag) => Tag = tag;
                    public long Tag { get; }
                }
            }
            """;

        // knows both sides; ships the serializer and offers the pairing to every consumer
        private const string HelperSource = """
            using ProtoBuf;
            using ProtoBuf.Serializers;

            #pragma warning disable PBN9001 // the compile-time model attributes are [Experimental]
            [assembly: ProtoSerializer(typeof(Norse.Token<>), typeof(Norse.Proto.TokenSerializer<>),
                IsScalar = true)]
            #pragma warning restore PBN9001

            namespace Norse.Proto
            {
                public sealed class TokenSerializer<T> : ISerializer<Norse.Token<T>>
                {
                    SerializerFeatures ISerializer<Norse.Token<T>>.Features
                        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;

                    Norse.Token<T> ISerializer<Norse.Token<T>>.Read(ref ProtoReader.State state, Norse.Token<T> value)
                        => new Norse.Token<T>(state.ReadInt64());

                    void ISerializer<Norse.Token<T>>.Write(ref ProtoWriter.State state, Norse.Token<T> value)
                        => state.WriteInt64(value.Tag);
                }
            }
            """;

        // WCF-attributed contracts; a third assembly so the serializer only ever arrives as metadata
        private const string ContractsSource = """
            using System.Runtime.Serialization;

            namespace Norse.Contracts
            {
                [DataContract]
                public class LoginRequest
                {
                    [DataMember(Order = 1)] public Norse.Token<int> UserId { get; set; }
                    [DataMember(Order = 2)] public string Password { get; set; }
                }
            }
            """;

        private const string ConsumerSource = """
            using ProtoBuf;
            using ProtoBuf.Meta;

            namespace Consumer
            {
                [ProtoModel]
                [ProtoSerializable(typeof(Norse.Contracts.LoginRequest))]
                public partial class ClientModel : TypeModel
                {
                }
            }
            """;

        [Fact]
        public void SerializerOfferedByAReferencedHelperIsHonoured()
        {
            var domain = Compile("Norse", DomainSource);
            var helper = Compile("Norse.Proto", HelperSource, domain);
            var contracts = Compile("Norse.Contracts", ContractsSource, domain);

            var result = Execute<ProtoModelGenerator>(ConsumerSource, null,
                extraReferences: new[] { domain, helper, contracts });

            Assert.Equal(0, result.ErrorCount);
            Assert.Contains("ISerializerProxy<global::Norse.Token<int>>", result.GeneratedCode);
            Assert.Contains(
                "SerializerCache.Get<global::Norse.Proto.TokenSerializer<int>, global::Norse.Token<int>>()",
                result.GeneratedCode);
            // IsScalar came from the declaration - the metadata-only route - so the member is
            // framed by the serializer, not as a sub-message
            Assert.Contains(".Read(ref state,", result.GeneratedCode);
            Assert.Contains("WriteAny<global::Norse.Token<int>>", result.GeneratedCode);
        }

        [Fact]
        public void WithoutIsScalarTheFramingDefersToRuntime()
        {
            var helperNoScalar = HelperSource.Replace(",\n    IsScalar = true", "")
                .Replace(", IsScalar = true", "");
            var domain = Compile("Norse", DomainSource);
            var helper = Compile("Norse.Proto", helperNoScalar, domain);
            var contracts = Compile("Norse.Contracts", ContractsSource, domain);

            var result = Execute<ProtoModelGenerator>(ConsumerSource, null,
                extraReferences: new[] { domain, helper, contracts });

            Assert.Equal(0, result.ErrorCount);
            // Features cannot fold across a compiled reference, so ReadAny/WriteAny decide at run time
            Assert.Contains("state.ReadAny<global::Norse.Token<int>>", result.GeneratedCode);
        }

        [Fact]
        public void AModelCanOverrideAnOfferFromAReference()
        {
            var consumerOverride = """
                using ProtoBuf;
                using ProtoBuf.Meta;
                using ProtoBuf.Serializers;

                namespace Consumer
                {
                    public sealed class MyTokenSerializer : ISerializer<Norse.Token<int>>
                    {
                        SerializerFeatures ISerializer<Norse.Token<int>>.Features
                            => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeFixed64;

                        Norse.Token<int> ISerializer<Norse.Token<int>>.Read(ref ProtoReader.State state, Norse.Token<int> value)
                            => new Norse.Token<int>(state.ReadInt64());

                        void ISerializer<Norse.Token<int>>.Write(ref ProtoWriter.State state, Norse.Token<int> value)
                            => state.WriteInt64(value.Tag);
                    }

                    [ProtoModel]
                    [ProtoSerializable(typeof(Norse.Contracts.LoginRequest))]
                    [ProtoSerializer(typeof(Norse.Token<int>), typeof(MyTokenSerializer), IsScalar = true)]
                    public partial class ClientModel : TypeModel
                    {
                    }
                }
                """;
            var domain = Compile("Norse", DomainSource);
            var helper = Compile("Norse.Proto", HelperSource, domain);
            var contracts = Compile("Norse.Contracts", ContractsSource, domain);

            var result = Execute<ProtoModelGenerator>(consumerOverride, null,
                extraReferences: new[] { domain, helper, contracts });

            Assert.Equal(0, result.ErrorCount);
            Assert.Contains("MyTokenSerializer", result.GeneratedCode);
            Assert.DoesNotContain("TokenSerializer<int>", result.GeneratedCode);
        }

        // copy the Compile helper verbatim from ProtoSurrogateReferenceTests.cs:150-167
    }
}
```

- [ ] **Step 2: Run to verify they fail / pass appropriately**

Run: `dotnet test src/BuildToolsUnitTests/BuildToolsUnitTests.csproj --filter "FullyQualifiedName~ProtoSerializerReferenceTests"`
Expected: PASS if Tasks 2-4 are complete (this is an integration proof, not new machinery). Any failure is a real precedence/metadata bug — fix in Parse.cs, not the test. Adjust the exact assert substrings to the generator's real output the first time (the emitted spellings for `WriteAny`/`ReadAny` may differ in whitespace/arguments — read `result.GeneratedCode`, then pin what it actually says, keeping the *distinguishing* substring: proxy for the closed type, `SerializerCache.Get<…TokenSerializer<int>…`, and `ReadAny` only in the no-IsScalar variant).

- [ ] **Step 3: Commit**

```bash
git add src/BuildToolsUnitTests/Aot/ProtoSerializerReferenceTests.cs
git commit -m "The three-assembly hand-off works for serializers: domain, package, consumer that declares nothing

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 10: AotDifferential corpus replay

**Files:**
- Modify: `src/AotDifferential/Program.cs` — `ApplySerializers(reference, corpus)` beside `ApplySurrogates(reference, corpus)` at L43

- [ ] **Step 1: Implement** — same shape as `ApplySurrogates` (Program.cs L87-165): eager-load `corpus.Neighbours`, scan `AppDomain.CurrentDomain.GetAssemblies()` for attributes whose `FullName == "ProtoBuf.ProtoSerializerAttribute"`, split closed/open by `IsGenericTypeDefinition`; close open declarations over every constructed-generic type reachable from `corpus.Contracts` member graphs (the `ReachableTypes` walk from Task 7, rooted per contract, with the assembly guard comparing to each contract's assembly); apply with `reference.Add(underlying, applyDefaultBehaviour: false).SerializerType = serializer;`, each `Apply` wrapped in the same try/catch-and-report the surrogate replay uses.

- [ ] **Step 2: Run the corpus**

Run: `dotnet build src/protobuf-net.Test/protobuf-net.Test.csproj src/Examples/Examples.csproj src/protobuf-net.Reflection.Test/protobuf-net.Reflection.Test.csproj && PBN_NO_SCHEMAS=1 dotnet run --project src/AotDifferential/AotDifferential.csproj`
Expected: exit code 0, zero mismatches. The corpus carries no `[ProtoSerializer]` declarations today, so this proves the replay is inert where undeclared — the regression guard, not the feature proof (Task 7 was that).

- [ ] **Step 3: Commit**

```bash
git add src/AotDifferential/Program.cs
git commit -m "The corpus differential replays [ProtoSerializer] declarations like it replays surrogates

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 11: AotSmoke members

**Files:**
- Modify: `src/AotSmoke/Program.cs` — members on `Order` (next free tag is **58**; verify against the file), serializer declarations beside `BatchSerializer` (~L449), model attribute on `SmokeModel` (~L485), asserts beside L648

- [ ] **Step 1: Add the types and declaration**

```csharp
// [ProtoSerializer] on the model: the open mapping closes over each instantiation, so ILC must
// generate SerializerCache<TallySerializer<int>, Tally<int>> from a name that exists only in
// generated code - the declaration-served twin of Batch/Gauge/Barcode above.
public readonly struct Tally<T>
{
    public Tally(long count) => Count = count;
    public long Count { get; }
}

public sealed class TallySerializer<T> : ISerializer<Tally<T>>
{
    SerializerFeatures ISerializer<Tally<T>>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;

    Tally<T> ISerializer<Tally<T>>.Read(ref ProtoReader.State state, Tally<T> value)
        => new Tally<T>(state.ReadInt64());

    void ISerializer<Tally<T>>.Write(ref ProtoWriter.State state, Tally<T> value)
        => state.WriteInt64(value.Count);
}
```

On `Order`:

```csharp
    // declaration-served scalar: field 58 is a bare varint - a wrongly-assumed message category
    // would have written a length prefix over it, so check the payload dump by eye too
    [ProtoMember(58)] public Tally<int> Score { get; set; }
```

(Include `[ProtoMember(59)] public Tally<string>? Bonus { get; set; }` only if Task 6 landed nullable support.)

On `SmokeModel`: `[ProtoSerializer(typeof(Tally<>), typeof(TallySerializer<>), IsScalar = true)]`.

In `Main`: populate `Score = new Tally<int>(421)` (and `Bonus = new Tally<string>(-9)` if applicable), and beside the L648 checks:

```csharp
        Check(ref failures, "Score", original.Score.Count, clone.Score.Count);
```

- [ ] **Step 2: Build (trim analysis) + run Debug (exercises the category assert)**

Run: `dotnet build src/AotSmoke/AotSmoke.csproj && dotnet run --project src/AotSmoke -c Debug`
Expected: build succeeds; run prints the payload hex and `AOT smoke test PASSED`. Verify field 58 in the dump is a bare varint (tag `D0-03` then the varint, no length prefix).

- [ ] **Step 3: Re-measure the trim-warning baseline.** Clear `src/AotSmoke/obj` and `bin`, then `dotnet publish src/AotSmoke/AotSmoke.csproj -c Release -r linux-x64` (if the platform toolchain is available — otherwise record that the publish is pending Windows CI). Compare the warning count and binary size to the pre-change baseline measured the same way; record the delta in `docs/aot-findings.md`.

- [ ] **Step 4: Commit**

```bash
git add src/AotSmoke/Program.cs docs/aot-findings.md
git commit -m "AotSmoke proves an open [ProtoSerializer] mapping survives ILC end to end

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 12: Docs, release notes, full sweep

**Files:**
- Modify: `docs/releasenotes.md`, `AGENTS.md`

- [ ] **Step 1: Release notes** — add under the next unreleased version heading, matching the file's list style:

```
- add `[ProtoSerializer]` (`[Experimental]`): assembly/model-scoped hand-written serializer
  declarations for the AOT generator, with open-generic mapping — the declarative twin of
  `MetaType.SerializerType` (#xxxx)
```

- [ ] **Step 2: AGENTS.md** — in the "Hand-written serializers" subsection, add a short paragraph: `[ProtoSerializer]` externalizes `Serializer =` exactly as `[ProtoSurrogate]` externalizes `Surrogate =` (same three-scope gathering, same full-name matching, closed-beats-open, own-attribute-beats-assembly-but-not-model); all three harnesses replay it through `MetaType.SerializerType`; the category assert's message now names both routes.

- [ ] **Step 3: Full sweep** — all four harness commands from Global Constraints, expecting green (corpus: zero mismatches; goldens: no unexpected diffs in `git status`).

- [ ] **Step 4: Commit + open the PR** (against this fork's `main`; the upstream PR to mgravell/protobuf-net follows an issue, per the spec's Delivery section)

```bash
git add docs/releasenotes.md AGENTS.md
git commit -m "Docs: [ProtoSerializer] is the declarative twin of MetaType.SerializerType

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```
