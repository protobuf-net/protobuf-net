using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Generators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ProtoBuf.AotCoverage;

/// <summary>
/// Runs <see cref="ProtoModelGenerator"/> over every <c>[ProtoContract]</c> in a set of already-built
/// assemblies and tallies what it could and could not handle.
/// </summary>
/// <remarks>
/// A reviewing aid, like <c>AotRefGen</c> — it exists so that "what should the generator support
/// next" is answered by counting real contracts rather than by guessing. Contracts are seeded from
/// *metadata*, not source, so this needs the target projects to have been built first.
/// </remarks>
internal static class Program
{
    private const string ProtoContractAttribute = "ProtoBuf.ProtoContractAttribute";

    private static int Main(string[] args)
    {
        var assemblies = args.Length != 0 ? args : DefaultTargets().ToArray();
        var present = assemblies.Where(File.Exists).ToArray();
        foreach (var missing in assemblies.Except(present))
        {
            Console.Error.WriteLine($"not built, skipping: {missing}");
        }
        if (present.Length == 0)
        {
            Console.Error.WriteLine("nothing to scan; build the target projects first");
            return 1;
        }

        // every dll beside the targets, so that types the contracts reference still bind
        var references = present
            .SelectMany(static x => Directory.GetFiles(Path.GetDirectoryName(x)!, "*.dll"))
            .Concat(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string tpa
                ? tpa.Split(Path.PathSeparator) : [])
            .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .Select(static x => x.First())
            // BuildTools *compiles in* protobuf-net.Core's sources, so referencing both would make
            // every type in Core ambiguous - see AGENTS.md
            .Where(static x => !Path.GetFileName(x).StartsWith("protobuf-net.BuildTools",
                StringComparison.OrdinalIgnoreCase))
            .Select(static x => (MetadataReference)MetadataReference.CreateFromFile(x))
            .ToList();

        var probe = CSharpCompilation.Create("probe", references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var contracts = new List<INamedTypeSymbol>();
        var skipped = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var path in present)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var assembly = probe.References
                .Select(probe.GetAssemblyOrModuleSymbol)
                .OfType<IAssemblySymbol>()
                .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (assembly is null)
            {
                Console.Error.WriteLine($"could not load symbols for {name}");
                continue;
            }
            Collect(assembly.GlobalNamespace, contracts, skipped);
        }

        Console.WriteLine($"# AOT generator coverage");
        Console.WriteLine();
        Console.WriteLine($"scanned: {string.Join(", ", present.Select(Path.GetFileNameWithoutExtension))}");
        Console.WriteLine($"seedable `[ProtoContract]` types: **{contracts.Count}**");
        foreach (var pair in skipped.OrderByDescending(static x => x.Value))
        {
            Console.WriteLine($"- not seedable, {pair.Key}: {pair.Value}");
        }
        Console.WriteLine();

        var source = BuildModel(contracts);
        var compilation = CSharpCompilation.Create("coverage",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                // PBN9001 is the [Experimental] id on the model attributes, so it is an *error* by
                // default and our own seed source trips it once per contract. Every other consumer
                // in the tree NoWarns it in the csproj; this is the programmatic equivalent.
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
                {
                    ["PBN9001"] = ReportDiagnostic.Suppress,
                }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new ProtoModelGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        foreach (var result in driver.GetRunResult().Results)
        {
            if (result.Exception is not null)
            {
                Console.Error.WriteLine($"generator threw: {result.Exception}");
                return 1;
            }
        }

        Report(contracts.Count, diagnostics);

        // the emitted code has to actually compile; anything else is a generator bug, not a gap
        var errors = output.GetDiagnostics()
            .Where(static x => x.Severity == DiagnosticSeverity.Error)
            .GroupBy(static x => x.Id)
            .OrderByDescending(static x => x.Count())
            .ToList();

        // ...except for the one error this harness causes itself: it flattens every dll beside the
        // targets into one reference set, so a type name declared by two of them is ambiguous here
        // and would not be in a real consumer. Reported, but not as a generator fault.
        var artefacts = errors.Where(static x => x.Key == "CS0433").ToList();
        errors = errors.Except(artefacts).ToList();

        Console.WriteLine();
        Console.WriteLine(errors.Count == 0
            ? "the generated code compiles cleanly."
            : "**the generated code does not compile** — these are generator bugs, not gaps:");
        foreach (var group in errors.Take(10))
        {
            Console.WriteLine($"- {group.Key}: {group.Count()} — {group.First().GetMessage()}");
        }
        foreach (var group in artefacts)
        {
            Console.WriteLine();
            Console.WriteLine($"harness artefact, {group.Key}: {group.Count()} — two scanned assemblies "
                + $"declare the same type name, e.g. {group.First().GetMessage()}");
        }
        return 0;
    }

