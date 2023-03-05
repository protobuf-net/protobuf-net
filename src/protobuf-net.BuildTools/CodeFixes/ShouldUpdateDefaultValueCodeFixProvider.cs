using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using ProtoBuf.BuildTools.Analyzers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;

namespace ProtoBuf.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ShouldUpdateDefaultValueCodeFixProvider)), Shared]
    public class ShouldUpdateDefaultValueCodeFixProvider : CodeFixProvider
    {
        const string CodeFixTitle = "Update [DefaultValue] attribute";
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DataContractAnalyzer.ShouldUpdateDefault.Id);

        /// <summary>
        /// Key of for a <see cref="KeyValuePair{TKey,TValue}"/> of diagnostic properties,
        /// containing <see cref="DefaultValueAttribute"/> constructor value to be inserted into code
        /// </summary>
        public const string UpdateValueDiagnosticArgKey = "UpdateValueDiagnosticArgKey";

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // probably Roslyn does the check by itself, but lets check that there are diagnostics to investigate it
            if (!context.Diagnostics.Any()) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticLocationSpan = diagnostic.Location.SourceSpan;
                var diagnosticTextSpan = new TextSpan(diagnosticLocationSpan.Start, diagnosticLocationSpan.Length);

                var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                var defaultValueAttributeSyntax = root?.FindNode(diagnosticTextSpan) as AttributeSyntax;
                if (defaultValueAttributeSyntax is null) return;
                
                if (!TryBuildDiagnosticArguments(diagnostic, out var diagnosticArguments)) return;
                
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixTitle,
                        createChangedSolution: 
                            cancellationToken => UpdateDefaultValueAttributeWithValueAsync(
                                context.Document, defaultValueAttributeSyntax, diagnosticArguments, cancellationToken),
                        equivalenceKey: nameof(ShouldUpdateDefaultValueCodeFixProvider)),
                    diagnostic);
            }
        }

        private async Task<Solution> UpdateDefaultValueAttributeWithValueAsync(
            Document document,
            AttributeSyntax defaultValueAttributeSyntax,
            DiagnosticArguments diagnosticArguments,
            CancellationToken cancellationToken)
        {
            var argumentList = defaultValueAttributeSyntax.ArgumentList;
            if (argumentList is null) return document.Project.Solution;
            
            var updatedArgumentList = argumentList.WithArguments(
                SyntaxFactory.SeparatedList(new[] { BuildDefaultValueArgumentSyntax() })
            );

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (oldRoot is null) return document.Project.Solution;
            
            var newRoot = oldRoot.ReplaceNode(argumentList, updatedArgumentList);
            document = document.WithSyntaxRoot(newRoot);
            return document.Project.Solution;

            AttributeArgumentSyntax BuildDefaultValueArgumentSyntax()
                => SyntaxFactory.AttributeArgument(
                    default, default, SyntaxFactory.ParseExpression(diagnosticArguments.DefaultValue));
        }
        
        private static bool TryBuildDiagnosticArguments(Diagnostic diagnostic, out DiagnosticArguments diagnosticArguments)
        {
            if (diagnostic.Properties.Count == 0)
            {
                diagnosticArguments = default;
                return false;
            }

            if (!diagnostic.Properties.TryGetValue(UpdateValueDiagnosticArgKey, out var defaultValue))
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
