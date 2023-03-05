using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.BuildTools.Internal;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
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
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <summary>
        /// Key of for a <see cref="KeyValuePair{TKey, TValue}"/> of diagnostic properties,
        /// containing <see cref="DefaultValueAttribute"/> constructor value to be inserted into code
        /// </summary>
        public const string DefaultValueDiagnosticArgKey = "DefaultValueDiagnosticArg";

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

                if (!TryBuildDiagnosticArguments(diagnostic, out var diagnosticArguments)) return;                
                
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixTitle,
                        createChangedSolution: 
                            cancellationToken => AddDefaultValueAttributeWithValueAsync(
                                context.Document, protoMemberAttributeSyntax, diagnosticArguments, cancellationToken),
                        equivalenceKey: nameof(ShouldDeclareDefaultCodeFixProvider)),
                    diagnostic);
            }
        }

        private async Task<Solution> AddDefaultValueAttributeWithValueAsync(
            Document document, 
            AttributeSyntax protoMemberAttributeSyntax, 
            DiagnosticArguments diagnosticArguments,
            CancellationToken cancellationToken)
        {
            // care: order is important to not lose an `AttributeSyntax`
            // 1st step is to fix the [DefaultValue] and then do other things (like adding `using System.xxx`)
            document = await AddDefaultValueAttributeWithValueAsyncImpl(document, protoMemberAttributeSyntax, diagnosticArguments, cancellationToken);
            document = await AddDefaultValueAttributeUsingDirectiveAsync(document, cancellationToken);
            return document.Project.Solution;
        }

        private async Task<Document> AddDefaultValueAttributeWithValueAsyncImpl(
            Document document,
            AttributeSyntax protoMemberAttributeSyntax,
            DiagnosticArguments diagnosticArguments,
            CancellationToken cancellationToken)
        {
            var attributeList = protoMemberAttributeSyntax.Parent as AttributeListSyntax;
            if (attributeList is null) return document;
            
            var updatedArgumentList = attributeList.AddAttributes(
                SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName("DefaultValue"),
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SeparatedList(new[] { BuildDefaultValueArgumentSyntax() })
                    )
                )
            );

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (oldRoot is null) return document;
            
            var newRoot = oldRoot.ReplaceNode(attributeList, updatedArgumentList);
            return document.WithSyntaxRoot(newRoot);

            AttributeArgumentSyntax BuildDefaultValueArgumentSyntax()
                => SyntaxFactory.AttributeArgument(
                    default, default, SyntaxFactory.ParseExpression(diagnosticArguments.DefaultValue));
        }

        private async Task<Document> AddDefaultValueAttributeUsingDirectiveAsync(
            Document document, 
            CancellationToken cancellationToken)
        {
            // if needed, lets add a `using ...` directive to properly import [DefaultValueAttribute]
            var tree = await document.GetSyntaxRootAsync(cancellationToken) as SyntaxNode;
            var compilationUnitSyntax = tree as CompilationUnitSyntax;
            if (compilationUnitSyntax is null)
            {
                return document;
            }

            // `System.ComponentModel`
            var systemComponentModelUsing = SyntaxFactory.QualifiedName(
                SyntaxFactory.IdentifierName("System"),
                SyntaxFactory.IdentifierName("ComponentModel"));

            compilationUnitSyntax = compilationUnitSyntax
                .AddUsingsIfNotExist(systemComponentModelUsing);

            return document.WithSyntaxRoot(compilationUnitSyntax);
        }

        private static bool TryBuildDiagnosticArguments(Diagnostic diagnostic, out DiagnosticArguments diagnosticArguments)
        {
            if (diagnostic.Properties.Count == 0)
            {
                diagnosticArguments = default;
                return false;
            }

            if (!diagnostic.Properties.TryGetValue(DefaultValueDiagnosticArgKey, out var defaultValue))
            {
                diagnosticArguments = default;
                return false;
            }

            diagnosticArguments = new() { DefaultValue = defaultValue };
            return true;
        }

        private struct DiagnosticArguments
        {
            public string DefaultValue { get; set; }
        }
    }
}