    private static IEnumerable<string> DefaultTargets()
    {
        // walk up to the repo root, so this works from bin/ as well as from the source tree
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "src")))
        {
            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
        }
        if (dir is null) yield break;

        foreach (var (project, assembly) in new[]
        {
            ("protobuf-net.Test", "protobuf-net.Test"),
            ("Examples", "Examples"),
            ("protobuf-net.Reflection.Test", "protobuf-net.Reflection.Test"),
        })
        {
            yield return Path.Combine(dir, "src", project, "bin", "Debug", "net8.0", assembly + ".dll");
        }
    }

    private static void Collect(INamespaceOrTypeSymbol scope, List<INamedTypeSymbol> contracts,
        Dictionary<string, int> skipped)
    {
        foreach (var member in scope.GetMembers())
        {
            switch (member)
            {
                case INamespaceSymbol ns:
                    Collect(ns, contracts, skipped);
                    break;
                case INamedTypeSymbol type:
                    Collect(type, contracts, skipped); // nested types
                    if (!type.GetAttributes().Any(static a
                        => a.AttributeClass?.ToDisplayString() == ProtoContractAttribute))
                    {
                        break;
                    }
                    // only what a `typeof(...)` in another assembly can actually name
                    if (type.DeclaredAccessibility != Accessibility.Public
                        || (type.ContainingType is not null && !IsPubliclyNested(type)))
                    {
                        Bump(skipped, "not public");
                    }
                    else if (type.IsGenericType)
                    {
                        Bump(skipped, "generic");
                    }
                    else
                    {
                        contracts.Add(type);
                    }
                    break;
            }
        }
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

    private static string BuildModel(List<INamedTypeSymbol> contracts)
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
        return sb.AppendLine("public partial class CoverageModel : TypeModel")
                 .AppendLine("{")
                 .AppendLine("}")
                 .ToString();
    }

    /// <summary>
    /// Group the diagnostics by *reason*, with the type and member names stripped out, so that the
    /// same underlying gap counts once per occurrence rather than appearing as N unique messages.
    /// </summary>
    private static void Report(int seeded, IEnumerable<Diagnostic> diagnostics)
    {
        var byReason = new Dictionary<string, int>(StringComparer.Ordinal);
        var byMemberType = new Dictionary<string, int>(StringComparer.Ordinal);
        var cascades = 0;
        var dropped = new HashSet<string>(StringComparer.Ordinal);

        foreach (var diagnostic in diagnostics)
        {
            var message = diagnostic.GetMessage();
            var subject = Regex.Match(message, @"Contract '([^']+)'");
            if (subject.Success) dropped.Add(subject.Groups[1].Value);

            if (diagnostic.Id == "PBN2004")
            {
                cascades++;
                continue; // derivative: something it references failed for its own reason
            }

            // "unsupported type" is the one reason where the detail *is* the finding
            var memberType = Regex.Match(message, @"has unsupported type '([^']+)'");
            if (memberType.Success) Bump(byMemberType, Generalize(memberType.Groups[1].Value));

            Bump(byReason, $"{diagnostic.Id} {Normalize(message)}");
        }

        Console.WriteLine($"contracts dropped: **{dropped.Count}** of {seeded} "
            + $"({(seeded == 0 ? 0 : 100 - (dropped.Count * 100 / seeded))}% emitted)");
        Console.WriteLine($"of which dropped only by cascade: {cascades}");
        Console.WriteLine();
        Console.WriteLine("| count | reason |");
        Console.WriteLine("| ---: | --- |");
        foreach (var pair in byReason.OrderByDescending(static x => x.Value))
        {
            Console.WriteLine($"| {pair.Value} | {pair.Key} |");
        }

        if (byMemberType.Count != 0)
        {
            Console.WriteLine();
            Console.WriteLine("Member types we could not handle:");
            Console.WriteLine();
            Console.WriteLine("| count | type |");
            Console.WriteLine("| ---: | --- |");
            foreach (var pair in byMemberType.OrderByDescending(static x => x.Value).Take(20))
            {
                Console.WriteLine($"| {pair.Value} | `{pair.Key}` |");
            }
        }
    }

    /// <summary>
    /// Shorten a member type for the table: namespaces are noise, but the generic arguments are not.
    /// </summary>
    /// <remarks>
    /// This used to collapse to <c>Dictionary&lt;…&gt;</c> on the grounds that the element was
    /// rarely the interesting part. That stopped being true once collections were supported: a
    /// collection now only fails *because* of its element, so eliding it hid the actual reason.
    /// </remarks>
    private static string Generalize(string typeName)
        => Regex.Replace(typeName, @"[A-Za-z_]\w*(\.[A-Za-z_]\w*)+",
            static m => m.Value[(m.Value.LastIndexOf('.') + 1)..]);

    private static string Normalize(string message)
    {
        var reason = message.IndexOf("model: ", StringComparison.Ordinal) is var i and >= 0
            ? message[(i + 7)..] : message;
        reason = Regex.Replace(reason, @"'[^']*'", "'…'");
        return reason.TrimEnd('.');
    }
}
