using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ProtoBuf.AotDifferential;

/// <summary>
/// Discovers every <c>[ProtoContract]</c> in the target assemblies, runs the AOT generator over them,
/// and compiles and loads the result so it can actually be executed.
/// </summary>
/// <remarks>
/// The generator is loaded reflectively rather than referenced — see the note in the csproj. We only
/// ever touch it through Roslyn's own interfaces, so BuildTools' copy of protobuf-net.Core and the
/// real one never have to agree about anything.
/// </remarks>
internal sealed class Corpus
{
    private const string ProtoContractAttribute = "ProtoBuf.ProtoContractAttribute";
    private const string GeneratorTypeName = "ProtoBuf.BuildTools.Generators.ProtoModelGenerator";
    public const string ModelTypeName = "DifferentialModel";

    public List<Type> Contracts { get; } = [];
    public Type ModelType { get; private set; }
    public Dictionary<string, int> Skipped { get; } = new(StringComparer.Ordinal);
    public int Seeded { get; private set; }
    public int Dropped { get; private set; }

    /// <summary>Assemblies dropped because they collide with a scanned target.</summary>
    public List<string> Ambiguous { get; private set; } = [];

    /// <summary>The <c>.proto</c> half of the corpus, or <c>null</c> if it was skipped.</summary>
    public Schemas Schemas { get; private set; }

    /// <summary>
    /// Every dll beside the targets — the same set the generator sees as references, and therefore
    /// where an assembly-level <c>[ProtoSurrogate]</c> can come from.
    /// </summary>
    public List<string> Neighbours { get; } = [];

