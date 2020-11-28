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

        private readonly struct FieldReservation
        {
            public int From { get; }
            public int To { get; }
            public FieldReservation(int from)
                => From = To = from;

            public FieldReservation(int from, int to)
            {
                From = from;
                To = to;
            }
        }

        private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            if (context.ContainingSymbol is null) return;

            var attribs = context.ContainingSymbol.GetAttributes();
            switch (context.ContainingSymbol)
            {
                case IFieldSymbol:
                case IPropertySymbol:
                    CheckValidFieldNumbersAndNames(ref context, ref attribs, FieldCheckMode.Member, default, default);
                    break;
                case ITypeSymbol type:
                    bool isProtoContract = IsProtoContract(ref context, type, ref attribs);
                    HashSet<int>? knownFields = default;
                    List<FieldReservation>? reservations = default;

                    if (isProtoContract)
                    {
                        knownFields = new HashSet<int>();
                        reservations = new List<FieldReservation>();
                    }

                    CheckValidFieldNumbersAndNames(ref context, ref attribs, FieldCheckMode.TypeReservations, knownFields, reservations);
                    CheckValidFieldNumbersAndNames(ref context, ref attribs, FieldCheckMode.TypeIncludes, knownFields, reservations);
                    CheckValidFieldNumbersAndNames(ref context, ref attribs, FieldCheckMode.TypePartialMembers, knownFields, reservations);

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
                                                    AssertLegalFieldNumber(ref context, fieldNumber, knownFields, reservations, FieldCheckMode.TypeMembers, member);
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

            static void AssertLegalFieldNumber(ref SyntaxNodeAnalysisContext context, int fieldNumber, HashSet<int>? knownFields, List<FieldReservation>? reservations, FieldCheckMode mode, ISymbol? symbol)
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
                                    location: PickLocation(ref context, symbol),
                                    effectiveSeverity: severity,
                                    messageArgs: new object[] { fieldNumber },
                                    additionalLocations: null,
                                    properties: null
                                ));
                    }
                }

                if (mode != FieldCheckMode.TypeReservations)
                {   // reservations don't add to the set of known fields, nor do they need to be tested against reservations
                    if (knownFields is not null && !knownFields.Add(fieldNumber))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: DuplicateFieldNumber,
                            location: PickLocation(ref context, symbol),
                            messageArgs: new object[] { fieldNumber },
                            additionalLocations: null,
                            properties: null
                        ));
                    }

                    if (reservations is not null)
                    {
                        foreach (var reservation in reservations)
                        {
                            if (reservation.From <= fieldNumber && reservation.To >= fieldNumber)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(
                                    descriptor: ReservedFieldNumber,
                                    location: PickLocation(ref context, symbol),
                                    messageArgs: new object[] { fieldNumber },
                                    additionalLocations: null,
                                    properties: null
                                ));
                            }
                        }
                    }
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
                HashSet<int>? knownFields, List<FieldReservation>? reservations)
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
                            if (GetMemberMultiplicity(type, reservedName, out var found) != Multiplicity.Zero)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(
                                           descriptor: ReservedFieldName,
                                           location: PickLocation(ref context, found),
                                           messageArgs: new object[] { reservedName },
                                           additionalLocations: null,
                                           properties: null
                                       ));
                            }
                        }
                        else if (TryReadFieldNumber(args[0], out var fieldNumber))
                        {
                            AssertLegalFieldNumber(ref context, fieldNumber, knownFields, reservations, mode, default);

                            switch (mode)
                            {
                                case FieldCheckMode.TypePartialMembers
                                when args.Length > 1 && args[1].Kind == TypedConstantKind.Primitive && args[1].Value is string memberName
                                    && type is not null:

                                    if (GetMemberMultiplicity(type, memberName, out var found) != Multiplicity.One)
                                    {
                                        context.ReportDiagnostic(Diagnostic.Create(
                                           descriptor: MemberNotFound,
                                           location: PickLocation(ref context, found),
                                           messageArgs: new object[] { memberName },
                                           additionalLocations: null,
                                           properties: null
                                       ));
                                    }
                                    break;
                                case FieldCheckMode.TypeReservations
                                when args.Length > 1 && args[1].Kind == TypedConstantKind.Primitive && args[1].Value is not string
                                    && TryReadFieldNumber(args[1], out var endFieldNumber):
                                    AssertLegalFieldNumber(ref context, endFieldNumber, knownFields , reservations, mode, default);
                                    reservations?.Add(new FieldReservation(fieldNumber, endFieldNumber));
                                    break;
                                case FieldCheckMode.TypeReservations:
                                    // single field reservations
                                    reservations?.Add(new FieldReservation(fieldNumber));
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

            static Location PickLocation(ref SyntaxNodeAnalysisContext context, ISymbol? preferred)
            {
                if (preferred is null || preferred.Locations.IsEmpty) return context.Node.GetLocation();
                return preferred.Locations[0];
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
