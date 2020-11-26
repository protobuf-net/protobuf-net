using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.BuildTools.Internal;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace ProtoBuf.BuildTools
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ProtobufFieldAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor InvalidFieldNumber = new DiagnosticDescriptor(
            id: "PBN0001",
            title: nameof(ProtobufFieldAnalyzer) + "." + nameof(InvalidFieldNumber),
            messageFormat: "The specified field number {0} is invalid; the valid range is 1-536870911, omitting 19000-19999.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor MemberNotFound = new DiagnosticDescriptor(
            id: "PBN0002",
            title: nameof(ProtobufFieldAnalyzer) + "." + nameof(MemberNotFound),
            messageFormat: "The specified type member '{0}' could not be resolved.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = Utils.GetDeclared(typeof(ProtobufFieldAnalyzer));

        public override void Initialize(AnalysisContext ctx)
        {
            ctx.EnableConcurrentExecution();
            ctx.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            ctx.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.PropertyDeclaration, SyntaxKind.FieldDeclaration, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
        }

        private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            if (context.ContainingSymbol is null) return;

            var lookForAttrib = context.ContainingSymbol switch {
                IPropertySymbol or IFieldSymbol => context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(ProtoMemberAttribute).FullName),
                ITypeSymbol => context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(ProtoPartialMemberAttribute).FullName),
                _ => null,
            };

            if (lookForAttrib is null) return;

            foreach (var attrib in context.ContainingSymbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attrib.AttributeClass, lookForAttrib))
                {
                    var args = attrib.ConstructorArguments;
                    if (args.IsEmpty) continue;

                    if (args[0].Kind == TypedConstantKind.Primitive && TryParseFieldNumber(args[0].Value, out int fieldNumber)
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
                    if (args.Length > 1 && args[1].Kind == TypedConstantKind.Primitive && args[1].Value is string memberName
                        && context.ContainingSymbol is ITypeSymbol type)
                    {
                        int count = 0;
                        foreach (var member in type.GetMembers())
                        {
                            if (member.Name == memberName)
                            {
                                count++;
                                if (count > 1) break; // that's enough to detect a problem
                            }
                        }
                        if (count != 1) // single unique match
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                               descriptor: MemberNotFound,
                               location: context.Node.GetLocation(),
                               messageArgs: new object[] { memberName },
                               additionalLocations: null,
                               properties: null
                           ));
                        }
                    }
                }
            }

            static bool IsLegalFieldNumber(int fieldNumber, out DiagnosticSeverity severity)
            {
                const int FieldMinNumber = 1, FieldMaxNumber = 536870911, FieldReservationStart = 19000, FieldReservationEnd = 19999;

                if (fieldNumber < FieldMinNumber || fieldNumber > FieldMaxNumber)
                {
                    severity = DiagnosticSeverity.Error;
                    return false;
                }
                if (fieldNumber >= FieldReservationStart && fieldNumber <= FieldReservationEnd)
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