    /// <summary>Build the corpus, or return the reason it could not be built.</summary>
    public string Build(string[] targets)
    {
        var present = targets.Where(File.Exists).ToArray();
        if (present.Length == 0) return "nothing to scan; build the target projects first";

        // every dll beside the targets, so that types the contracts reference still bind
        Neighbours.AddRange(present
            .SelectMany(static x => Directory.GetFiles(Path.GetDirectoryName(x)!, "*.dll"))
            .Distinct(StringComparer.OrdinalIgnoreCase));

        var paths = present
            .SelectMany(static x => Directory.GetFiles(Path.GetDirectoryName(x)!, "*.dll"))
            .Concat(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string tpa
                ? tpa.Split(Path.PathSeparator) : [])
            .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .Select(static x => x.First())
            .Where(static x => !Path.GetFileName(x).StartsWith("protobuf-net.BuildTools",
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        // The .proto half: generate DTOs from the schema tree, compile them, and treat the result as
        // one more target. It is built here rather than by the caller because it needs exactly this
        // reference set - a DTO assembly compiled against a *different* protobuf-net than the one the
        // corpus loads would give every contract a second, incompatible ProtoContractAttribute.
        //
        // Skippable, since it is the slowest part of the run and a generator change rarely needs it:
        // PBN_NO_SCHEMAS=1 leaves the corpus as it was.
        if (Environment.GetEnvironmentVariable("PBN_NO_SCHEMAS") is not { Length: > 0 })
        {
            Schemas = new Schemas();
            if (Schemas.Build(paths, AotDifferential.Schemas.OutputPath()) is { } schemaAssembly)
            {
                present = [.. present, schemaAssembly];
                paths.Add(schemaAssembly);
                Neighbours.Add(schemaAssembly);
            }
        }

        // Flattening every dll beside the targets into one reference set is what lets the contracts
        // bind, and is also the harness's own worst artefact: two of them declare `Timestamp` and
        // `Duration`, so those names are ambiguous *here* in a way no real consumer would see. The
        // clash is resolved by dropping the assembly that is *not* a scanned target - which is
        // decided from the CS0433 message rather than from a hard-coded list, so it cannot rot.
        var targetNames = new HashSet<string>(
            present.Select(Path.GetFileNameWithoutExtension), StringComparer.OrdinalIgnoreCase);
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        List<INamedTypeSymbol> symbols = null;
        HashSet<string> dropped = null;
        var peStream = new MemoryStream();

        for (var attempt = 0; ; attempt++)
        {
            peStream = new MemoryStream();
            var references = paths
                .Where(x => !excluded.Contains(Path.GetFileNameWithoutExtension(x)))
                .Select(static x => (MetadataReference)MetadataReference.CreateFromFile(x))
                .ToList();

            var probe = CSharpCompilation.Create("probe", references: references,
                options: MetadataOptions());

            symbols = [];
            Skipped.Clear();
            foreach (var path in present)
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var assembly = probe.References
                    .Select(probe.GetAssemblyOrModuleSymbol)
                    .OfType<IAssemblySymbol>()
                    .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (assembly is null) continue;
                Collect(probe, assembly.GlobalNamespace, symbols);
            }
            Seeded = symbols.Count;

            // triage aid: PBN_MEMBERS=<type substring> prints what the generator actually *sees* for
            // a contract read from metadata - members, accessibility, and the attributes on each.
            // The question it exists to answer is whether a member is missing or merely unrecognised
            if (Environment.GetEnvironmentVariable("PBN_MEMBERS") is { Length: > 0 } inspect)
            {
                foreach (var symbol in symbols)
                {
                    if (!symbol.ToDisplayString().Contains(inspect)) continue;
                    Console.Error.WriteLine($"--- {symbol.ToDisplayString()}");
                    foreach (var member in symbol.GetMembers())
                    {
                        if (member is not (IFieldSymbol or IPropertySymbol)) continue;
                        var attributes = member.GetAttributes()
                            .Select(static a => a.AttributeClass?.ToDisplayString() ?? "<unresolved>");
                        Console.Error.WriteLine($"    {member.Kind} {member.DeclaredAccessibility} "
                            + $"{member.Name}  [{string.Join(", ", attributes)}]");
                    }
                }
            }

            var source = BuildModelSource(symbols);
            var compilation = CSharpCompilation.Create("differential",
                [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
                references, MetadataOptions()
                    .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
                    {
                        ["PBN9001"] = ReportDiagnostic.Suppress,
                    }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                [LoadGenerator()], parseOptions: new CSharpParseOptions(LanguageVersion.Latest));
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

            // a dropped contract has no serializer, so there is nothing to compare - and asking would
            // just throw TypeModel's "no serializer" backstop, which is the intended behaviour
            dropped = new HashSet<string>(StringComparer.Ordinal);
            foreach (var diagnostic in diagnostics)
            {
                var match = Regex.Match(diagnostic.GetMessage(), @"Contract '([^']+)'");
                if (match.Success) dropped.Add(match.Groups[1].Value);
            }
            Dropped = dropped.Count;

            // triage aid: PBN_DUMP=<substring> prints the generated source for matching contracts,
            // which is the fastest way to tell a generator bug from a harness one
            if (Environment.GetEnvironmentVariable("PBN_DUMP") is { Length: > 0 } wanted)
            {
                foreach (var tree in output.SyntaxTrees)
                {
                    var lines = tree.GetText().Lines;
                    var remaining = 0;
                    foreach (var line in lines)
                    {
                        var text = line.ToString();
                        // a window, not just the matching line: the interesting part of a serializer
                        // is the member writes below the signature, which never name the type
                        if (text.Contains(wanted)) remaining = 14;
                        if (remaining-- > 0) Console.Error.WriteLine(text.TrimEnd());
                    }
                }
            }

            var emit = output.Emit(peStream);
            if (emit.Success) break;

            var errors = emit.Diagnostics.Where(static x => x.Severity == DiagnosticSeverity.Error).ToList();
            var newlyExcluded = errors.Where(static x => x.Id == "CS0433")
                .SelectMany(x => Regex.Matches(x.GetMessage(), @"'([^',]+), Version=").Cast<Match>())
                .Select(static x => x.Groups[1].Value)
                .Where(x => !targetNames.Contains(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(x => excluded.Add(x))
                .ToList();

            if (newlyExcluded.Count == 0 || attempt >= 4)
            {
                // the generated model is six figures of lines here, so an error naming line 105357
                // is useless on its own; write it out and say where it went
                var dump = Dump(output);
                return "the generated model does not compile, so nothing can be compared:"
                    + string.Concat(errors.Take(5).Select(static x => Environment.NewLine + "  " + x))
                    + (dump is null ? "" : Environment.NewLine + "  generated source written to " + dump);
            }
            Console.Error.WriteLine("ambiguous with a scanned target, excluding: "
                + string.Join(", ", newlyExcluded));
        }
        Ambiguous = excluded.ToList();

        // the targets have to be *loaded*, not just referenced: we construct instances of them
        foreach (var path in present) Assembly.LoadFrom(path);
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            var wanted = new AssemblyName(args.Name).Name + ".dll";
            var found = paths.FirstOrDefault(x => string.Equals(Path.GetFileName(x), wanted,
                StringComparison.OrdinalIgnoreCase));
            return found is null ? null : Assembly.LoadFrom(found);
        };

        var modelAssembly = Assembly.Load(peStream.ToArray());
        ModelType = modelAssembly.GetType(ModelTypeName)
            ?? throw new InvalidOperationException("the generated model type is missing");

        foreach (var symbol in symbols)
        {
            var name = symbol.ToDisplayString();
            if (dropped.Contains(name)) continue;
            if (Resolve(symbol) is { } type) Contracts.Add(type);
            else Bump(Skipped, "could not be resolved at runtime");
        }
        return null;
    }

    /// <summary>
    /// Write the generator's own output beside the harness, so a compile error in it can be read.
    /// </summary>
    private static string Dump(Compilation output)
    {
        try
        {
            var dir = Path.Combine(RepoRoot() ?? ".", "src", "AotDifferential", "obj", "generated");
            Directory.CreateDirectory(dir);
            string last = null;
            foreach (var tree in output.SyntaxTrees)
            {
                if (string.IsNullOrEmpty(tree.FilePath)) continue;
                last = Path.Combine(dir, Path.GetFileName(tree.FilePath));
                File.WriteAllText(last, tree.ToString());
            }
            return last;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Roslyn symbol to runtime <see cref="Type"/>, via the reflection name.</summary>
    private static Type Resolve(INamedTypeSymbol symbol)
    {
        var name = new StringBuilder();
        var nesting = new List<INamedTypeSymbol>();
        for (var current = symbol; current is not null; current = current.ContainingType) nesting.Add(current);
        nesting.Reverse();

        var ns = nesting[0].ContainingNamespace;
        if (ns is { IsGlobalNamespace: false }) name.Append(ns.ToDisplayString()).Append('.');
        // nested types are separated by '+' in reflection names, not '.'
        name.Append(string.Join("+", nesting.Select(static x => x.MetadataName)));

        var full = name.ToString();
        var assemblyName = symbol.ContainingAssembly?.Name;
        foreach (var candidate in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assemblyName is not null && candidate.GetName().Name != assemblyName) continue;
            if (candidate.GetType(full, throwOnError: false) is { } found) return found;
        }
        return Type.GetType(full, throwOnError: false);
    }

    private static ISourceGenerator LoadGenerator()
    {
        var dir = RepoRoot() ?? throw new InvalidOperationException("could not locate the repo root");
        foreach (var configuration in new[] { "Debug", "Release" })
        {
            var path = Path.Combine(dir, "src", "protobuf-net.BuildTools", "bin", configuration,
                "netstandard2.0", "protobuf-net.BuildTools.dll");
            if (!File.Exists(path)) continue;
            var type = Assembly.LoadFrom(path).GetType(GeneratorTypeName);
            if (type is null) continue;
            return ((IIncrementalGenerator)Activator.CreateInstance(type)).AsSourceGenerator();
        }
        throw new InvalidOperationException("build protobuf-net.BuildTools first");
    }

    /// <summary>
    /// Compilation options that import <em>all</em> members of a metadata reference, not just the
    /// public ones.
    /// </summary>
    /// <remarks>
    /// <see cref="MetadataImportOptions"/> defaults to <c>Public</c>, so a private field simply is
    /// not in the symbol — <c>GetMembers()</c> never returns it and nothing reports its absence.
    /// A contract whose members are non-public then emits a serializer with <em>no members</em>,
    /// silently, and still counts as "emitted" in the sweep. It looks exactly like a generator bug
    /// and is not one: a real consumer compiles from source, where every member is present.
    /// This is a property of driving the generator from metadata, which only these two harnesses do.
    /// </remarks>
    private static CSharpCompilationOptions MetadataOptions()
        => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithMetadataImportOptions(MetadataImportOptions.All);

    public static string RepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "src")))
        {
            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
        }
        return dir;
    }

