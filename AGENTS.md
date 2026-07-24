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
- Every dropped contract must **say why**: `PBN2001` unsupported member, `PBN2002` unsupported
  declaration, `PBN2003` unsupported protobuf-net option, `PBN2004` dropped by cascade. All are
  **warnings**, not errors — an incomplete model still builds, and the runtime "no serializer" throw
  is the backstop; erroring would make the generator unusable while coverage is partial. Anyone
  wanting strictness can escalate via `WarningsAsErrors`.
- **C# 12 is a hard floor.** Below it the generator reports `PBN2000` and emits nothing, rather than
  emitting code that won't compile. Do not add down-level fallbacks: supporting multiple language
  versions multiplies every emitted construct for no benefit to anyone doing AOT. (netstandard2.0
  and net4x default to C# 7.3, so those consumers must set `<LangVersion>` — accepted deliberately.)

AOT generator diagnostics use their own **`PBN2000+`** block: `PBN0001`–`PBN0016` belong to
`DataContractAnalyzer` and `PBN1000+` to `ProtoFileGenerator`'s schema errors. New IDs must be added
to `AnalyzerReleases.Unshipped.md` or RS2000 fires at build time.

Note the shipped analyzer still compiles against the low Roslyn baseline (4.3.1), which predates
`LanguageVersion.CSharp12` — hence the numeric constant in `ProtoModelGenerator`. `BuildToolsUnitTests`
carries a `VersionOverride` to 4.8.0 purely so its in-memory compilations can parse C# 12.

**Don't rev the central Roslyn version speculatively.** Only bump it if we genuinely need a modern
Roslyn API we cannot work around — e.g. detecting a language feature we actually use. The old
baseline is what lets `protobuf-net.BuildTools.Legacy` serve very old SDKs; those users are not doing
AOT by definition, so none of the AOT generator's requirements apply to them.

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
