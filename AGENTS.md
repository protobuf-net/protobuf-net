# protobuf-net — notes for agents

Only non-obvious things live here; the code is the reference for everything else.

## Layout and build

- The solution is **`protobuf-net.slnx`**. `protobuf-net.sln.old` is a stale leftover — don't use it.
- CI (`.github/workflows/dotnet.yml`) runs on **windows-latest** and builds/tests **`Build.csproj`**,
  a `Microsoft.Build.Traversal` project. It globs `src\*\*.csproj`, so a new project under `src/`
  is picked up by CI automatically — including `net4x`-only ones, which are fine because CI is Windows.
- **Central package management is on.** Add versions to `src/Directory.Packages.props`; leave
  `Version=` off the `PackageReference` in the csproj.

## protobuf-net.BuildTools compiles in its dependencies

`src/protobuf-net.BuildTools/protobuf-net.BuildTools.csproj` does not *reference*
protobuf-net.Core / protobuf-net.Reflection — it **compiles their sources in**
(`<Compile Include="../protobuf-net.Core/**/*.cs" />`), because package references inside analyzers
are painful.

Consequence worth knowing in tests: `typeof(TypeModel).Assembly` resolves to the **BuildTools**
assembly, not protobuf-net. That is deliberate and is what `MetadataReferenceHelpers` relies on.

## Persist-to-dll is .NET Framework only

`RuntimeTypeModel.Compile(string name, string path)` is inside `#if !PLAT_NO_EMITDLL`, and
`PLAT_NO_EMITDLL` is defined for every TFM **except `net462`** (`src/protobuf-net/protobuf-net.csproj`).
On modern TFMs `CompilerOptions.OutputPath` is `[Obsolete]` and a non-empty path throws
`NotSupportedException`. The save path still uses `AssemblyBuilderAccess.RunAndSave`;
`PersistedAssemblyBuilder` (.NET 9+) is not used.

**The trap:** `src/Examples/CoreFxHacks.cs` defines, under `#if COREFX`, an extension method
`Compile(this RuntimeTypeModel, string x, string y) => model.Compile();` that silently discards the
path. So `src/Examples` calls `model.Compile("X", "X.dll")` and compiles cleanly on `net8.0` while
writing no dll at all. Don't infer from that code that persistence works on modern .NET.

If a new TFM is ever needed, prefer **net10.0** (LTS) over net9.0.

## AOT source generator (work in progress)

`ProtoModelGenerator` (`src/protobuf-net.BuildTools/Generators/ProtoModelGenerator.cs`) is an
`IIncrementalGenerator` building compile-time serializers for code-first contracts. It is
deliberately separate from `ProtoFileGenerator` (the `.proto` → DTO path, still `ISourceGenerator`).

Design constraints that are settled, and should not be quietly relaxed:

- The consumer opts in with `[ProtoModel] partial class MyModel : TypeModel`, seeded by
  `[ProtoSerializable(typeof(Foo))]`; everything reachable from a seed is pulled in automatically.
- The trigger attributes are **generator-owned**, emitted via `RegisterPostInitializationOutput`,
  not declared in protobuf-net.Core.
- The model is **closed over what is visible at compile time**. It never consults the runtime model.
  A contract the generator can't handle gets a diagnostic and is omitted — it must *not* fall back
  to ref-emit, since that would silently defeat AOT. `TypeModel`'s inherited "no serializer for
  type X" throw is the intended backstop.
- Dropping a contract must **cascade to its referrers** (`DropUnsatisfiable`, run to a fixed point).
  A contract whose member type was dropped would emit `ReadMessage<T>(..., this)` for a `T` the
  services type does not implement `ISerializer<T>` for, which does not compile.
