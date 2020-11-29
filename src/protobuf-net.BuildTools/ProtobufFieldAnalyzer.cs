using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.BuildTools.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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

        internal static readonly DiagnosticDescriptor ReservedFieldName = new DiagnosticDescriptor(
            id: "PBN0004",
            title: nameof(ProtobufFieldAnalyzer) + "." + nameof(ReservedFieldName),
            messageFormat: "The specified field name '{0}' is explicitly reserved.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ReservedFieldNumber = new DiagnosticDescriptor(
            id: "PBN0005",
            title: nameof(ProtobufFieldAnalyzer) + "." + nameof(ReservedFieldNumber),
            messageFormat: "The specified field number {0} is explicitly reserved.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateFieldName = new DiagnosticDescriptor(
            id: "PBN0006",
            title: nameof(ProtobufFieldAnalyzer) + "." + nameof(DuplicateFieldName),
            messageFormat: "The specified field name '{0}' is duplicated; field names should be unique between all declared members on a single type.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateReservation = new DiagnosticDescriptor(
            id: "PBN0007",
            title: nameof(ProtobufFieldAnalyzer) + "." + nameof(DuplicateReservation),
            messageFormat: "The reservations {0} and {1} overlap each-other.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = Utils.GetDeclared(typeof(ProtobufFieldAnalyzer));

        public override void Initialize(AnalysisContext ctx)
        {
            ctx.EnableConcurrentExecution();
            ctx.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            ctx.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.PropertyDeclaration, SyntaxKind.FieldDeclaration, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
        }

        private enum FieldCheckMode
        {
            Member,
            TypeReservations,
            TypeIncludes,
            TypePartialMembers,
            TypeMembers,
        }

        private enum Multiplicity
        {
            Zero,
            One,
            Multiple
        }

        

        

        private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            if (context.ContainingSymbol is null) return;

            var attribs = context.ContainingSymbol.GetAttributes();
            switch (context.ContainingSymbol)
            {
                case IFieldSymbol field:
                    CheckValidFieldNumbersAndNames(ref context, ref attribs, FieldCheckMode.Member, default, context.ContainingSymbol, field.Name);
                    break;
                case IPropertySymbol prop:
                    CheckValidFieldNumbersAndNames(ref context, ref attribs, FieldCheckMode.Member, default, context.ContainingSymbol, prop.Name);
                    break;
                case ITypeSymbol type:
                    bool isProtoContract = IsProtoContract(ref context, type, ref attribs);

                    var typeContext = isProtoContract ? new TypeContext() : null;

                    CheckValidFieldNumbersAndNames(ref context, ref attribs, FieldCheckMode.TypeReservations, typeContext, context.ContainingSymbol, null);
                    CheckValidFieldNumbersAndNames(ref context, ref attribs, FieldCheckMode.TypeIncludes, typeContext, context.ContainingSymbol, null);
                    CheckValidFieldNumbersAndNames(ref context, ref attribs, FieldCheckMode.TypePartialMembers, typeContext, context.ContainingSymbol, null);

                    if (isProtoContract)
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
                                                    var fieldName = TryGetNonEmptyNamedString(attrib, nameof(ProtoMemberAttribute.Name)) ?? member.Name;
                                                    AssertLegalField(ref context, fieldName, fieldNumber, typeContext, FieldCheckMode.TypeMembers, member);
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

            static string? TryGetNonEmptyNamedString(AttributeData attributeData, string name)
            {
                foreach (var namedArg in attributeData.NamedArguments)
                {
                    if (namedArg.Key == name && namedArg.Value.Kind == TypedConstantKind.Primitive && namedArg.Value.Value is string s && s.Length != 0)
                    {
                        return s;
                    }
                }
                return null;
            }

            static void AssertLegalField(ref SyntaxNodeAnalysisContext context, string? name, int fieldNumber, TypeContext? typeContext, FieldCheckMode mode, ISymbol? symbol)
            {
                if (mode != FieldCheckMode.TypeMembers)
                {   // we'll check this bit on the member itself
                    var severity = fieldNumber switch
                    {
                        < 1 or > 536870911 => DiagnosticSeverity.Error, // legal range
                        >= 19000 and <= 19999 => DiagnosticSeverity.Warning, // reserved range; it'll work, but is a bad idea
                        _ => DiagnosticSeverity.Hidden,
                    };
                    if (severity != DiagnosticSeverity.Hidden)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                                    descriptor: InvalidFieldNumber,
                                    location: Utils.PickLocation(ref context, symbol),
                                    effectiveSeverity: severity,
                                    messageArgs: new object[] { fieldNumber },
                                    additionalLocations: null,
                                    properties: null
                                ));
                    }
                }

                if (typeContext is not null && mode != FieldCheckMode.TypeReservations)
                {   // reservations don't add to the set of known fields, nor do they need to be tested against reservations

                    if (typeContext.OverlapsField(fieldNumber))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: DuplicateFieldNumber,
                            location: Utils.PickLocation(ref context, symbol),
                            messageArgs: new object[] { fieldNumber },
                            additionalLocations: null,
                            properties: null
                        ));
                    }
                    if (typeContext.OverlapsReservation(fieldNumber, out var reservation))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: ReservedFieldNumber,
                            location: Utils.PickLocation(ref context, symbol),
                            messageArgs: new object[] { reservation },
                            additionalLocations: null,
                            properties: null
                        ));
                    }
                    if (name is not null)
                    {
                        if (typeContext.OverlapsField(name))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                descriptor: DuplicateFieldName,
                                location: Utils.PickLocation(ref context, symbol),
                                messageArgs: new object[] { name },
                                additionalLocations: null,
                                properties: null
                            ));
                        }

                        if (typeContext.OverlapsReservation(name, out reservation))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                descriptor: ReservedFieldName,
                                location: Utils.PickLocation(ref context, symbol),
                                messageArgs: new object[] { reservation },
                                additionalLocations: null,
                                properties: null
                            ));
                        }
                    }
                    typeContext.AddKnownField(name, fieldNumber);
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

            static void CheckValidFieldNumbersAndNames(ref SyntaxNodeAnalysisContext context, ref ImmutableArray<AttributeData> attribs, FieldCheckMode mode,
                TypeContext? typeContext, ISymbol? symbol, string? ambientName)
            {
                string? attributeName = mode switch
                {
                    FieldCheckMode.Member => "ProtoBuf.ProtoMemberAttribute",
                    FieldCheckMode.TypeReservations => "ProtoBuf.ProtoReservedAttribute",
                    FieldCheckMode.TypeIncludes => "ProtoBuf.ProtoIncludeAttribute",
                    FieldCheckMode.TypePartialMembers => "ProtoBuf.ProtoPartialMemberAttribute",
                    _ => default,
                };
                var lookForAttrib = attributeName is null ? default : context.SemanticModel.Compilation.GetTypeByMetadataName(attributeName);
                if (lookForAttrib is null) return;

                var type = context.ContainingSymbol as ITypeSymbol;
                foreach (var attrib in attribs)
                {
                    if (SymbolEqualityComparer.Default.Equals(attrib.AttributeClass, lookForAttrib))
                    {
                        var args = attrib.ConstructorArguments;
                        if (args.IsEmpty) continue;

                        if (mode == FieldCheckMode.TypeReservations && args[0].Value is string reservedName
                            && type is not null)
                        {
                            typeContext?.AddReservation(ref context, new FieldReservation(reservedName), symbol);
                        }
                        else if (TryReadFieldNumber(args[0], out var fieldNumber))
                        {
                            string? name = null;
                            switch(mode)
                            {
                                case FieldCheckMode.Member:
                                    name = TryGetNonEmptyNamedString(attrib, nameof(ProtoMemberAttribute.Name)) ?? ambientName;
                                    break;
                                case FieldCheckMode.TypePartialMembers:
                                    if (args.Length > 1 && args[1].Kind == TypedConstantKind.Primitive && args[1].Value is string assumedName)
                                    {
                                        name = assumedName;
                                    }
                                    break;
                            }
                            AssertLegalField(ref context, name, fieldNumber, typeContext, mode, symbol);

                            switch (mode)
                            {
                                case FieldCheckMode.TypePartialMembers
                                when args.Length > 1 && args[1].Kind == TypedConstantKind.Primitive && args[1].Value is string memberName
                                    && type is not null:

                                    if (GetMemberMultiplicity(type, memberName, out var found) != Multiplicity.One)
                                    {
                                        context.ReportDiagnostic(Diagnostic.Create(
                                           descriptor: MemberNotFound,
                                           location: Utils.PickLocation(ref context, found),
                                           messageArgs: new object[] { memberName },
                                           additionalLocations: null,
                                           properties: null
                                       ));
                                    }
                                    break;
                                case FieldCheckMode.TypeReservations
                                when args.Length > 1 && args[1].Kind == TypedConstantKind.Primitive && args[1].Value is not string
                                    && TryReadFieldNumber(args[1], out var endFieldNumber):
                                    AssertLegalField(ref context, default, endFieldNumber, typeContext, mode, default);
                                    typeContext?.AddReservation(ref context, new FieldReservation(fieldNumber, endFieldNumber), symbol);
                                    break;
                                case FieldCheckMode.TypeReservations:
                                    // single field reservations
                                    typeContext?.AddReservation(ref context, new FieldReservation(fieldNumber), symbol);
                                    break;
                            }
                        }
                    }
                }

                static Multiplicity GetMemberMultiplicity(ITypeSymbol type, string memberName, out ISymbol? found)
                {
                    found = null;
                    foreach (var member in type.GetMembers())
                    {
                        if (member.Name == memberName)
                        {
                            if (found is not null) return Multiplicity.Multiple;
                            found = member;
                        }
                    }
                    return found is not null ? Multiplicity.One : Multiplicity.Zero;
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