    public static IEnumerable<string> DefaultTargets()
    {
        if (RepoRoot() is not { } dir) yield break;
        foreach (var project in new[] { "protobuf-net.Test", "Examples", "protobuf-net.Reflection.Test" })
        {
            yield return Path.Combine(dir, "src", project, "bin", "Debug", "net8.0", project + ".dll");
        }
    }

    private void Collect(Compilation compilation, INamespaceOrTypeSymbol scope, List<INamedTypeSymbol> found)
    {
        foreach (var member in scope.GetMembers())
        {
            switch (member)
            {
                case INamespaceSymbol ns:
                    Collect(compilation, ns, found);
                    break;
                case INamedTypeSymbol type:
                    Collect(compilation, type, found);
                    if (!type.GetAttributes().Any(static a
                        => a.AttributeClass?.ToDisplayString() == ProtoContractAttribute))
                    {
                        break;
                    }
                    if (type.DeclaredAccessibility != Accessibility.Public || !IsPubliclyNested(type))
                    {
                        Bump(Skipped, "not public");
                    }
                    else if (type.IsGenericType)
                    {
                        Bump(Skipped, "generic");
                    }
                    // a name two scanned assemblies both declare is ambiguous *here* and would not be
                    // in a real consumer - GetTypeByMetadataName returns null for exactly that. It is
                    // the harness's own artefact (the sweep reports it as CS0433), so seeding it would
                    // just fail the whole compile
                    else if (compilation.GetTypeByMetadataName(MetadataName(type)) is null)
                    {
                        Bump(Skipped, "declared by two scanned assemblies");
                    }
                    else
                    {
                        found.Add(type);
                    }
                    break;
            }
        }
    }