- **Nothing in `Internal/Aot/` may hold a Roslyn reference** — no `ISymbol`, `Location`, `SyntaxNode`,
  `Compilation`. Doing so causes two silent failures at once: equality becomes reference-based so the
  cache never hits, *and* the model pins the whole compilation graph alive for as long as the driver
  holds it, which is a serious leak in a long-running IDE session. This has sunk previous attempts at
  this work. Roslyn *value* types (`TextSpan`, `LinePositionSpan`) are fine — they are plain data —
  which is why `PlanLocation` stores those and reconstitutes a `Location` only at report time.
  `ProtoModelPlanShapeTests` enforces this by reflection, scoped to the `Internal/Aot` namespace.
  (Analyzer helpers in `Internal/` proper *do* legitimately hold symbols; analyzers aren't cached
  across compilations. That is why the model types have their own namespace.)
- The model types are hand-written equatable values so the driver can cache them; `ImmutableArray<T>`
  is deliberately *not* used, as its equality is reference-based and would defeat caching silently.
  `ProtoModelGeneratorIncrementalTests` asserts both directions: cached on irrelevant edits, and
  *not* cached on real ones, so the test is known to be able to fail.
- Diagnostics are projected through a **separate** `Select` from the plan, because they carry
  locations (which shift whenever anything above them moves) and the plan does not — so the emit
  step stays cached across edits that only move code around.
- **Not everything that changes the wire format is a `ProtoBuf` attribute.** `MetaType.ApplyDefaultBehaviour`
  also honours `System.Runtime.Serialization` (`[DataContract]`/`[DataMember]`, and the
  `[OnDeserialized]` callback family — which live on *methods*), `System.Xml.Serialization`,
  `[NonSerialized]`, and `[DefaultValue]` (which changes the write guard from `!= 0` to `!= default`).
  Worse, the `{Name}Specified` / `ShouldSerialize{Name}()` conventions are matched **by name**, so no
  attribute inspection finds them at all. `IsSignificantAttribute` and `GetConditionalPattern` exist
  to bail on all of these; anything added to `MetaType`'s list must be added there too, or the
  generator will silently emit wrong bytes.
- **Auto-tuples** are a second emit shape, not a variation on the first. The read declares a local
  per constructor parameter (seeded from the incoming value so merge still works), reads into those,
  and calls the constructor at the *end*; the write emits every scalar **unconditionally** — with
  construction-time assignment there is no way to tell "absent" from "default" — and skips
  `ThrowUnexpectedSubtype`. Field numbers are 1..n in constructor-parameter order.

  Detection mirrors `MetaType.ResolveTupleConstructor` and must not drift from it. It engages **only
  when the type carries no contract attribute family at all** (`MetaType.GetContractFamily`) — a
  `[ProtoContract]` on an immutable type *defeats* detection and makes ref-emit produce a serializer
  that finds no members and throws `ThrowCannotCreateInstance`; we drop such types instead, which is
  deliberately better than matching. The rule is "no *public* setter", not "immutable": non-public
  and `init`-only setters are both tolerated, and any type with **"Tuple" in its name** is exempt
  from the read-only demand entirely — which is the only reason `ValueTuple`'s public mutable fields
  qualify. Members include fields as well as properties, the constructor is matched by parameter
  name (case-insensitive) with exact type equality, and **exactly one** constructor may map.
  Closed constructed generics are supported, since `KeyValuePair<K,V>` is the common case.

  `ValueTuple` needs two Roslyn-specific allowances, both found the hard way: its `Item1`/`Item2`
  fields are reported as **`IsImplicitlyDeclared`**, so a filter for that (intended to skip
  auto-property backing fields — which the public-accessibility test already excludes) leaves it with
  no members at all; and its name renders as `(int, string)`, so it must be built with a **tuple
  literal** — `new (int, string)(...)` is not legal C#. Note `TupleUnderlyingType` is *null* for
  these symbols, so it is no help in normalising them.

  Tuple-typed **members** are supported as sub-messages even though they carry no contract
  attribute: `GetMemberShape` falls through to `IsTupleCandidate`, and the closure handles the rest.

  **Element names are accepted from consumers but erased in our output** (`EraseTupleNames`, applied
  recursively and also through enclosing generics such as `KeyValuePair<int, (int A, int B)>`).
  A consumer writing `public (int Id, string Name) Pair { get; set; }` works — be gracious in what we
  accept; erasure governs only what *we* key on and emit. It is free, because the conversion between
  tuple types differing only in names is an *identity* conversion, so `value.Named` passes to
  `WriteMessage<(int, string)>(...)` with no cast.

  Erasure is not cosmetic, and all three reasons were confirmed by probing the symbols rather than
  assumed:
  - **Detection**: a *named* tuple reports four public fields (`Item1, Id, Item2, Name`), which fails
    the constructor-arity match; the erased form reports the two we want.
  - **De-duplication**: `SymbolEqualityComparer.Default` returns **false** between the two spellings,
    and `ToDisplayString` includes the names — so the same shape named two ways would emit
    `ISerializer<(int, string)>` *twice* and fail to compile.
  - **Alignment**: ref-emit works in metadata where names do not exist, so it collapses them for
    free; erasing keeps our serializer set identical to its.
