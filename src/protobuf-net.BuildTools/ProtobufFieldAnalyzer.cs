using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.BuildTools.Internal;
using System;
using System.Collections.Immutable;
using System.Security;

namespace ProtoBuf.BuildTools
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ProtobufFieldAnalyzer : DiagnosticAnalyzer
    {
        internal static DiagnosticDescriptor InvalidFieldNumber { get; } = new DiagnosticDescriptor(
            id: "PBN0001",
            title: nameof(ProtobufFieldAnalyzer) + "." + nameof(InvalidFieldNumber),
            messageFormat: "The specified field number {0} is invalid; the valid range is 1-536870911, omitting 19000-19999.",
            category: Literals.CategorySerialization,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(InvalidFieldNumber);

        public override void Initialize(AnalysisContext ctx)
        {
            ctx.EnableConcurrentExecution();
            ctx.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            ctx.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.PropertyDeclaration, SyntaxKind.FieldDeclaration);
        }

        private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            if (context.ContainingSymbol is null) return;

            INamedTypeSymbol? pma = context.SemanticModel.Compilation.GetTypeByMetadataName("ProtoBuf.ProtoMemberAttribute");
            foreach (var attrib in context.ContainingSymbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attrib.AttributeClass, pma))
                {
                    var args = attrib.ConstructorArguments;
                    if (!args.IsEmpty && args[0].Kind == TypedConstantKind.Primitive && TryParseFieldNumber(args[0].Value, out int fieldNumber)
                        && !IsLegalFieldNumber(fieldNumber, out var severity))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: InvalidFieldNumber,
                            location: context.Node.GetLocation(),
                            effectiveSeverity: severity,
                            messageArgs: new object[] { fieldNumber },
                            additionalLocations: null,
                            properties: null
                        ));
                    }
                }
            }

            static bool IsLegalFieldNumber(int fieldNumber, out DiagnosticSeverity severity)
            {
                if (fieldNumber < 1 || fieldNumber > 536870911)
                {
                    severity = DiagnosticSeverity.Error;
                    return false;
                }
                if (fieldNumber >= 19000 && fieldNumber <= 19999)
                {
                    severity = DiagnosticSeverity.Warning;
                    return false;
                }
                severity = default;
                return true;
            }

            static bool TryParseFieldNumber(object? raw, out int typed)
            {
                try
                {
                    typed = Convert.ToInt32(raw);
                    return true;
                }
                catch
                {
                    typed = default;
                    return false;
                }
            }
        }
    }
}
