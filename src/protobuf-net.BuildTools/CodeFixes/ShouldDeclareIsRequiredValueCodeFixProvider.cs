using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using ProtoBuf.BuildTools.Analyzers;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.Internal.Roslyn.Extensions;

namespace ProtoBuf.CodeFixes
{
    /// <summary>
    /// Implements a CodeFix on 'ShouldDeclareIsRequired' diagnostic
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ShouldDeclareIsRequiredValueCodeFixProvider)), Shared]
    public class ShouldDeclareIsRequiredValueCodeFixProvider : CodeFixProvider
    {
        const string CodeFixTitle = "Set [ProtoMember] attribute as Required";
        
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DataContractAnalyzer.ShouldDeclareIsRequired.Id);
        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // probably Roslyn does the check by itself, but lets check that there are diagnostics to investigate it
            if (!context.Diagnostics.Any()) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticLocationSpan = diagnostic.Location.SourceSpan;
                var diagnosticTextSpan = new TextSpan(diagnosticLocationSpan.Start, diagnosticLocationSpan.Length);

                var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                var protoMemberAttributeSyntax = root?.FindNode(diagnosticTextSpan) as AttributeSyntax;
                if (protoMemberAttributeSyntax is null) return;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixTitle,
                        createChangedSolution: 
                            cancellationToken => AddIsRequiredPropertyToProtoMemberAttributeAsync(
                                context.Document, protoMemberAttributeSyntax, cancellationToken),
                        equivalenceKey: nameof(ShouldDeclareIsRequiredValueCodeFixProvider)),
                    diagnostic);
            }
        }

        private async Task<Solution> AddIsRequiredPropertyToProtoMemberAttributeAsync(
            Document document,
            AttributeSyntax protoMemberAttributeSyntax,
            CancellationToken cancellationToken)
        {
            var argumentList = protoMemberAttributeSyntax.ArgumentList;
            if (argumentList is null) return document.Project.Solution;

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (oldRoot is null) return document.Project.Solution;

            var nameEqualsSyntax = SyntaxFactory.NameEquals(nameof(ProtoMemberAttribute.IsRequired));
            var searchedSyntax = argumentList.Arguments.FirstOrDefault(x => x.NameEquals.EqualsByIdentifierText(nameEqualsSyntax));

            SyntaxNode newRoot;
            if (searchedSyntax is null)
            {
                var updatedArgumentList = argumentList.AddArguments(
                    BuildIsRequiredAttributeArgumentSyntax()
                );
                newRoot = oldRoot.ReplaceNode(argumentList, updatedArgumentList);    
            }
            else
            {
                var newAssignmentSyntax = BuildIsRequiredAttributeArgumentSyntax();
                newRoot = oldRoot.ReplaceNode(searchedSyntax, newAssignmentSyntax);
            }
            
            document = document.WithSyntaxRoot(newRoot);
            return document.Project.Solution;

            AttributeArgumentSyntax BuildIsRequiredAttributeArgumentSyntax()
                => SyntaxFactory.AttributeArgument(
                    nameEqualsSyntax,
                    default,
                    SyntaxFactory.ParseExpression("true")
                );
        }
    }
}