- **Value types are first-class contracts.** A struct needs no construction or null test on read, and
  no `ThrowUnexpectedSubtype` on write (that is constrained to reference types). A struct-typed
  *member* is never null, so neither side tests for it — and unlike a reference-type message,
  `Nullable<TStruct>` **is** expressible and uses `HasValue`/`GetValueOrDefault`.
- **Field numbers have three sources**, in the precedence `MetaType.ApplyDefaultBehaviour` uses:
  `[ProtoMember]`, then `[DataMember(Order)]`, then `[XmlElement(Order)]`/`[XmlArray(Order)]`.
  `[DataContract]` and `[XmlType]` are contract markers in their own right, and the two families can
  be mixed on one type — `[ProtoMember]` wins for its own member while `[DataMember]` supplies the
  rest. `DataMemberOffset` applies **only** to the `[DataMember]` orders, never to the Xml ones. An
  order below 1 means "not declared" (`DataMember.Order` defaults to -1). `[XmlIgnore]` and
  `[NonSerialized]` exclude a member rather than dropping the contract.
- `[ProtoContract(SkipConstructor = true)]` constructs via
  `BclHelpers.GetUninitializedObject(typeof(T))` and additionally implements `IFactory<T>`. Its
  effect is only visible in non-serialized members, which is why the differential tests compare
  simply-typed properties as well as bytes.
- An **enum** is its underlying scalar plus a cast in each direction, compared against
  `default(TEnum)`. `[Flags]` makes no difference to the wire form, and `[ProtoEnum]` only renames
  for schema purposes (`ProtoEnumAttribute.Value` is `[Obsolete(..., error: true)]`), so neither
  needs guarding against. Older protobuf-net supported enum value-aliasing and validity-checking
  (auto-disabled by `[Flags]`, which is why that looks worth testing); that was simplified away in
  line with .proto, and is now *unreachable* rather than merely deprecated — `EnumPassthru`'s setter
  throws on both `ProtoContractAttribute` and `MetaType`. Don't reintroduce support for it. **char** is a `ushort` varint, needing an explicit cast on read. Note the
  CLR permits char-backed enums even though C# cannot declare one (CS1008) — those are refused,
  since the shape cannot be tested from C#.
- Write guards, all three proven against ref-emit rather than assumed: a plain scalar is written when
  `!= <type default>`; a `[DefaultValue]` scalar when `!= <declared>`; a `Nullable<T>` when
  `HasValue` — **presence, not value**, so a nullable zero *is* written where a plain zero is not.
  The two compose by nesting (`if (HasValue) { if (v != declared) ... }`), and `[DefaultValue(null)]`
  means "no declared default". `[DefaultValue]` is write-only — the reader never applies it, so a
  declared default without a matching initialiser is lossy across a round-trip (protobuf-net
  behaviour generally; `PBN0020`/`PBN0021` exist to nag about it).
- Every dropped contract must **say why**: `PBN2001` unsupported member, `PBN2002` unsupported
  declaration, `PBN2003` unsupported protobuf-net option, `PBN2004` dropped by cascade. All are
  **warnings**, not errors — an incomplete model still builds, and the runtime "no serializer" throw
  is the backstop; erroring would make the generator unusable while coverage is partial. Anyone
  wanting strictness can escalate via `WarningsAsErrors`.