    private static string MetadataName(INamedTypeSymbol type)
    {
        var nesting = new List<INamedTypeSymbol>();
        for (var current = type; current is not null; current = current.ContainingType) nesting.Add(current);
        nesting.Reverse();
        var ns = nesting[0].ContainingNamespace;
        var prefix = ns is { IsGlobalNamespace: false } ? ns.ToDisplayString() + "." : "";
        return prefix + string.Join("+", nesting.Select(static x => x.MetadataName));
    }

    private static bool IsPubliclyNested(INamedTypeSymbol type)
    {
        for (var current = type.ContainingType; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public) return false;
        }
        return true;
    }

    private static void Bump(Dictionary<string, int> counts, string key)
        => counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;

    private static string BuildModelSource(List<INamedTypeSymbol> contracts)
    {
        var sb = new StringBuilder()
            .AppendLine("using ProtoBuf;")
            .AppendLine("using ProtoBuf.Meta;")
            .AppendLine()
            .AppendLine("[ProtoModel]");
        foreach (var contract in contracts)
        {
            sb.Append("[ProtoSerializable(typeof(")
              .Append(contract.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
              .AppendLine("))]");
        }
        return sb.Append("public partial class ").Append(ModelTypeName).AppendLine(" : TypeModel")
                 .AppendLine("{")
                 .AppendLine("}")
                 .ToString();
    }
}
