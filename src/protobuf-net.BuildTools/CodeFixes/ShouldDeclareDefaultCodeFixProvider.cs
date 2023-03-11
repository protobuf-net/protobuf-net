using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
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
using ProtoBuf.Internal;

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
        internal const string DefaultValueStringRepresentationArgKey = "DefaultValueStringRepresentationArgKey";
        
        /// <summary>
        /// 'object.ToString()' representation.
        /// </summary>
        internal const string DefaultValueCalculatedArgKey = "DefaultValueCalculatedArgKey";
        
        /// <summary>
        /// <see cref="SpecialType"/> value of member type. Helps to consider which syntax to use
        /// </summary>
        internal const string MemberSpecialTypeArgKey = "MemberSpecialTypeArgKey";

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
                        UseShortSyntax(diagnosticArguments.MemberSpecialType)
                        ? BuildDefaultValueShortArgumentSyntax()
                        : BuildDefaultValueLongArgumentSyntax()
                    )
                )
            );

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (oldRoot is null) return document;
            
            var newRoot = oldRoot.ReplaceNode(attributeList, updatedArgumentList);
            return document.WithSyntaxRoot(newRoot);

            SeparatedSyntaxList<AttributeArgumentSyntax> BuildDefaultValueShortArgumentSyntax()
                => SyntaxFactory.SeparatedList(new []
                {
                    SyntaxFactory.AttributeArgument(
                        default, default, SyntaxFactory.ParseExpression(diagnosticArguments.DefaultValueStringRepresentation))
                });
            
            SeparatedSyntaxList<AttributeArgumentSyntax> BuildDefaultValueLongArgumentSyntax()
                => SyntaxFactory.SeparatedList(new []
                {
                    SyntaxFactory.AttributeArgument(default, default,
                        SyntaxFactory.ParseExpression($"typeof({diagnosticArguments.MemberSpecialType.GetSpecialTypeCSharpKeyword()})")),
                    
                    SyntaxFactory.AttributeArgument(default, default, 
                        SyntaxFactory.ParseExpression("\"" + diagnosticArguments.DefaultValueCalculated + "\""))
                });
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

            compilationUnitSyntax = compilationUnitSyntax
                .AddUsingsIfNotExist("System.ComponentModel");

            return document.WithSyntaxRoot(compilationUnitSyntax);
        }

        private static bool TryBuildDiagnosticArguments(Diagnostic diagnostic, out DiagnosticArguments diagnosticArguments)
        {
            if (diagnostic.Properties.Count == 0)
            {
                diagnosticArguments = default;
                return false;
            }

            if (!diagnostic.Properties.TryGetValue(DefaultValueStringRepresentationArgKey, out var defaultValueStringRepresentation))
            {
                diagnosticArguments = default;
                return false;
            }
            
            if (!diagnostic.Properties.TryGetValue(DefaultValueCalculatedArgKey, out var defaultValueCalculated))
            {
                diagnosticArguments = default;
                return false;
            }
            
            if (!diagnostic.Properties.TryGetValue(MemberSpecialTypeArgKey, out var memberSpecialTypeRaw)
                || !Enum.TryParse<SpecialType>(memberSpecialTypeRaw, out var memberSpecialType))
            {
                diagnosticArguments = default;
                return false;
            }

            diagnosticArguments = new()
            {
                DefaultValueStringRepresentation = defaultValueStringRepresentation,
                DefaultValueCalculated = defaultValueCalculated,
                MemberSpecialType = memberSpecialType
            };
            return true;
        }

        /// <summary>
        /// Some of known types can not use easy "[<see cref="DefaultValueAttribute"/>(value)]" syntax
        /// and instead we can use <see cref="DefaultValueAttribute"/>(typeof(type), "rawValue") syntax
        /// </summary>
        private static bool UseShortSyntax(SpecialType specialType) => specialType switch
        {
            SpecialType.System_Decimal => false,
            _ => true
        };

        private struct DiagnosticArguments
        {
            public string DefaultValueStringRepresentation { get; set; }
            public string DefaultValueCalculated { get; set; }
            public SpecialType MemberSpecialType { get; set; }
        }
    }
}
