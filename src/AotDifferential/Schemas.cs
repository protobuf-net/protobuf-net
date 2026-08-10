using Google.Protobuf.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProtoBuf.AotDifferential;

/// <summary>
/// The other half of the corpus: DTOs generated from <c>.proto</c> schemas, compiled in-process and
/// handed to <see cref="Corpus"/> as one more target assembly.
/// </summary>
/// <remarks>
/// <para>
/// The hand-written corpus is code-first contracts written by people, which is only half of what
/// protobuf-net produces. Schema-generated DTOs are a different shape entirely — deeply nested types,
/// <c>oneof</c> discriminated unions, <c>map</c> fields, groups, <c>[DefaultValue]</c> everywhere,
/// and contracts whose members are all <c>[DataMember]</c>-free <c>[ProtoMember]</c> with explicit
/// <c>Name</c> and <c>DataFormat</c> — and none of it had ever been compared against ref-emit.
/// </para>
/// <para>
/// Generated at run time rather than checked in, deliberately: a checked-in snapshot of generated
/// code drifts from the generator that produced it, and the drift is invisible until someone reads
/// the file. The cost is that the schemas must parse, so <see cref="Rejected"/> reports what did not
/// rather than hiding it.
/// </para>
/// </remarks>
internal sealed class Schemas
{
    /// <summary>The assembly name, and so the name the corpus reports it under.</summary>
    public const string AssemblyName = "SchemaCorpus";

    /// <summary><c>.proto</c> files found under the schema root.</summary>
    public int Found { get; private set; }

    /// <summary><c>.proto</c> files that parsed and reached code generation.</summary>
    public int Accepted { get; private set; }

    /// <summary>C# files the generator produced from those.</summary>
    public int Generated { get; private set; }

    /// <summary>Why a schema or a generated file was dropped, and how many.</summary>
    public Dictionary<string, int> Rejected { get; } = new(StringComparer.Ordinal);

    /// <summary>Examples per rejection reason, so a regression is diagnosable from the report.</summary>
    public Dictionary<string, List<string>> Examples { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Parse every schema, generate DTOs, compile them into one assembly, and return its path —
    /// or <c>null</c> if there is nothing to add, which is never fatal: the corpus is still a corpus
    /// without this half.
    /// </summary>
    public string Build(IEnumerable<string> references, string outputPath)
    {
        var root = SchemaRoot();
        if (root is null) return null;

        var protos = Directory.GetFiles(root, "*.proto", SearchOption.AllDirectories)
            .Select(x => x.Substring(root.Length + 1).Replace(Path.DirectorySeparatorChar, '/'))
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
        Found = protos.Count;
        if (Found == 0) return null;

        var set = Parse(root, protos);
        if (set is null) return null;

        List<CodeFile> files;
        try
        {
            files = CSharpCodeGenerator.Default.Generate(set).ToList();
        }
        catch (Exception ex)
        {
            Reject("code generation threw", ex.GetType().Name + ": " + ex.Message);
            return null;
        }
        Generated = files.Count;

        return Compile(files, references, outputPath);
    }

    /// <summary>
    /// Add every schema to one <see cref="FileDescriptorSet"/>, dropping whatever fails to parse and
    /// retrying until it settles.
    /// </summary>
    /// <remarks>
    /// One set rather than one per schema, because the schemas import each other: generating a file
    /// in isolation still emits references to the types its imports declare, so those have to be
    /// generated too, exactly once, and from the same set. Dropping is iterative because dropping an
    /// *imported* schema breaks its importers, which then fail on the next pass and are dropped in
    /// turn — it converges, but not in one step.
    /// </remarks>
    private FileDescriptorSet Parse(string root, List<string> protos)
    {
        var accepted = new List<string>(protos);
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var set = new FileDescriptorSet();
            set.AddImportPath(root);
            foreach (var proto in accepted) set.Add(proto, includeInOutput: true);
            set.Process();

            var errors = set.GetErrors().Where(static x => x.IsError).ToList();
            if (errors.Count == 0)
            {
                Accepted = accepted.Count;
                return set;
            }

            var bad = new HashSet<string>(errors.Select(static x => x.File),
                StringComparer.OrdinalIgnoreCase);
            var removed = accepted.RemoveAll(x => bad.Contains(x));
            if (removed == 0)
            {
                // the errors name a file that is not one of ours (an import we cannot drop), so
                // nothing would change on the next pass
                Reject("schema does not parse", errors[0].Message, errors.Take(3).Select(static x => x.File));
                return null;
            }
            foreach (var error in errors.GroupBy(static x => x.File))
            {
                Reject("schema does not parse", error.First().Message, [error.Key]);
            }
        }
        Reject("schema parse did not settle", "still failing after 8 passes");
        return null;
    }

