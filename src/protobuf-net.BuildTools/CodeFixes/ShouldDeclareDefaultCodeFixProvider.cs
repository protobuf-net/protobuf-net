using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using ProtoBuf.BuildTools.Analyzers;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProtoBuf.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ShouldDeclareDefaultCodeFixProvider)), Shared]
    public class ShouldDeclareDefaultCodeFixProvider : CodeFixProvider
    {
        const string CodeFixTitle = "Add [DefaultValue] attribute";
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DataContractAnalyzer.ShouldDeclareDefault.Id);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // probably Roslyn does the check by itself, but lets check that there are diagnostics to investigate it
            if (!context.Diagnostics.Any()) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticLocationSpan = diagnostic.Location.SourceSpan;
                var diagnosticTextSpan = new TextSpan(diagnosticLocationSpan.Start, diagnosticLocationSpan.Length);

                var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                var protoMemberAttributeSyntax = root.FindNode(diagnosticTextSpan) as AttributeSyntax;
                if (protoMemberAttributeSyntax is null) return;

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixTitle,
                        createChangedSolution: cancellationToken => MarkArgumentWithStaticKeywordAsync(context.Document, protoMemberAttributeSyntax, cancellationToken),
                        equivalenceKey: nameof(ShouldDeclareDefaultCodeFixProvider)),
                    diagnostic);
            }
        }

        private async Task<Solution> MarkArgumentWithStaticKeywordAsync(Document document, AttributeSyntax protoMemberAttributeSyntax, CancellationToken cancellationToken)
        {
            var newAttributeSyntax = protoMemberAttributeSyntax.InsertNodesAfter();


            // finally replace original node with new node
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(protoMemberAttributeSyntax, newAttributeSyntax);
            return document.WithSyntaxRoot(newRoot).Project.Solution;
        }
    }
}