- **C# 12 is a hard floor.** Below it the generator reports `PBN2000` and emits nothing, rather than
  emitting code that won't compile. Do not add down-level fallbacks: supporting multiple language
  versions multiplies every emitted construct for no benefit to anyone doing AOT. (netstandard2.0
  and net4x default to C# 7.3, so those consumers must set `<LangVersion>` — accepted deliberately.)

AOT generator diagnostics use their own **`PBN2000+`** block: `PBN0001`–`PBN0022` belong to
`DataContractAnalyzer` and `PBN1000+` to `ProtoFileGenerator`'s schema errors. New IDs should be
added to `AnalyzerReleases.Unshipped.md` — note that release tracking is not actually *enforced*
here (the `Microsoft.CodeAnalysis.Analyzers` RS2000 rules are not active), so the table is
documentation rather than a build gate, and it has drifted: `PBN0020`–`PBN0022` are missing from it.

Note the shipped analyzer still compiles against the low Roslyn baseline (4.3.1), which predates
`LanguageVersion.CSharp12` — hence the numeric constant in `ProtoModelGenerator`. `BuildToolsUnitTests`
carries a `VersionOverride` to 4.8.0 purely so its in-memory compilations can parse C# 12.

**Don't rev the central Roslyn version speculatively.** Only bump it if we genuinely need a modern
Roslyn API we cannot work around — e.g. detecting a language feature we actually use. The old
baseline is what lets `protobuf-net.BuildTools.Legacy` serve very old SDKs; those users are not doing
AOT by definition, so none of the AOT generator's requirements apply to them.

### Collections

`T[]` and `List<T>` of scalars and messages, via `RepeatedSerializer.CreateVector<T>()` /
`CreateList<T>()`. Read uses the same merge shape as sub-messages (existing collection passed in,
result assigned back only when non-null). Facts confirmed against ref-emit rather than assumed:

- **Packing is a compile-time decision.** The features constant carries `OptionPackedDisabled`, and
  ref-emit simply *omits* it for `[ProtoMember(IsPacked = true)]`. Unpacked is the default; that
  named argument is not supported yet, so we always emit the disabled form.
- The features wire type is the **element's**, not the member's.
- A message element passes `this` as the sub-serializer; a scalar element passes nothing.
- **A repeated enum is refused.** Unlike inline scalars, `RepeatedSerializer` resolves an
  `ISerializer<TEnum>` *from the model* — ref-emit emits `values, this as ISerializer<TEnum>` — and
  the generated services type does not expose one. Supporting it needs the
  `ISerializerProxy<TEnum>` + `EnumSerializer.CreateXxx<T>()` pattern that
  `Internal/CustomProtogenSerializer.cs` uses. Until then it is dropped at build time rather than
  failing with "no serializer for type" at runtime.
- protobuf-net **rejects null elements** inside a collection (`ThrowNullRepeatedContents`), so
  fixtures must not contain them.

Collection options are pure features composition, and compose orthogonally:
`IsPacked = true` *omits* `OptionPackedDisabled`; `OverwriteList = true` *adds*
`OptionClearCollection`. Both are refused on a non-collection member, where they mean nothing.

`DataFormat` and `IsRequired` change the emitted *shape*, not just the features:

- **`DataFormat`** selects the wire type. On a scalar that means `WriteFieldHeader(n, WireType.X)` +
  the wire-type-aware `WriteInt32`/`WriteInt64` — **not** the `WriteInt32Varint` shortcut, which
  writes its own varint header and therefore only applies at the default format. `FixedSize` picks
  `Fixed32`/`Fixed64` from the *member's* width. On a repeated member it is only a features swap.
- **`ZigZag` reads need `state.Hint(WireType.SignedVarint)` before the read**; no other format does.
- **`Group`** differs on the **write only** (`WriteGroup` for `WriteMessage`); its read is an
  ordinary `ReadMessage`.
- **`TwosComplement` is byte-identical to `Default`** for every type we handle, so it maps onto it.
- **`IsRequired`** drops the write guard so the member is always written. It is only observable for
  value-type scalars — reference types were already unguarded on write — and it does **not** affect
  the read: a required string still keeps its `if (x != null)` on the way back in. The emitter omits
  the test entirely rather than emitting `if (true)`.

`WellKnown` is still refused.

Note `ListSet` and `RepeatedAsList` are **protogen schema-codegen** options that shape generated DTOs
from `.proto`; they are not `[ProtoMember]` options and have nothing to do with this generator.

### Not yet supported

Dropped with a diagnostic rather than mis-emitted; roughly in expected order of difficulty:

- **`init`-only accessors** — currently `PBN2001`. Note these cannot be assigned after construction,
  so they likely have to route through the constructor/tuple path rather than the property-setter one.
- collections and maps (`RepeatedSerializer`, packed vs unpacked, `ListSet`/`RepeatedAsList`)
- the compatibility-level BCL types (`DateTime`/`TimeSpan`/`decimal`/`Guid`)
- inheritance / `[ProtoInclude]`, surrogates, serialization callbacks, fields-as-members,
  `ShouldSerialize`/`Specified`

### Golden-file tests

`src/BuildToolsUnitTests/Aot/` pairs each `Data/*.input.cs` with the exact code it generates
(`*.output.cs`) and the diagnostics it reports (`*.txt`).

**The tests rewrite those goldens in the source tree on every run**, then assert. So:

- a new fixture fails on its first run (nothing to compare against) — re-run, then review `git diff`;
- a behaviour change shows up as a diff to read, not an assertion to appease;
- don't hand-edit a golden to make a test pass — fix the generator and re-run.

`Data/*.cs` files are excluded from compilation via `<Compile Remove="Aot/Data/**/*.*.cs" />` and
copied to the output directory instead.

Fixture conventions:

- **One namespace per fixture** (`namespace AotFixtures.Simple;`). Every fixture is linked into a
  single assembly by both `AotRefGen` and `AotConformanceTests`, so unqualified names would collide.
- `<Name>.input.cs` declares model type `<Name>Model`, and may declare `<Name>Samples.Values`
  (a `public static object[]`) supplying the values the differential tests exercise.
- A sibling `<Name>.langver` file pins the parse language version for that fixture — used to prove
  the `PBN2000` floor fires.
- **A fixture member with `[DefaultValue(x)]` must also be initialised to `x`.** `[DefaultValue]`
  affects writing only, so without the initialiser an empty payload deserializes to the CLR default
  and the round-trip assertion fails — correctly. This has caught out two fixtures so far; it is a
  fixture-authoring rule, not a generator limitation.
- `Data/Diagnostics/**` holds fixtures that exist to produce diagnostics rather than working code.
  The golden tests glob recursively; `AotRefGen` and `AotConformanceTests` deliberately glob only
  `Data/*.input.cs`, so these are excluded from both.

### Differential tests

`src/AotConformanceTests` references the generator as an **analyzer** (`OutputItemType="Analyzer"`)
and links `Data/*.input.cs`, so the generator runs for real during that project's build. It compiles
only `*.input.cs` — never `*.output.cs`, which is a test artefact, and compiling it would mean
testing a stale snapshot instead of the current generator.

Each sample is checked four ways: bytes from the generated model vs `RuntimeTypeModel` must match,
and each model must read what the other wrote. The cross-deserialization is the point — a serializer
that consistently writes the wrong field number round-trips against itself perfectly. Equivalence is
asserted by re-serializing with the reference model rather than via a hand-written deep comparer.

`RepeatedFieldOccurrencesMergeIdentically` covers what round-tripping structurally cannot: merge
behaviour on **repeated occurrences of the same field**. Serialization never emits a duplicated
field, so `AppendBytes` (which *concatenates* byte arrays) and `ReadMessage`'s merge-into-existing
are otherwise untested. It concatenates every sample's payload — itself a valid protobuf message —
to manufacture the duplicates.

### Native AOT smoke test

`src/AotSmoke` is a `PublishAot` console app that round-trips through a generated model and returns
a non-zero exit code on mismatch. It is the only thing here that proves the actual goal; everything
else runs on a JIT runtime where ref-emit still exists.

```
dotnet publish src/AotSmoke/AotSmoke.csproj -c Release -r win-x64
```

`vswhere.exe` must be on `PATH` (`%ProgramFiles(x86)%\Microsoft Visual Studio\Installer`) or ILC's
link step fails with a mangled command line — the error names `link.exe`, which is misleading.

Because `PublishAot` enables trim/AOT analysis at **build** time too, an ordinary `dotnet build` of
this project catches annotation regressions without paying for a native publish. That is how IL2095
was found. Two things it has already caught, both invisible on JIT:

- The generated `GetSerializer<T>` override must restate the base's
  `[DynamicallyAccessedMembers(DynamicAccess.ContractType)]` exactly, or IL2095 fires. `DynamicAccess`
  is internal to protobuf-net, so the emitter spells the flags out — keep them in step with
  `protobuf-net.Core/Internal/DynamicallyAccessedMembersAttribute.cs`, and note the attribute only
  exists on net5+, so the generator probes for it rather than assuming.
- `SerializerCache.Get<TProvider, T>` had **no** annotations while the `SerializerCache<TProvider>`
  it forwards to needs `DynamicAccess.Serializer` to preserve the constructor used by
  `Activator.CreateInstance`. The chain broke at that public boundary, ILC trimmed the constructor,
  and the first serialize threw `MissingMethodException` at runtime. This affected *any* hand-written
  `TypeModel` under AOT, not just generated ones.

### Trim/AOT annotations: which axis they belong on

`[DynamicallyAccessedMembers]` says *"someone will reflect over this T"*. That is a property of **how
a serializer is obtained** (the reflection-based `RuntimeTypeModel` builds one by inspecting the
contract), **not of what a serializer is**. It was originally declared on `ISerializer<T>` and its
siblings, so every consumer paid the runtime model's cost — including generated models, which never
reflect at all. `PrimaryTypeProvider` implements `ISerializer<Type>`, and since `System.Type` is
saturated with `RequiresDynamicCode` members, that single instantiation produced ~180 warnings.

Removing it from the serializer interfaces took the `AotSmoke` publish from **200 warnings to 33**.
Do not reintroduce it there; annotate the reflection entry points instead.

`Requires*` attributes were tried on the dynamic helpers and **reverted** — they do not remove
warnings, they relocate them to callers, and the callers here are
`ProtoReader.State.DeserializeRootImpl<T>`, `TypeModel.CreateInstance<T>` and
`TypeHelper<T>..cctor()` — i.e. the *generic paths generated models use*. The reflective fallbacks
are entangled inside the AOT-safe paths rather than sitting behind a boundary, so `Requires*` has
nowhere clean to terminate. Fixing that properly means **restructuring** (the generic path must not
reference the fallback at all), not annotating.

The remaining 33 are genuinely dynamic: `MakeGenericType`/`Type.GetType`/`Array.CreateInstance` in
the runtime-model and collection paths. Measure with a publish rather than reasoning about them.

### Reference output from ref-emit

`src/AotRefGen` (net472, hence the section above) exists so the generator's expected output is
*derived* rather than guessed. It links `BuildToolsUnitTests/Aot/Data/*.input.cs` directly, runs
them through `RuntimeTypeModel`, persists the compiled model, decompiles it with
`ICSharpCode.Decompiler`, and writes `*.reference.cs` beside the fixture. Run it after adding or
changing a fixture; the tests don't consume its output, it's a reviewing aid.

`*.reference.cs` is **intentionally tracked in git** (not generated-and-ignored), so that changes in
ref-emit behaviour show up in review.

- Fixture convention: `<Name>.input.cs` declares model type `<Name>Model`, giving `<Name>.reference.cs`.
- Contract types in fixtures must be `public` — full ref-emit compilation only reaches public members.
- `src/AotRefGen/TriggerAttributes.cs` duplicates the generator's post-init attributes so the shared
  fixtures compile; keep the two in step.

Two artefacts of decompilation are cosmetic, not semantic: `Features` appears as a uniquely-named
method plus an ILSpy `.override` note (it's really an explicit-interface property), and
"Error decoding local variables" reflects ref-emit's empty locals signature.