    /// <summary>
    /// Compile the generated DTOs into one assembly, dropping the files that do not compile and
    /// retrying — same shape as the parse loop, and for the same reason.
    /// </summary>
    /// <remarks>
    /// Two schemas declaring the same C# type is the common case here (the corpus contains several
    /// versions of the same API), and it is a property of gathering unrelated schemas into one
    /// assembly rather than anything a real consumer would hit — so it is dropped and counted, not
    /// treated as a failure.
    /// </remarks>
    private string Compile(List<CodeFile> files, IEnumerable<string> references, string outputPath)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var trees = files.ToDictionary(
            static x => x.Name,
            x => CSharpSyntaxTree.ParseText(x.Text, parseOptions, path: x.Name),
            StringComparer.Ordinal);

        // Google.Protobuf declares the descriptor types (`FieldOptions`, `MethodOptions`, …) that
        // protobuf-net.Reflection also declares, so a schema extending descriptor.proto - which is
        // what a custom option *is* - sees them ambiguously and does not compile. The corpus drops
        // Google.Protobuf as ambiguous a step later anyway, so dropping it here just brings that
        // forward; without it, every custom-options schema is lost.
        var metadata = references
            .Where(static x => !string.Equals(Path.GetFileNameWithoutExtension(x), "Google.Protobuf",
                StringComparison.OrdinalIgnoreCase))
            .Select(static x => (MetadataReference)MetadataReference.CreateFromFile(x))
            .ToList();

