#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using ProtoBuf.BuildTools.Analyzers;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProtoBuf.CodeFixes
{
    /// <summary>
    /// Writes the <c>[ProtoModel]</c> stub for a project that has contracts and no model.
    /// </summary>
    /// <remarks>
    /// This is what turns <c>PBN2012</c>/<c>PBN2013</c> from a notification into an action. The
    /// diagnostics are anchored on a contract purely so that this can be offered — a code fix has to
    /// attach to a document — and the new file is added to the project rather than edited into an
    /// existing one, since a model is a declaration about the whole project rather than about the
    /// type the lightbulb happens to sit on.
    /// </remarks>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddProtoModelCodeFixProvider)), Shared]
    public class AddProtoModelCodeFixProvider : CodeFixProvider
    {
        private const string FileName = "ProtoModel.cs";

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            AotMigrationAnalyzer.NoModelUnderAot.Id, AotMigrationAnalyzer.NoModel.Id);

        /// <inheritdoc/>
        public override FixAllProvider? GetFixAllProvider() => null; // one model per project; nothing to batch

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (model is null || root is null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                // the anchor names a contract; seeding it is the useful starting point, and the
                // generator pulls in everything reachable from there by itself
                var declared = model.GetDeclaredSymbol(
                    root.FindNode(diagnostic.Location.SourceSpan), context.CancellationToken);
                if (declared is not INamedTypeSymbol contract) continue;

                const string Title = "Add an AOT model for this project";
                context.RegisterCodeFix(
                    CodeAction.Create(Title,
                        ct => AddModel(context.Document.Project, contract, ct),
                        equivalenceKey: Title),
                    diagnostic);
            }
        }

        private static Task<Solution> AddModel(Project project, INamedTypeSymbol contract,
            CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            var name = contract.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // Namespace resolution, most-authoritative first. The first tier is what rescues the
            // common console shape: with top-level statements the anchor contract sits in the global
            // namespace and DefaultNamespace has proven unreliable in practice, but RootNamespace is
            // compiler-visible by default in the SDK and MSBuild defaults it to the project name.
            // 1. build_property.RootNamespace;
            // 2. the workspace's DefaultNamespace, where the host supplies one;
            // 3. the anchor contract's own namespace;
            // 4. none - and since the file is added at the project *root*, the csproj convention
            //    (folders, not project or assembly name) genuinely says global namespace there.
            string? ns = null;
            if (project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions
                    .TryGetValue("build_property.RootNamespace", out var rootNamespace)
                && !string.IsNullOrWhiteSpace(rootNamespace))
            {
                ns = rootNamespace;
            }
            if (string.IsNullOrEmpty(ns)) ns = project.DefaultNamespace;
            if (string.IsNullOrEmpty(ns) && contract.ContainingNamespace is { IsGlobalNamespace: false } containing)
            {
                ns = containing.ToDisplayString();
            }

            // internal, deliberately: a serialization model is project infrastructure, and a fixer
            // should not add to the public surface. Block-scoped namespace, so the output parses at
            // whatever LangVersion the project is on.
            const string Comment = @"// Compile-time serializers for this project. Name the types you serialize *directly*; everything
// reachable from those - member types, collection elements, map keys and values, [ProtoInclude]
// sub-types - is included automatically.
//
// See https://protobuf-net.github.io/protobuf-net/aot
";
            var source = string.IsNullOrEmpty(ns)
                ? $@"using ProtoBuf;
using ProtoBuf.Meta;

{Comment}[ProtoModel]
[ProtoSerializable(typeof({name}))]
internal partial class ProtoModel : TypeModel
{{
}}
"
                : $@"using ProtoBuf;
using ProtoBuf.Meta;

{Comment}namespace {ns}
{{
    [ProtoModel]
    [ProtoSerializable(typeof({name}))]
    internal partial class ProtoModel : TypeModel
    {{
    }}
}}
";
            // a unique name, so the fix is safe to apply in a project that already has a ProtoModel.cs
            var fileName = project.Documents.Any(static d => d.Name == FileName)
                ? $"ProtoModel.{contract.Name}.cs"
                : FileName;

            // at the project root, deliberately: the folder half of the namespace convention is then
            // empty by construction, so the namespace above is the whole answer
            return Task.FromResult(project
                .AddDocument(fileName, SourceText.From(source))
                .Project.Solution);
        }
    }
}
