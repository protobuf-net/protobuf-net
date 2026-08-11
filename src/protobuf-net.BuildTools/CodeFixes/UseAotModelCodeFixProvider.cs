#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProtoBuf.BuildTools.Analyzers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProtoBuf.CodeFixes
{
    /// <summary>
    /// Rewrites a call that goes through the runtime model so that it goes through an AOT model
    /// instead — <c>Serializer.Serialize(s, x)</c> becomes <c>myModel.Serialize(s, x)</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Offered **only when an instance of the model is already in scope** — a field, property, local
    /// or parameter. That restriction is the point rather than a shortcut: the alternative is to
    /// write <c>new MyModel()</c> at the call site, and a <c>TypeModel</c> is a cache that is meant
    /// to be built once and reused, so a fixer that scattered constructions through a codebase would
    /// be doing harm tidily. Where nothing is in scope the diagnostic still stands and the author
    /// decides where the instance should live.
    /// </para>
    /// <para>
    /// Only <c>PBN2010</c> is fixable. <c>PBN2011</c> — the <c>object</c>/<c>Type</c> APIs — has no
    /// mechanical rewrite, because the whole difficulty is that nobody can tell what it serializes.
    /// </para>
    /// </remarks>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseAotModelCodeFixProvider)), Shared]
    public class UseAotModelCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(AotMigrationAnalyzer.UsesRuntimeModel.Id);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null || model is null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                if (root.FindNode(diagnostic.Location.SourceSpan) is not InvocationExpressionSyntax invocation
                    || invocation.Expression is not MemberAccessExpressionSyntax access)
                {
                    continue;
                }

                foreach (var candidate in InScopeModels(model, invocation, context.CancellationToken))
                {
                    var title = $"Use '{candidate}' instead of the runtime model";
                    context.RegisterCodeFix(
                        CodeAction.Create(title,
                            _ => Task.FromResult(Rewrite(context.Document, root, access, candidate)),
                            equivalenceKey: title),
                        diagnostic);
                }
            }
        }

        private static Document Rewrite(Document document, SyntaxNode root,
            MemberAccessExpressionSyntax access, string instance)
        {
            // only the receiver changes: `Serializer.Serialize(...)` -> `instance.Serialize(...)`,
            // so the arguments, trivia and the rest of the statement are left exactly as they were
            var replacement = access.WithExpression(
                SyntaxFactory.IdentifierName(instance).WithTriviaFrom(access.Expression));
            return document.WithSyntaxRoot(root.ReplaceNode(access, replacement));
        }

        /// <summary>
        /// Names in scope at the call site that are already a <c>[ProtoModel]</c> instance.
        /// </summary>
        private static IEnumerable<string> InScopeModels(SemanticModel model,
            SyntaxNode at, CancellationToken cancellationToken)
        {
            var seen = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var symbol in model.LookupSymbols(at.SpanStart))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var type = symbol switch
                {
                    IFieldSymbol field => field.Type,
                    IPropertySymbol property => property.Type,
                    ILocalSymbol local => local.Type,
                    IParameterSymbol parameter => parameter.Type,
                    _ => null,
                };
                if (type is null || !IsProtoModel(type)) continue;
                if (seen.Add(symbol.Name)) yield return symbol.Name;
            }
        }

        private static bool IsProtoModel(ITypeSymbol type)
            => type.GetAttributes().Any(static a
                => a.AttributeClass?.ToDisplayString() == "ProtoBuf.ProtoModelAttribute");
    }
}
