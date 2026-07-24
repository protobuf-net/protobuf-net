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

### Golden-file tests

`src/BuildToolsUnitTests/Aot/` pairs each `Data/*.input.cs` with the exact code it generates
(`*.output.cs`) and the diagnostics it reports (`*.txt`).

**The tests rewrite those goldens in the source tree on every run**, then assert. So:

- a new fixture fails on its first run (nothing to compare against) — re-run, then review `git diff`;
- a behaviour change shows up as a diff to read, not an assertion to appease;
- don't hand-edit a golden to make a test pass — fix the generator and re-run.

`Data/*.cs` files are excluded from compilation via `<Compile Remove="Aot/Data/**/*.*.cs" />` and
copied to the output directory instead.

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
