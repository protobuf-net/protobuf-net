#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using ProtoBuf.BuildTools.Analyzers;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace ProtoBuf.BuildTools.CodeFixes
{
    /// <summary>
    /// Fixes <c>PBN4016</c> by passing the generated factory: <c>CreateGrpcService&lt;T&gt;()</c> becomes
    /// <c>CreateGrpcService&lt;T&gt;(MyServices.Instance)</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The whole fix is one argument, and that is the point rather than a limitation: the explicit form and
    /// the interceptor produce the same program, so this is exactly what interception would have done -
    /// which is what makes the interceptor honest and this fixer its equivalent for anyone who has not
    /// enabled the feature.
    /// </para>
    /// <para>
    /// The factory comes from the diagnostic's properties rather than being re-derived here. The analyzer
    /// already resolved which model covers the contract, and a fixer that worked it out again could
    /// disagree with the diagnostic it is fixing.
    /// </para>
    /// <para>
    /// Emitted fully qualified with <see cref="Simplifier.Annotation"/>, as
    /// <c>UseAotModelCodeFixProvider</c> does: the fixer cannot know what is in scope at the call site, so
    /// it writes the unambiguous form and lets the simplifier reduce it - leaving <c>global::</c> only
    /// where it is genuinely needed.
    /// </para>
    /// </remarks>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseGeneratedClientFactoryCodeFixProvider))]
    [Shared]
    public sealed class UseGeneratedClientFactoryCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(GrpcMigrationAnalyzer.CallDoesNotUseGeneratedFactory.Id);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.FirstOrDefault();
            if (diagnostic is null) return;
            if (!diagnostic.Properties.TryGetValue(GrpcMigrationAnalyzer.FactoryProperty, out var factory)
                || string.IsNullOrEmpty(factory))
            {
                return;
            }

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null) return;
            if (root.FindNode(diagnostic.Location.SourceSpan) is not InvocationExpressionSyntax invocation)
            {
                // the reported span is the whole invocation, but be defensive about trivia and parens
                invocation = root.FindNode(diagnostic.Location.SourceSpan)
                    .FirstAncestorOrSelf<InvocationExpressionSyntax>()!;
                if (invocation is null) return;
            }

            var title = $"Use '{Simple(factory!)}.Instance'";
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    ct => WithFactoryAsync(context.Document, root, invocation, factory!, ct),
                    equivalenceKey: nameof(UseGeneratedClientFactoryCodeFixProvider)),
                diagnostic);
        }

        private static Task<Document> WithFactoryAsync(Document document, SyntaxNode root,
            InvocationExpressionSyntax invocation, string factory, System.Threading.CancellationToken _)
        {
            var access = SyntaxFactory.ParseExpression(factory + ".Instance")
                .WithAdditionalAnnotations(Simplifier.Annotation);

            // Append, unless there is an explicit `null` to replace - which covers all four shapes the
            // call can take, and the first cut got wrong by assuming only one of them:
            //
            //   invoker.CreateGrpcService<T>()                      -> append
            //   invoker.CreateGrpcService<T>(null)                  -> replace the null
            //   GrpcClientFactory.CreateGrpcService<T>(invoker)      -> append (the receiver is an argument
            //                                                          here, and replacing it deleted it)
            //   GrpcClientFactory.CreateGrpcService<T>(invoker, null) -> replace the null
            //
            // Safe because the analyzer only reports calls where no real factory was passed, so a null
            // literal in the list can only be the factory.
            var existing = invocation.ArgumentList.Arguments;
            var nullIndex = -1;
            for (var i = 0; i < existing.Count; i++)
            {
                if (existing[i].Expression.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    nullIndex = i;
                    break;
                }
            }

            var arguments = nullIndex < 0
                ? invocation.ArgumentList.AddArguments(SyntaxFactory.Argument(access))
                : invocation.ArgumentList.WithArguments(
                    existing.Replace(existing[nullIndex], SyntaxFactory.Argument(access)));

            var updated = invocation.WithArgumentList(arguments);
            return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(invocation, updated)));
        }

        /// <summary>The last segment of a qualified name, for the code-action title.</summary>
        private static string Simple(string qualified)
        {
            var trimmed = qualified.StartsWith("global::", System.StringComparison.Ordinal)
                ? qualified.Substring("global::".Length)
                : qualified;
            var dot = trimmed.LastIndexOf('.');
            return dot < 0 ? trimmed : trimmed.Substring(dot + 1);
        }
    }
}
