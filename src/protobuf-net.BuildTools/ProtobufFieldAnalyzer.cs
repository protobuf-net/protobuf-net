using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.BuildTools.Internal;
using System;
using System.Collections.Generic;
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

        internal static readonly DiagnosticDescriptor DuplicateFieldNumber = new DiagnosticDescriptor(
            id: "PBN0003",
            title: nameof(ProtobufFieldAnalyzer) + "." + nameof(DuplicateFieldNumber),
            messageFormat: "The specified field number {0} is duplicated; field numbers must be unique between all declared members and includes on a single type.",
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

            var attribs = context.ContainingSymbol.GetAttributes();
            switch (context.ContainingSymbol)
            {
                case IFieldSymbol:
                case IPropertySymbol:
                    CheckValidFieldNumbersAndNames(ref context, ref attribs, "ProtoBuf.ProtoMemberAttribute");
                    break;
                case ITypeSymbol type:
                    bool isProtoContract = IsProtoContract(ref context, type, ref attribs);
                    HashSet<int>? knownFields = isProtoContract ? new HashSet<int>() : default;
                    CheckValidFieldNumbersAndNames(ref context, ref attribs, "ProtoBuf.ProtoIncludeAttribute", knownFields);
                    CheckValidFieldNumbersAndNames(ref context, ref attribs, "ProtoBuf.ProtoPartialMemberAttribute", knownFields, checkMemberNames: true);

                    if (knownFields is object)
                    {
                        var pma = context.SemanticModel.Compilation.GetTypeByMetadataName("ProtoBuf.ProtoMemberAttribute");
                        if (pma is object)
                        {
                            foreach (var member in type.GetMembers())
                            {
                                // also check the fields underneath, for duplicate field numbers
                                switch (member)
                                {
                                    case IPropertySymbol:
                                    case IFieldSymbol:
                                        foreach (var attrib in member.GetAttributes())
                                        {
                                            if (SymbolEqualityComparer.Default.Equals(attrib.AttributeClass, pma))
                                            {
                                                var args = attrib.ConstructorArguments;
                                                if (!args.IsEmpty && TryReadFieldNumber(args[0], out int fieldNumber))
                                                {
                                                    AddKnownField(ref context, knownFields, fieldNumber);
                                                }
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    break;
            }

            static void AddKnownField(ref SyntaxNodeAnalysisContext context, HashSet< int> knownFields, int fieldNumber)
            {
                if (!knownFields.Add(fieldNumber))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor: DuplicateFieldNumber,
                        location: context.Node.GetLocation(),
                        messageArgs: new object[] { fieldNumber },
                        additionalLocations: null,
                        properties: null
                    ));
                }
            }

            static bool TryReadFieldNumber(TypedConstant value, out int fieldNumber)
            {
                try
                {
                    if (value.Kind == TypedConstantKind.Primitive)
                    {
                        fieldNumber = Convert.ToInt32(value.Value);
                        return true;
                    }
                }
                catch { }
                fieldNumber = default;
                return false;
            }

            static void CheckValidFieldNumbersAndNames(ref SyntaxNodeAnalysisContext context, ref ImmutableArray<AttributeData> attribs, string attributeName, HashSet<int>? knownFields = null, bool checkMemberNames = false)
            {
                var lookForAttrib = context.SemanticModel.Compilation.GetTypeByMetadataName(attributeName);
                if (lookForAttrib is null) return;

                foreach (var attrib in attribs)
                {
                    if (SymbolEqualityComparer.Default.Equals(attrib.AttributeClass, lookForAttrib))
                    {
                        var args = attrib.ConstructorArguments;
                        if (args.IsEmpty) continue;

                        if (TryReadFieldNumber(args[0], out var fieldNumber))
                        {
                            if (knownFields is object) AddKnownField(ref context, knownFields, fieldNumber);
                            if (!IsLegalFieldNumber(fieldNumber, out var severity))
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
                        
                        if (checkMemberNames && args.Length > 1 && args[1].Kind == TypedConstantKind.Primitive && args[1].Value is string memberName
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
            }

            static bool IsProtoContract(ref SyntaxNodeAnalysisContext context, ITypeSymbol type, ref ImmutableArray<AttributeData> attribs)
            {
                var lookForAttrib = context.SemanticModel.Compilation.GetTypeByMetadataName("ProtoBuf.ProtoContractAttribute");
                if (lookForAttrib is null) return false;
                foreach (var attrib in attribs)
                {
                    if (SymbolEqualityComparer.Default.Equals(attrib.AttributeClass, lookForAttrib)) return true;
                }
                return false;
            }
        }
    }
}
