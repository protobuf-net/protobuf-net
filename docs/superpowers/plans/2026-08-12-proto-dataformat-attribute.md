# ProtoDataFormatAttribute Implementation Plan (PR 2)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `[ProtoDataFormat(Type, DataFormat)]` — a cross-cutting per-type `DataFormat` default (assembly/module/type scoped, explicit member format always wins), honored by **both** the reflection runtime and the AOT generator, so `[DataMember]` Guids at compatibility level 300 can be 16-byte `GuidBytes` with two assembly lines.

**Architecture:** Clone the `CompatibilityLevelAttribute` machinery on both sides: a Core attribute, an internal `TypeDataFormatHelper` beside `TypeCompatibilityHelper` (runtime), a hook in `MetaType.ApplyDefaultBehaviour` where the `ValueMember` is built, and a sibling of the generator's `GetCompatibilityLevel` walk in `ProtoModelGenerator.Parse.cs`. Because both models honor the attribute, the differential suite asserts JIT/AOT wire parity directly — no replay needed.

**Tech Stack:** C# / Roslyn 4.3.1 API surface (netstandard2.0 generator), xUnit, the repo's runtime tests + golden/differential/native-AOT harnesses.

**Spec:** `docs/superpowers/specs/2026-08-12-aot-external-serializer-dataformat-design.md` (Proposal 2). Independent of PR 1; no ordering dependency.

## Global Constraints

- Branch: `git checkout -b feat/proto-dataformat-attribute design/aot-external-serializer-dataformat` (or from `main` once the spec has merged).
- **Never cast `DataFormat` → the generator's `ProtoDataFormat`** — the ordinals differ (`FixedSize` = 3 casts onto `Group`); always go through `GetDataFormat(int)` (`ProtoModelGenerator.Parse.cs:2317`).
- **`DataFormat.Default` is the zero sentinel** — "explicitly Default" and "unstated" are indistinguishable, and both take the cross-cutting default (spec-recorded quirk). Consequence: a member cannot opt back to the default *format* under a type default; the escape is stating a concrete format on `[ProtoMember]`.
- **Maps are excluded**: `MapKeyFormat`/`MapValueFormat` (`[ProtoMap]`) are a separate path on both sides and must not see the injected default.
- **`[NullWrappedValue]`/`[NullWrappedCollection]` members are exempt from injection** — the runtime throws on a lone `[NullWrappedValue]` with any non-Default format (`ValueMember.cs:529`) and the generator refuses it (`Parse.cs:1107-1113`); injecting a default there would break contracts that work today.
- **Fixture-assembly trap:** the differential/conformance fixtures link into one assembly, and `protobuf-net.Test` contracts share one module — an assembly- or module-scoped `[ProtoDataFormat]` in either would silently re-format every Guid in it. Assembly/module scope is proven **only** in the isolated satellite project (Task 4); everything else uses type scope.
- Golden tests rewrite goldens in the source tree; new fixtures fail their first run; never hand-edit a golden.
- The attribute is **not** `[Experimental]` (spec decision; its precedent `CompatibilityLevelAttribute` is not either), so no PBN9001 handling anywhere.
- Commit messages: repo-style narrative one-liners, ending with `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- Test commands:
  - Runtime: `dotnet test src/protobuf-net.Test/protobuf-net.Test.csproj --filter "FullyQualifiedName~ProtoDataFormat"`
  - Golden: `dotnet test src/BuildToolsUnitTests/BuildToolsUnitTests.csproj --filter "FullyQualifiedName~ProtoModelGeneratorTests"`
  - Differential: `dotnet test src/AotConformanceTests/AotConformanceTests.csproj`
  - Corpus: `dotnet build src/protobuf-net.Test/protobuf-net.Test.csproj src/Examples/Examples.csproj src/protobuf-net.Reflection.Test/protobuf-net.Reflection.Test.csproj && PBN_NO_SCHEMAS=1 dotnet run --project src/AotDifferential/AotDifferential.csproj`
  - Smoke: `dotnet build src/AotSmoke/AotSmoke.csproj && dotnet run --project src/AotSmoke -c Debug`

### Known-good wire literals (reused throughout)

From `src/protobuf-net.Test/CompatibilityLevelListsMaps.cs:370,384-399` — `Guid.Parse("c416e4af-455e-414c-948c-f27873263547")` at field 1:

| form | hex |
| --- | --- |
| level 300 `GuidBytes` (FixedSize) | `0A-10-C4-16-E4-AF-45-5E-41-4C-94-8C-F2-78-73-26-35-47` |
| level 300 `GuidString` (default) | `0A-24-63-34-31-36-65-34-61-66-2D-34-35-35-65-2D-34-31-34-63-2D-39-34-38-63-2D-66-32-37-38-37-33-32-36-33-35-34-37` |

---

### Task 1: Core attribute

**Files:**
- Create: `src/protobuf-net.Core/ProtoDataFormatAttribute.cs`

**Interfaces:**
- Produces: `ProtoBuf.ProtoDataFormatAttribute` — ctor `(Type type, DataFormat dataFormat)`, properties `Type Type { get; }`, `DataFormat DataFormat { get; }`. Matched by the runtime as a typed attribute, by the generator via the full name `"ProtoBuf.ProtoDataFormatAttribute"`.

- [ ] **Step 1: Write the attribute** (model: the attribute half of `src/protobuf-net.Core/CompatibilityLevel.cs`; the one deliberate difference is `AllowMultiple = true`, one declaration per keyed type)

```csharp
using System;
using System.ComponentModel;