        // the generated code is not ours to tidy: it deliberately carries obsolete members for
        // schemas that use them, and warns about unused usings in nearly every file
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
            {
                ["CS0612"] = ReportDiagnostic.Suppress,
                ["CS0618"] = ReportDiagnostic.Suppress,
                ["CS0619"] = ReportDiagnostic.Suppress,
            });

        DropCollisions(trees, metadata);

        for (var attempt = 0; attempt < 8; attempt++)
        {
            var compilation = CSharpCompilation.Create(AssemblyName, trees.Values, metadata, options);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            using var peStream = new MemoryStream();
            var result = compilation.Emit(peStream);
            if (result.Success)
            {
                File.WriteAllBytes(outputPath, peStream.ToArray());
                return outputPath;
            }

            var errors = result.Diagnostics
                .Where(static x => x.Severity == DiagnosticSeverity.Error)
                .ToList();

            // a duplicate type is reported against *both* declarations, so dropping every file an
            // error names would take the good one with it; drop the later one by name order, which
            // is stable, and let the next pass confirm
            var implicated = errors
                .Select(static x => x.Location.SourceTree?.FilePath)
                .Where(static x => x is not null)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static x => x, StringComparer.Ordinal)
                .ToList();
            var drop = errors.Any(static x => x.Id is "CS0101" or "CS0111")
                ? implicated.Skip(1).ToList()
                : implicated;

            if (drop.Count == 0 || drop.Count == trees.Count)
            {
                Reject("generated DTO does not compile", errors[0].GetMessage(),
                    errors.Take(3).Select(static x => x.Location.SourceTree?.FilePath ?? "?"));
                return null;
            }
            foreach (var file in drop)
            {
                var why = errors.FirstOrDefault(x => x.Location.SourceTree?.FilePath == file);
                Reject("generated DTO does not compile", why?.GetMessage() ?? "?", [file]);
                trees.Remove(file);
            }
        }
        Reject("generated DTO compilation did not settle", "still failing after 8 passes");
        return null;
    }

    /// <summary>
    /// Drop generated files that declare a name the hand-written corpus already owns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>protobuf-net.Test</c> carries checked-in DTOs for <c>google/protobuf/duration.proto</c> and
    /// <c>timestamp.proto</c>, so generating them again declares
    /// <c>Google.Protobuf.WellKnownTypes.Duration</c> twice; <c>Examples</c> has a *namespace* called
    /// <c>SearchRequest</c> where a schema declares a *type* of that name, which is CS0434 rather
    /// than CS0433 and so needs the namespace checked as well as the type.
    /// </para>
    /// <para>
    /// The hand-written one wins, deliberately. It is already in the corpus and already compared, so
    /// the schema copy would add a duplicate rather than coverage — and dropping it costs nothing
    /// downstream, because the *type* still resolves for every schema that references it: the
    /// declaring assembly is in this compilation's reference set.
    /// </para>
    /// </remarks>
    private void DropCollisions(Dictionary<string, SyntaxTree> trees, List<MetadataReference> metadata)
    {
        var probe = CSharpCompilation.Create("collision-probe", references: metadata);

        // Ask each assembly separately rather than asking the compilation. Compilation-level lookup
        // returns null for an *ambiguous* name, and these names are ambiguous by definition - both
        // `Google.Protobuf` and `protobuf-net.Test` declare `Duration` - so the very collisions this
        // exists to catch are the ones it would silently miss.
        var assemblies = probe.References
            .Select(probe.GetAssemblyOrModuleSymbol)
            .OfType<IAssemblySymbol>()
            .ToList();

        foreach (var entry in trees.ToList())
        {
            foreach (var name in DeclaredTypeNames(entry.Value))
            {
                if (!assemblies.Any(x => x.GetTypeByMetadataName(name) is not null)
                    && !NamespaceExists(probe, name))
                {
                    continue;
                }
                Reject("name already declared by the hand-written corpus", name, [entry.Key]);
                trees.Remove(entry.Key);
                break;
            }
        }
    }

    /// <summary>
    /// The full names of the <em>top-level</em> types a generated file declares.
    /// </summary>
    /// <remarks>
    /// Top-level only, deliberately: a nested type cannot collide unless its containing type does,
    /// so checking the outer one covers it — and it avoids having to spell nested names the way
    /// metadata does, with <c>+</c> rather than <c>.</c>.
    /// </remarks>
    private static IEnumerable<string> DeclaredTypeNames(SyntaxTree tree)
    {
        foreach (var node in tree.GetRoot().DescendantNodes())
        {
            if (node is not BaseTypeDeclarationSyntax declaration) continue;
            if (declaration.Parent is BaseTypeDeclarationSyntax) continue;

            var ns = declaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
            yield return ns is null
                ? declaration.Identifier.ValueText
                : ns.Name + "." + declaration.Identifier.ValueText;
        }
    }

    /// <summary>Whether a namespace of exactly this name exists — the other half of CS0434.</summary>
    private static bool NamespaceExists(Compilation probe, string fullName)
    {
        INamespaceSymbol current = probe.GlobalNamespace;
        foreach (var part in fullName.Split('.'))
        {
            current = current.GetNamespaceMembers()
                .FirstOrDefault(x => string.Equals(x.Name, part, StringComparison.Ordinal));
            if (current is null) return false;
        }
        return true;
    }

    private void Reject(string reason, string detail, IEnumerable<string> files = null)
    {
        var key = reason + ": " + Summarize(detail);
        Rejected[key] = Rejected.TryGetValue(key, out var n) ? n + 1 : 1;
        if (files is null) return;
        if (!Examples.TryGetValue(key, out var examples)) Examples[key] = examples = [];
        foreach (var file in files)
        {
            if (examples.Count >= 3) break;
            if (!examples.Contains(file)) examples.Add(file);
        }
    }

    private static string Summarize(string message)
    {
        message = message.Replace("\r", " ").Replace("\n", " ");
        return message.Length > 100 ? message[..100] + "…" : message;
    }

    /// <summary>
    /// The schema corpus. <c>protobuf-net.Reflection.Test</c>'s tree is the one worth using: it is
    /// large, it is real (the Google API surface, OpenTelemetry, Vault), and it is already a test
    /// input, so it is maintained rather than a snapshot someone has to remember to refresh.
    /// </summary>
    public static string SchemaRoot()
    {
        if (Corpus.RepoRoot() is not { } dir) return null;
        var root = Path.Combine(dir, "src", "protobuf-net.Reflection.Test", "Schemas");
        return Directory.Exists(root) ? root : null;
    }

    /// <summary>Where the compiled DTO assembly goes; under <c>obj</c>, so it is not committed.</summary>
    public static string OutputPath()
        => Path.Combine(Corpus.RepoRoot() ?? ".", "src", "AotDifferential", "obj", "schema-corpus",
            AssemblyName + ".dll");
}
