#nullable enable
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

                var offered = new HashSet<string>(System.StringComparer.Ordinal);
                foreach (var candidate in InScopeModels(model, invocation, context.CancellationToken)
                    .Concat(SharedInstances(diagnostic)))
                {
                    if (!offered.Add(candidate)) continue;
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
            // so the arguments, trivia and the rest of the statement are left exactly as they were.
            //
            // The shared-instance form arrives fully qualified, because the analyzer cannot know what
            // is in scope here; Simplifier.Annotation is how that is resolved - Roslyn reduces it to
            // the shortest unambiguous spelling when the fix is applied, so a consumer sees
            // `MyModel.Instance` and only gets `global::` where it is genuinely needed.
            var replacement = access.WithExpression(
                SyntaxFactory.ParseExpression(instance)
                    .WithAdditionalAnnotations(Simplifier.Annotation)
                    .WithTriviaFrom(access.Expression));
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

        /// <summary>
        /// The generated <c>Model.Instance</c> accessors, which is what makes this fixable when
        /// nothing is in scope — the common case for a codebase part-way through migrating.
        /// </summary>
        /// <remarks>
        /// The model names come from the diagnostic's properties rather than being re-derived here;
        /// the analyzer has already done that scan. `Instance` is generated onto every model unless
        /// the consumer declared their own member of that name, so this is offered after anything
        /// genuinely in scope rather than instead of it.
        /// </remarks>
        private static IEnumerable<string> SharedInstances(Diagnostic diagnostic)
        {
            if (!diagnostic.Properties.TryGetValue(AotMigrationAnalyzer.ModelsProperty, out var models)
                || string.IsNullOrEmpty(models))
            {
                yield break;
            }
            foreach (var model in models!.Split(';'))
            {
                if (model.Length != 0) yield return model + ".Instance";
            }
        }

        private static bool IsProtoModel(ITypeSymbol type)
            => type.GetAttributes().Any(static a
                => a.AttributeClass?.ToDisplayString() == "ProtoBuf.ProtoModelAttribute");
    }
}