namespace ProtoBuf
{
    /// <summary>
    /// Declares the default <see cref="ProtoBuf.DataFormat"/> for members of a given type, applied
    /// wherever the member does not state a format itself. An explicit
    /// <see cref="ProtoMemberAttribute.DataFormat"/> always wins.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Resolution mirrors <see cref="CompatibilityLevelAttribute"/>: the declaring contract type
    /// (including its base types), then the module, then the assembly. The declaration applies to
    /// the member's scalar type — for a <c>Nullable&lt;T&gt;</c> member the underlying <c>T</c>,
    /// for a repeated member the element type. Map key/value formats belong to
    /// <see cref="ProtoMapAttribute"/> and are not affected.
    /// </para>
    /// <para>
    /// The motivating case: <c>[assembly: ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]</c>
    /// alongside <c>[assembly: CompatibilityLevel(CompatibilityLevel.Level300)]</c> makes every
    /// undecorated <see cref="Guid"/> member serialize as the 16-byte form.
    /// </para>
    /// </remarks>
    [ImmutableObject(true)]
    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Module
        | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
        AllowMultiple = true, Inherited = true)]
    public sealed class ProtoDataFormatAttribute : Attribute
    {
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="type">The member type the default applies to.</param>
        /// <param name="dataFormat">The format such members take when they state none.</param>
        public ProtoDataFormatAttribute(Type type, DataFormat dataFormat)
        {
            Type = type;
            DataFormat = dataFormat;
        }

        /// <summary>
        /// The member type the default applies to.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The format such members take when they state none.
        /// </summary>
        public DataFormat DataFormat { get; }
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build src/protobuf-net.Core/protobuf-net.Core.csproj`
Expected: success.

- [ ] **Step 3: Commit**

```bash
git add src/protobuf-net.Core/ProtoDataFormatAttribute.cs
git commit -m "[ProtoDataFormat]: a per-type format default, resolved like CompatibilityLevel

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: TypeDataFormatHelper + type-scope resolution tests

**Files:**
- Create: `src/protobuf-net/Internal/TypeDataFormatHelper.cs`
- Create: `src/protobuf-net.Test/ProtoDataFormatTests.cs`

**Interfaces:**
- Produces: `internal static DataFormat ProtoBuf.Internal.TypeDataFormatHelper.GetTypeDataFormat(Type declaringType, Type scalarType)` — returns `DataFormat.Default` when nothing is declared. Task 3 calls it from `MetaType`; the test project reaches it via the existing `InternalsVisibleTo` (as `CompatibilityLevelTests` reaches `TypeCompatibilityHelper`).

- [ ] **Step 1: Write the failing tests** — helper-level, type scope only (module/assembly scope is Task 4's satellite; declaring either here would poison the whole test assembly):

```csharp
using ProtoBuf;
using ProtoBuf.Internal;
using System;
using Xunit;

namespace ProtoBuf.Test
{
    public class ProtoDataFormatTests
    {
        [ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
        [ProtoDataFormat(typeof(int), DataFormat.ZigZag)]
        public class Declaring { }

        public class Derived : Declaring { }

        [ProtoDataFormat(typeof(int), DataFormat.FixedSize)]
        public class Overriding : Declaring { }

        public class Plain { }

        [Fact]
        public void DeclaredTypeIsMatched()
            => Assert.Equal(DataFormat.FixedSize,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Declaring), typeof(Guid)));

        [Fact]
        public void MultipleDeclarationsAreKeyedByType()
            => Assert.Equal(DataFormat.ZigZag,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Declaring), typeof(int)));

        [Fact]
        public void UndeclaredTypeGetsDefault()
            => Assert.Equal(DataFormat.Default,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Declaring), typeof(long)));

        [Fact]
        public void BaseTypeDeclarationIsInherited()
            => Assert.Equal(DataFormat.FixedSize,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Derived), typeof(Guid)));

        [Fact]
        public void DerivedDeclarationWinsOverBase()
            => Assert.Equal(DataFormat.FixedSize,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Overriding), typeof(int)));

        [Fact]
        public void UndecoratedTypeGetsDefault()
            => Assert.Equal(DataFormat.Default,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Plain), typeof(Guid)));
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test src/protobuf-net.Test/protobuf-net.Test.csproj --filter "FullyQualifiedName~ProtoDataFormatTests"`
Expected: FAIL — `TypeDataFormatHelper` does not exist (compile error).

- [ ] **Step 3: Implement** (model: `src/protobuf-net/Internal/TypeCompatibilityHelper.cs`, including its calculate-outside-the-lock caching)

```csharp
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf.Internal
{
    /// <summary>
    /// Resolves the cross-cutting per-type <see cref="DataFormat"/> default: the declaring type
    /// (walking base types), then the module, then the assembly — a sibling of
    /// <see cref="TypeCompatibilityHelper"/>, keyed per scalar type because the attribute is
    /// AllowMultiple.
    /// </summary>
    internal static class TypeDataFormatHelper
    {
        private static readonly Dictionary<Module, KeyValuePair<Type, DataFormat>[]> s_ByModule
            = new Dictionary<Module, KeyValuePair<Type, DataFormat>[]>();

        internal static DataFormat GetTypeDataFormat(Type declaringType, Type scalarType)
        {
            // explicit base-type walk with inherit: false per level: AllowMultiple = true makes
            // Attribute.GetCustomAttributes(..., inherit: true) merge base and derived declarations
            // with no defined winner, and derived must win
            for (var current = declaringType; current is object; current = current.BaseType)
            {
                if (FindDeclared(Attribute.GetCustomAttributes(
                    current, typeof(ProtoDataFormatAttribute), inherit: false), scalarType) is { } declared)
                {
                    return declared;
                }
            }
            foreach (var pair in GetModuleDefaults(declaringType.Module))
            {
                if (pair.Key == scalarType) return pair.Value;
            }
            return DataFormat.Default;
        }

        private static DataFormat? FindDeclared(Attribute[] attributes, Type scalarType)
        {
            foreach (var attribute in attributes)
            {
                if (attribute is ProtoDataFormatAttribute declared && declared.Type == scalarType)
                {
                    return declared.DataFormat;
                }
            }
            return null;
        }

        private static KeyValuePair<Type, DataFormat>[] GetModuleDefaults(Module module)
        {
            if (module is null) return Array.Empty<KeyValuePair<Type, DataFormat>>();
            lock (s_ByModule)
            {
                if (s_ByModule.TryGetValue(module, out var alreadyKnown)) return alreadyKnown;
            }
            // calculated twice outside the lock rather than blocking other paths; indexer-set,
            // not Add — the same trade TypeCompatibilityHelper records
            var calculated = Calculate(module);
            lock (s_ByModule)
            {
                s_ByModule[module] = calculated;
            }
            return calculated;

            static KeyValuePair<Type, DataFormat>[] Calculate(Module module)
            {
                var result = new List<KeyValuePair<Type, DataFormat>>();
                // module first, then assembly, skipping types the module already declared —
                // module wins, as it does for CompatibilityLevel
                foreach (ProtoDataFormatAttribute declared in Attribute.GetCustomAttributes(
                    module, typeof(ProtoDataFormatAttribute), inherit: true))
                {
                    result.Add(new KeyValuePair<Type, DataFormat>(declared.Type, declared.DataFormat));
                }
                var assembly = module.Assembly;
                if (assembly is object)
                {
                    foreach (ProtoDataFormatAttribute declared in Attribute.GetCustomAttributes(
                        assembly, typeof(ProtoDataFormatAttribute), inherit: true))
                    {
                        var seen = false;
                        foreach (var pair in result)
                        {
                            if (pair.Key == declared.Type) { seen = true; break; }
                        }
                        if (!seen)
                        {
                            result.Add(new KeyValuePair<Type, DataFormat>(declared.Type, declared.DataFormat));
                        }
                    }
                }
                return result.ToArray();
            }
        }
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test src/protobuf-net.Test/protobuf-net.Test.csproj --filter "FullyQualifiedName~ProtoDataFormatTests"`
Expected: PASS (6 tests).

- [ ] **Step 5: Commit**

```bash
git add src/protobuf-net/Internal/TypeDataFormatHelper.cs src/protobuf-net.Test/ProtoDataFormatTests.cs
git commit -m "TypeDataFormatHelper: the CompatibilityLevel walk, keyed per scalar type

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: The MetaType hook + wire-level runtime tests

**Files:**
- Modify: `src/protobuf-net/Meta/MetaType.cs` — `ApplyDefaultBehaviour(bool isEnum, ProtoMemberAttribute normalizedAttribute)`, beside the `GetMemberCompatibilityLevel` call (~L1234) and the `ValueMember` construction (~L1270)
- Modify: `src/protobuf-net.Test/ProtoDataFormatTests.cs` (add wire tests)

**Interfaces:**
- Consumes: `TypeDataFormatHelper.GetTypeDataFormat(Type, Type)` (Task 2); `GetAttribute(attribs, string)` (existing in `MetaType`); `model.TryGetRepeatedProvider(...)` / `repeated.ItemType` / `repeated.IsMap` (existing locals in the method).

- [ ] **Step 1: Write the failing wire tests** — append to `ProtoDataFormatTests` (an `AssertPayload<T>` helper copied from `CompatibilityLevelListsMaps.cs:401-418`, which exercises the runtime, `CompileInPlace`, and `Compile()` model modes):

```csharp
        private static readonly Guid s_KnownGuid = Guid.Parse("c416e4af-455e-414c-948c-f27873263547");

        [ProtoContract, CompatibilityLevel(CompatibilityLevel.Level300)]
        [ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
        public class FixedGuidHolder
        {
            [ProtoMember(1)] public Guid Id { get; set; }
        }

        [Fact]
        public void TypeScopedDefaultMakesBareGuidFixed() => AssertPayload(
            new FixedGuidHolder { Id = s_KnownGuid },
            "0A-10-C4-16-E4-AF-45-5E-41-4C-94-8C-F2-78-73-26-35-47");
        /*
        0A = field 1, type String
        10 = length 16
        payload = the guid's 16 bytes; without the default this is the 36-char string form
        */

        [ProtoContract, CompatibilityLevel(CompatibilityLevel.Level300)]
        [ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
        public class NullableAndRepeatedGuids
        {
            [ProtoMember(1)] public Guid? MaybeId { get; set; }
            [ProtoMember(2)] public List<Guid> Batch { get; } = new List<Guid>();
        }

        [Fact]
        public void NullableUnwrapsToTheDeclaredType() => AssertPayload(
            new NullableAndRepeatedGuids { MaybeId = s_KnownGuid },
            "0A-10-C4-16-E4-AF-45-5E-41-4C-94-8C-F2-78-73-26-35-47");

        [Fact]
        public void RepeatedMembersKeyOnTheElementType() => AssertPayload(
            new NullableAndRepeatedGuids { Batch = { s_KnownGuid } },
            "12-10-C4-16-E4-AF-45-5E-41-4C-94-8C-F2-78-73-26-35-47");

        [ProtoContract]
        [ProtoDataFormat(typeof(int), DataFormat.ZigZag)]
        public class ExplicitWins
        {
            [ProtoMember(1)] public int Defaulted { get; set; }
            [ProtoMember(2, DataFormat = DataFormat.FixedSize)] public int Stated { get; set; }
        }

        [Fact]
        public void ExplicitMemberFormatBeatsTheDefault() => AssertPayload(
            new ExplicitWins { Defaulted = -1, Stated = -1 },
            "08-01-15-FF-FF-FF-FF");
        /*
        08-01       = field 1 varint, -1 zigzag-encoded as 1 (the default applied)
        15-FF^4     = field 2 fixed32, -1 (the explicit format won)
        */

        [ProtoContract, CompatibilityLevel(CompatibilityLevel.Level300)]
        [ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
        public class MapUntouched
        {
            [ProtoMember(1)] public Dictionary<int, Guid> ById { get; } = new Dictionary<int, Guid>();
        }

        [Fact]
        public void MapValuesDoNotTakeTheDefault() => AssertPayload(
            new MapUntouched { ById = { { 1, s_KnownGuid } } },
            "0A-28-08-01-12-24-63-34-31-36-65-34-61-66-2D-34-35-35-65-2D-34-31-34-63-2D-39-34-38-63-2D-66-32-37-38-37-33-32-36-33-35-34-37");
        /*
        0A-28 = field 1 (the map entry), length 40: key (08-01) + value tag/len (12-24) + 36 bytes.
        The value is the 36-char *string* form — [ProtoMap(ValueFormat)] is the tool for maps;
        the cross-cutting default deliberately does not reach them
        */

        [ProtoContract] // level 200: FixedSize on a Guid is simply ignored, like the explicit form
        [ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
        public class Level200Ignores
        {
            [ProtoMember(1)] public Guid Id { get; set; }
        }

        [Fact]
        public void Level200IgnoresAFixedGuidDefault() => AssertPayload(
            new Level200Ignores { Id = s_KnownGuid },
            "0A-12-09-AF-E4-16-C4-5E-45-4C-41-11-94-8C-F2-78-73-26-35-47");
        /*
        the level-200 BclGuid form regardless of the declared default, matching the explicit
        per-member behaviour ("FixedSize on a Guid below level 300 is simply ignored"):
        0A-12 = field 1, length 18; inside, two Fixed64 halves (09-... / 11-...) — the same
        literal CompatibilityLevelListsMaps.VanillaHazGuidsPayload pins for this guid
        */

        [ProtoContract]
        [ProtoDataFormat(typeof(int), DataFormat.FixedSize)]
        public class NullWrappedExempt
        {
            [ProtoMember(1), NullWrappedValue] public int? Wrapped { get; set; }
        }

        [Fact]
        public void NullWrappedMembersAreExemptFromInjection()
        {
            // would throw while building the model if the default were injected
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(NullWrappedExempt), applyDefaultBehaviour: true);
            _ = model.Serialize<NullWrappedExempt>(new MemoryStream(), new NullWrappedExempt { Wrapped = 0 });
        }

        [ProtoContract]
        [ProtoDataFormat(typeof(DateTime), DataFormat.WellKnown)]
        public class WellKnownPromotes
        {
            [ProtoMember(1)] public DateTime When { get; set; }
        }

        [Fact]
        public void WellKnownDefaultPromotesLevel200To240()
        {
            // identical semantics to the explicit per-member format: WellKnown at level 200 means
            // 240, i.e. Timestamp encoding. Pinned so the promotion is deliberate, not a surprise.
            var model = RuntimeTypeModel.Create();
            var schema = model.GetSchema(typeof(WellKnownPromotes), ProtoSyntax.Proto3);
            Assert.Contains(".google.protobuf.Timestamp", schema);
        }
```

Verify the hex literals against actual output before pinning: for any that were derived rather than copied from an existing test (`ExplicitWins`, `MapUntouched`, `RepeatedMembersKeyOnTheElementType`), first run with a deliberately-wrong literal, read the actual hex from the failure message, decode it by hand to confirm it is the *expected* encoding (zigzag/fixed32/string-guid as commented), then pin it.

- [ ] **Step 2: Run to verify failure** — the injection-dependent wire tests fail (the default is not applied anywhere yet); `MapValuesDoNotTakeTheDefault`, `NullWrappedMembersAreExemptFromInjection`, `Level200IgnoresAFixedGuidDefault`, and `ExplicitMemberFormatBeatsTheDefault`'s field-2 half pass trivially. That is expected: they exist to *stay* green as the hook lands.

- [ ] **Step 3: Implement the hook.** In `MetaType.ApplyDefaultBehaviour(bool isEnum, ProtoMemberAttribute normalizedAttribute)`, the method currently reads (~L1234):

```csharp
var memberCompatibility = TypeCompatibilityHelper.GetMemberCompatibilityLevel(member, CompatibilityLevel);
var repeated = model.TryGetRepeatedProvider(effectiveType, memberCompatibility);
```

Insert after those two lines:

```csharp
// the cross-cutting per-type default, applied only where the member states no format itself.
// Maps keep their own [ProtoMap] formats, and null-wrapped members would throw on any
// non-default format, so both are exempt
var dataFormat = normalizedAttribute.DataFormat;
if (dataFormat == DataFormat.Default
    && (repeated is null || !repeated.IsMap)
    && GetAttribute(attribs, "ProtoBuf.NullWrappedValueAttribute") is null
    && GetAttribute(attribs, "ProtoBuf.NullWrappedCollectionAttribute") is null)
{
    var scalarType = repeated?.ItemType ?? effectiveType;
    scalarType = Nullable.GetUnderlyingType(scalarType) ?? scalarType;
    dataFormat = TypeDataFormatHelper.GetTypeDataFormat(Type, scalarType);
}
```

and change the `ValueMember` construction (~L1270) to pass `dataFormat` where it currently passes `normalizedAttribute.DataFormat`. Notes for the implementer: `attribs` is the member's `AttributeMap[]` already in scope in this method (the `ProtoMapAttribute` read at ~L1304 uses it); if it is created *after* the insertion point, hoist that creation above the hook rather than creating it twice. `Type` (the `MetaType`'s contract type) is the declaring-type argument — for a surrogated contract this is the surrogate's `MetaType`, which matches where ref-emit reads member attributes.

- [ ] **Step 4: Run the tests**

Run: `dotnet test src/protobuf-net.Test/protobuf-net.Test.csproj --filter "FullyQualifiedName~ProtoDataFormatTests"`
Expected: PASS, all of them.

- [ ] **Step 5: Run the full runtime suite** to prove nothing regressed (the hook touches every attribute-built member):

Run: `dotnet test src/protobuf-net.Test/protobuf-net.Test.csproj`
Expected: PASS with the suite's pre-existing skip count and no new failures. Run it once on the base commit first if the baseline is unknown.

- [ ] **Step 6: Commit**

```bash
git add src/protobuf-net/Meta/MetaType.cs src/protobuf-net.Test/ProtoDataFormatTests.cs
git commit -m "A member that states no DataFormat takes the per-type default; maps and null-wrapping are exempt

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: Assembly/module scope — the satellite test project

**Files:**
- Create: `src/protobuf-net.TestDataFormat/protobuf-net.TestDataFormat.csproj`
- Create: `src/protobuf-net.TestDataFormat/WithModuleLevel.cs`
- Modify: `src/protobuf-net.Test/protobuf-net.Test.csproj` (ProjectReference), `protobuf-net.slnx` (register beside `protobuf-net.TestCompatibilityLevel` at L108-110)
- Modify: `src/protobuf-net.Test/ProtoDataFormatTests.cs` (consume it)

The precedent is `src/protobuf-net.TestCompatibilityLevel/` verbatim: an isolated `netstandard2.0` assembly whose whole point is carrying assembly- and module-level attributes without poisoning any shared test assembly. CI picks the new project up automatically (`Build.csproj` globs `src\*\*.csproj`).

- [ ] **Step 1: Write the project.** Csproj (copy of `protobuf-net.TestCompatibilityLevel.csproj` with the names changed):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <RootNamespace>ProtoBuf.Test.TestDataFormat</RootNamespace>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <IsTestProject>false</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\protobuf-net.Core\protobuf-net.Core.csproj" />
  </ItemGroup>
</Project>
```

`WithModuleLevel.cs` — both scopes at once, so module-beats-assembly is pinned exactly as `TestCompatibilityLevel` pins it:

```csharp
using ProtoBuf;
using System;

[assembly: CompatibilityLevel(CompatibilityLevel.Level300)]
[assembly: ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
[assembly: ProtoDataFormat(typeof(int), DataFormat.ZigZag)]
[module: ProtoDataFormat(typeof(int), DataFormat.FixedSize)]
// the module declaration should win for int; the assembly's Guid declaration stands

namespace ProtoBuf.Test.TestDataFormat
{
    [ProtoContract]
    public class AssemblyScopedFormats
    {
        [ProtoMember(1)] public Guid Guid { get; set; }
        [ProtoMember(2)] public int Int32 { get; set; }
    }
}
```

- [ ] **Step 2: Wire it up** — `<ProjectReference Include="..\protobuf-net.TestDataFormat\protobuf-net.TestDataFormat.csproj" />` in `protobuf-net.Test.csproj` (beside the TestCompatibilityLevel one at L53), and a `<Project Path="src/protobuf-net.TestDataFormat/protobuf-net.TestDataFormat.csproj">` entry in `protobuf-net.slnx` matching the TestCompatibilityLevel entry's shape.

- [ ] **Step 3: Write the failing tests** — append to `ProtoDataFormatTests`:

```csharp
        [Fact]
        public void AssemblyScopedGuidDefaultApplies()
            => Assert.Equal(DataFormat.FixedSize, TypeDataFormatHelper.GetTypeDataFormat(
                typeof(ProtoBuf.Test.TestDataFormat.AssemblyScopedFormats), typeof(Guid)));

        [Fact]
        public void ModuleBeatsAssembly()
            => Assert.Equal(DataFormat.FixedSize, TypeDataFormatHelper.GetTypeDataFormat(
                typeof(ProtoBuf.Test.TestDataFormat.AssemblyScopedFormats), typeof(int)));

        [Fact]
        public void AssemblyScopedGuidGoesOutFixed() => AssertPayload(
            new ProtoBuf.Test.TestDataFormat.AssemblyScopedFormats { Guid = s_KnownGuid },
            "0A-10-C4-16-E4-AF-45-5E-41-4C-94-8C-F2-78-73-26-35-47");
```

- [ ] **Step 4: Run** — the two helper tests pass already if Task 2 was correct (the helper reads real attributes); `AssemblyScopedGuidGoesOutFixed` proves the end-to-end path. All three green.

Run: `dotnet test src/protobuf-net.Test/protobuf-net.Test.csproj --filter "FullyQualifiedName~ProtoDataFormatTests"`

- [ ] **Step 5: Commit**

```bash
git add src/protobuf-net.TestDataFormat src/protobuf-net.Test protobuf-net.slnx
git commit -m "Assembly and module scope proven in isolation, where they cannot re-format anyone else's fixtures

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: Generator port + golden/differential fixture

**Files:**
- Modify: `src/protobuf-net.BuildTools/Generators/ProtoModelGenerator.Parse.cs`:
  - constant beside L26 (`CompatibilityLevelAttributeName`): `private const string ProtoDataFormatAttributeName = "ProtoBuf.ProtoDataFormatAttribute";`
  - `GetDataFormatDefault` + `GetDeclaredFormat` beside `GetCompatibilityLevel` (L1886)
  - injection at the member-parse site, immediately **before** the compatibility-level block at L1130-1141 (the level computation at L1139 already consumes `dataFormat`, and the ZigZag-on-BCL refusal at L1147 and Group-on-scalar-collection refusal at L1186 must see the effective value)
  - tolerance: add `ProtoDataFormatAttributeName` beside `CompatibilityLevelAttributeName` in `IsSignificantAttribute`'s case list (L2348-2357)
- Create: `src/BuildToolsUnitTests/Aot/Data/FormatDefault.input.cs`

**Interfaces:**
- Produces: `static ProtoDataFormat? GetDataFormatDefault(Compilation, INamedTypeSymbol contract, ITypeSymbol scalarType)` — null when nothing is declared or the declared value is unknown to `GetDataFormat`.
- Consumes: `GetDataFormat(int)` (L2317 — **the** conversion, never a cast), `Qualified`, the member-parse locals at ~L1100-1141 (`dataFormat`, `memberSource`, `shape`, the null-wrapping locals at ~L1107).

- [ ] **Step 1: Write the failing fixture**

```csharp
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

// NOTE: no .reference.cs yet - added on Linux, and AotRefGen is net472 so it could not be run.
// Nothing here is refused by ref-emit, so this fixture *should* have one. Differentially covered
// by AotConformanceTests - and unlike [ProtoSurrogate]/[ProtoSerializer] there is no replay: the
// runtime model honours [ProtoDataFormat] itself, so the differential asserts real JIT/AOT parity.
// Run AotRefGen on Windows and commit the result.
//
// Type-scoped deliberately: an assembly-scoped declaration would re-format every fixture in the
// linked assembly - the same trap AGENTS.md records for [module: CompatibilityLevel]. Assembly and
// module scope are proven by protobuf-net.TestDataFormat + the satellite tests instead.
namespace AotFixtures.FormatDefault;

[DataContract, CompatibilityLevel(CompatibilityLevel.Level300)]
[ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
[ProtoDataFormat(typeof(int), DataFormat.ZigZag)]
public class Payment
{
    // WCF-style members: no per-member format is expressible here, which is the whole point
    [DataMember(Order = 1)] public Guid Id { get; set; }
    [DataMember(Order = 2)] public Guid? Correlation { get; set; }
    [DataMember(Order = 3)] public List<Guid> Batch { get; set; }
    [DataMember(Order = 4)] public int Amount { get; set; }
    // an explicit [ProtoMember] format beats the type default ([ProtoMember] mixes with the
    // [DataMember] family; it wins for its own member while [DataMember] supplies the rest)
    [ProtoMember(5, DataFormat = DataFormat.FixedSize)] public int Stated { get; set; }
    // a map value does not take the default: [ProtoMap(ValueFormat)] is the tool there
    [DataMember(Order = 6)] public Dictionary<int, Guid> ById { get; set; }
}

public static class FormatDefaultSamples
{
    public static object[] Values =>
    [
        new Payment(),
        new Payment { Id = Guid.Parse("c416e4af-455e-414c-948c-f27873263547"), Amount = -3 },
        new Payment
        {
            Correlation = Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00"),
            Batch = [Guid.Parse("00112233-4455-6677-8899-aabbccddeeff")],
            Stated = -7,
            ById = new() { { 2, Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee") } },
        },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Payment))]
public partial class FormatDefaultModel : TypeModel
{
}
```

- [ ] **Step 2: Run the golden test twice** — the pre-implementation golden shows `Id` emitted via `WriteGuidString` and `Amount` as a plain varint. Don't commit; it exists to show the delta.

- [ ] **Step 3: Implement the generator side.**

3a. The walk, beside `GetCompatibilityLevel`:

```csharp
/// <summary>
/// The cross-cutting DataFormat default for a scalar type: the contract type (walking base
/// types), then the module, then the assembly — a sibling of GetCompatibilityLevel, keyed per
/// scalar type because [ProtoDataFormat] is AllowMultiple.
/// </summary>
private static ProtoDataFormat? GetDataFormatDefault(
    Compilation compilation, INamedTypeSymbol contract, ITypeSymbol scalarType)
{
    var key = Qualified(compilation, scalarType);
    for (INamedTypeSymbol? current = contract; current is not null; current = current.BaseType)
    {
        if (GetDeclaredFormat(compilation, current, key) is { } declared)
        {
            return GetDataFormat(declared); // never a cast: the ordinals differ
        }
    }
    if (GetDeclaredFormat(compilation, compilation.SourceModule, key) is { } fromModule)
    {
        return GetDataFormat(fromModule);
    }
    if (GetDeclaredFormat(compilation, compilation.Assembly, key) is { } fromAssembly)
    {
        return GetDataFormat(fromAssembly);
    }
    return null;
}

private static int? GetDeclaredFormat(Compilation compilation, ISymbol symbol, string scalarKey)
{
    foreach (var attribute in symbol.GetAttributes())
    {
        if (attribute.AttributeClass?.ToDisplayString() != ProtoDataFormatAttributeName) continue;
        if (attribute.ConstructorArguments.Length != 2) continue;
        if (attribute.ConstructorArguments[0].Value is not ITypeSymbol type) continue;
        if (attribute.ConstructorArguments[1].Value is not int format) continue;
        if (Qualified(compilation, type) == scalarKey) return format;
    }
    return null;
}
```

3b. Injection at the member parse, before the L1130 compatibility block. The scalar type to key on is the same one whose `Kind` the parse computed: the element type for a repeated member, the member's own type otherwise, after `Nullable<T>` unwrap — the member parse already holds this symbol (it is what `GetMemberShape` classified); reuse it rather than re-deriving. Skip when the member is a map (`shape.Map.Factory is not null`) or carries null-wrapping (the locals parsed at ~L1107 are in scope):

```csharp
// the cross-cutting per-type default: only where the member states no format, never for maps
// (whose per-side formats belong to [ProtoMap]) and never for null-wrapped members (protobuf-net
// throws on that combination, and ambient defaults must not newly break them)
if (dataFormat == ProtoDataFormat.Default
    && shape.Map.Factory is null
    && !nullWrappedValue && !nullWrappedCollection)
{
    if (GetDataFormatDefault(compilation, memberSource, scalarType) is { } ambient)
    {
        dataFormat = ambient;
    }
}
```

(`nullWrappedValue`/`nullWrappedCollection`/`scalarType` name the locals the file actually uses — read the surrounding code and match its spellings; the semantics above are what matters. `memberSource` is the contract type, or the surrogate type when one is present — already correct for this purpose.)

3c. Add `ProtoDataFormatAttributeName` to the tolerated list in `IsSignificantAttribute` (beside `CompatibilityLevelAttributeName`). If the golden run then still drops `Payment` because of the *type-level* attribute, grep `CompatibilityLevelAttributeName` in Parse.cs for the type-attribute tolerance site and add it there too — the fixture is the detector.

- [ ] **Step 4: Run the golden test twice, review the diff** — `Id`/`Correlation` now emit `WriteGuidBytes`/`ReadGuidBytes`, `Batch`'s element serializer takes the FixedSize form, `Amount` reads/writes zigzag (`SignedVarint` hint on the read), `Stated` is fixed32, and the `ById` map value stays `GuidString`.

- [ ] **Step 5: Run the differential**

Run: `dotnet test src/AotConformanceTests/AotConformanceTests.csproj`
Expected: PASS — this is the real parity assertion: the reference `RuntimeTypeModel` takes the same bytes via Task 3's hook, with **no replay code anywhere**.

- [ ] **Step 6: Run the corpus** (regression guard — no corpus assembly declares the attribute):

Run: the corpus command from Global Constraints. Expected: exit 0, zero mismatches.

- [ ] **Step 7: Commit**

```bash
git add -A src/protobuf-net.BuildTools src/BuildToolsUnitTests
git commit -m "The generator resolves [ProtoDataFormat] through the same walk it ports for CompatibilityLevel

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 6: AotSmoke member

**Files:**
- Modify: `src/AotSmoke/Program.cs` — a new contract class beside the others, a member on `Order` (next free tag — 58, or 60 if PR 1 landed first; check the file), population + assert in `Main`

- [ ] **Step 1: Add the contract and member**

```csharp
// [ProtoDataFormat] under native AOT: a [DataMember]-style contract whose bare Guid takes the
// 16-byte form from the type-scoped default. The inner payload is worth checking by eye: the
// guid field must be 0A-10 + 16 bytes, not 0A-24 + a 36-char string.
[ProtoContract, CompatibilityLevel(CompatibilityLevel.Level300)]
[ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
public class Ledger
{
    [ProtoMember(1)] public Guid Entry { get; set; }
}
```

On `Order`: `[ProtoMember(58)] public Ledger Ledger { get; set; }` (adjust the tag to the file's next free number). In `Main`: `Ledger = new Ledger { Entry = Guid.Parse("c416e4af-455e-414c-948c-f27873326547") }` — note: use a fresh literal guid, then `Check(ref failures, "Ledger", original.Ledger.Entry, clone.Ledger?.Entry);`.

- [ ] **Step 2: Build + run Debug, inspect the dump**

Run: `dotnet build src/AotSmoke/AotSmoke.csproj && dotnet run --project src/AotSmoke -c Debug`
Expected: PASSED; the `Ledger` sub-message in the printed hex contains `0A-10` followed by exactly 16 bytes.

- [ ] **Step 3: Re-measure the trim baseline** (clean `obj`/`bin`, `dotnet publish -c Release -r linux-x64` if the toolchain allows; otherwise record as pending Windows CI). Record the warning-count and size delta in `docs/aot-findings.md`.

- [ ] **Step 4: Commit**

```bash
git add src/AotSmoke/Program.cs docs/aot-findings.md
git commit -m "AotSmoke carries a [ProtoDataFormat] guid: 16 bytes on the wire under ILC, checked by eye

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 7: Docs, release notes, full sweep

**Files:**
- Modify: `docs/releasenotes.md`, `AGENTS.md`

- [ ] **Step 1: Release notes** entry under the next unreleased version:

```
- add `[ProtoDataFormat(type, format)]`: a per-type DataFormat default (assembly/module/type
  scoped, explicit member format wins), honoured by both the runtime model and the AOT
  generator — e.g. fixed 16-byte Guids at CompatibilityLevel 300 without editing members (#xxxx)
```

- [ ] **Step 2: AGENTS.md** — in the "Compatibility level and the BCL types" section, add a short paragraph: `[ProtoDataFormat]` resolves like the level (type → module → assembly, explicit member format wins, `Default` is the zero sentinel so "explicit Default" cannot opt out), keys on the Nullable-unwrapped scalar/element type, and deliberately never reaches maps or null-wrapped members; both the runtime (`TypeDataFormatHelper` + the `MetaType.ApplyDefaultBehaviour` hook) and the generator (`GetDataFormatDefault` beside `GetCompatibilityLevel`) honor it, so the differential covers it with no replay. Note the fixture-assembly trap applies to it exactly as to `[module: CompatibilityLevel]`.

- [ ] **Step 3: Full sweep** — runtime suite, golden suite, differential, corpus, smoke build+Debug run. All green, `git status` shows no unexpected golden drift.

- [ ] **Step 4: Commit + open the PR** (fork `main` first; upstream issue + PR per the spec's Delivery section, filed independently of PR 1)

```bash
git add docs/releasenotes.md AGENTS.md
git commit -m "Docs: [ProtoDataFormat] rides the CompatibilityLevel machinery on both sides

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```
