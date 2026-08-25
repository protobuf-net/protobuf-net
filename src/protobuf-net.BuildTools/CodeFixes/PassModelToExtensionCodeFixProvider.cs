using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using ProtoBuf.BuildTools.Analyzers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace ProtoBuf.CodeFixes
{
    /// <summary>
    /// Supplies a model to a `.proto` extension accessor that needs one (<c>PBN3014</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="UseAotModelCodeFixProvider"/>, which swaps the <em>receiver</em>, this adds
    /// an <em>argument</em> — so it is a separate provider rather than a branch, and it offers the
    /// same candidates: anything of a model's type already in scope, then the generated
    /// <c>Model.Instance</c>.
    /// </para>
    /// <para>
    /// <b>The trailing argument cannot simply be replaced.</b> A setter's last argument is the
    /// <em>value</em>, and <c>obj.SetText(null)</c> is a perfectly ordinary call — blindly
    /// overwriting a trailing <c>null</c> would silently discard it. The model parameter is located
    /// by symbol instead, and only an argument genuinely bound to it is replaced; otherwise the
    /// model is appended.
    /// </para>
    /// </remarks>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PassModelToExtensionCodeFixProvider)), Shared]
    public class PassModelToExtensionCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(AotMigrationAnalyzer.ExtensionNeedsModel.Id);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var semantic = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null || semantic is null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                if (root.FindNode(diagnostic.Location.SourceSpan) is not InvocationExpressionSyntax invocation)
                {
                    continue;
                }

                var offered = new HashSet<string>(System.StringComparer.Ordinal);
                foreach (var candidate in UseAotModelCodeFixProvider
                    .InScopeModels(semantic, invocation, context.CancellationToken)
                    .Concat(UseAotModelCodeFixProvider.SharedInstances(diagnostic)))
                {
                    if (!offered.Add(candidate)) continue;
                    var title = $"Pass '{candidate}' to the extension accessor";
                    context.RegisterCodeFix(
                        CodeAction.Create(title,
                            _ => Task.FromResult(Rewrite(context.Document, root, semantic, invocation, candidate)),
                            equivalenceKey: title),
                        diagnostic);
                }
            }
        }

        private static Document Rewrite(Document document, SyntaxNode root, SemanticModel semantic,
            InvocationExpressionSyntax invocation, string instance)
        {
            // fully qualified from the analyzer, because it cannot know what is in scope here;
            // Simplifier reduces it to the shortest unambiguous spelling on application
            var value = SyntaxFactory.Argument(
                SyntaxFactory.ParseExpression(instance).WithAdditionalAnnotations(Simplifier.Annotation));

            var arguments = invocation.ArgumentList;
            var replaceAt = ModelArgumentIndex(semantic, invocation);
            var updated = replaceAt >= 0 && replaceAt < arguments.Arguments.Count
                ? arguments.WithArguments(arguments.Arguments.Replace(arguments.Arguments[replaceAt], value))
                : arguments.WithArguments(arguments.Arguments.Add(value));

            return document.WithSyntaxRoot(root.ReplaceNode(arguments, updated));
        }

        /// <summary>
        /// The position of an argument already bound to the model parameter, or -1 to append.
        /// </summary>
        private static int ModelArgumentIndex(SemanticModel semantic, InvocationExpressionSyntax invocation)
        {
            if (semantic.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method) return -1;

            IParameterSymbol? modelParameter = null;
            foreach (var parameter in method.Parameters)
            {
                if (parameter.Type?.ToDisplayString() == "ProtoBuf.Meta.TypeModel")
                {
                    modelParameter = parameter;
                    break;
                }
            }
            if (modelParameter is null) return -1; // the legacy overload: appending re-binds it

            // only an argument the compiler actually bound to that parameter counts - a defaulted
            // one has no syntax at all, and a trailing `null` may well be the value
            var index = 0;
            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                var bound = argument.NameColon?.Name.Identifier.ValueText is { } named
                    ? named == modelParameter.Name
                    : index == modelParameter.Ordinal;
                if (bound) return index;
                index++;
            }
            return -1;
        }
    }
}
