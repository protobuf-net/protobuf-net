using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.BuildTools.Internal;
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

        internal static readonly DiagnosticDescriptor DuplicateMemberName = new DiagnosticDescriptor(
            id: "PBN0008",
            title: nameof(ProtobufFieldAnalyzer) + "." + nameof(DuplicateMemberName),
            messageFormat: "The underlying member '{0}' is described multiple times.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ShouldBeProtoContract = new DiagnosticDescriptor(
            id: "PBN0009",
            title: nameof(ProtobufFieldAnalyzer) + "." + nameof(ShouldBeProtoContract),
            messageFormat: "The type is not marked as a proto-contract; additional annotations will be ignored.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DeclaredAndIgnored = new DiagnosticDescriptor(
            id: "PBN0010",
            title: nameof(ProtobufFieldAnalyzer) + "." + nameof(DeclaredAndIgnored),
            messageFormat: "The member '{0}' is marked to be ignored; additional annotations will be ignored.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateInclude = new DiagnosticDescriptor(
            id: "PBN0011",
            title: nameof(ProtobufFieldAnalyzer) + "." + nameof(DuplicateInclude),
            messageFormat: "The type '{0}' is declared as an include multiple times.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor IncludeNonDerived = new DiagnosticDescriptor(
            id: "PBN0012",
            title: nameof(ProtobufFieldAnalyzer) + "." + nameof(IncludeNonDerived),
            messageFormat: "The type '{0}' is declared as an include, but is not a direct sub-type.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);


        internal static readonly DiagnosticDescriptor IncludeNotDeclared = new DiagnosticDescriptor(
            id: "PBN0013",
            title: nameof(ProtobufFieldAnalyzer) + "." + nameof(IncludeNotDeclared),
            messageFormat: "The base-type '{0}' is a proto-contract, but no include is declared for '{1}'.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor SubTypeShouldBeProtoContract = new DiagnosticDescriptor(
            id: "PBN0014",
            title: nameof(ProtobufFieldAnalyzer) + "." + nameof(SubTypeShouldBeProtoContract),
            messageFormat: "The base-type '{0}' is a proto-contract; '{1}' should also be a proto-contract.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = Utils.GetDeclared(typeof(ProtobufFieldAnalyzer));

        private static readonly ImmutableArray<SyntaxKind> s_syntaxKinds =
            ImmutableArray.Create(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
        public override void Initialize(AnalysisContext ctx)
        {
            ctx.EnableConcurrentExecution();
            ctx.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            ctx.RegisterSyntaxNodeAction(ConsiderPossibleProtoBufType, s_syntaxKinds);
        }

        private static void ConsiderPossibleProtoBufType(SyntaxNodeAnalysisContext context)
        {
            if (!(context.ContainingSymbol is ITypeSymbol type)) return;
            
            var attribs = type.GetAttributes();

            TypeContext? typeContext = null;
            TypeContext Context() => typeContext ??= new TypeContext();

            foreach (var attrib in type.GetAttributes())
            {
                var ac = attrib.AttributeClass;
                if (ac is null) continue;
                switch (ac.Name)
                {
                    case nameof(ProtoContractAttribute) when ac.InProtoBufNamespace():
                        Context().SetContract(type, attrib);
                        break;
                    case nameof(ProtoIncludeAttribute) when ac.InProtoBufNamespace():
                        Context().AddInclude(type, attrib);
                        break;
                    case nameof(ProtoReservedAttribute) when ac.InProtoBufNamespace():
                        Context().AddReserved(type, attrib);
                        break;
                    case nameof(ProtoPartialMemberAttribute) when ac.InProtoBufNamespace():
                        Context().AddMember(type, attrib, null);
                        break;
                    case nameof(ProtoPartialIgnoreAttribute) when ac.InProtoBufNamespace():
                        Context().AddIgnore(type, attrib, null);
                        break;
                    case nameof(CompatibilityLevelAttribute) when ac.InProtoBufNamespace():
                        break;
                }
            }

            foreach (var member in type.GetMembers())
            {
                switch (member)
                {
                    case IPropertySymbol:
                    case IFieldSymbol:
                        var memberAttribs = member.GetAttributes();
                        foreach (var attrib in memberAttribs)
                        {
                            var ac = attrib.AttributeClass;
                            if (ac is null) continue;

                            switch (ac.Name)
                            {
                                case nameof(ProtoMemberAttribute) when ac.InProtoBufNamespace():
                                    Context().AddMember(member, attrib, member.Name);
                                    break;
                                case nameof(ProtoIgnoreAttribute) when ac.InProtoBufNamespace():
                                    Context().AddIgnore(member, attrib, member.Name);
                                    break;
                                case nameof(ProtoMapAttribute) when ac.InProtoBufNamespace():
                                    break;
                                case nameof(CompatibilityLevelAttribute) when ac.InProtoBufNamespace():
                                    break;
                            }
                        }
                        break;
                }
            }
            typeContext?.ReportProblems(context, type);

            if (type.BaseType is not null)
            {
                bool baseIsContract = false, currentTypeIsDeclared = false;
                foreach (var attrib in type.BaseType.GetAttributes())
                {
                    var ac = attrib.AttributeClass;
                    if (ac is null) continue;
                    switch (ac.Name)
                    {
                        case nameof(ProtoContractAttribute) when ac.InProtoBufNamespace():
                            baseIsContract = true;
                            break;
                        case nameof(ProtoIncludeAttribute) when ac.InProtoBufNamespace():
                            if (attrib.TryGetTypeByName(nameof(ProtoIncludeAttribute.KnownType), out var knownType) &&
                                SymbolEqualityComparer.Default.Equals(knownType, type))
                            {
                                currentTypeIsDeclared = true;
                            }
                            break;
                    }
                }
                if (baseIsContract)
                {
                    if (typeContext is null || !typeContext.IsContract)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: ProtobufFieldAnalyzer.SubTypeShouldBeProtoContract,
                            location: Utils.PickLocation(ref context, type),
                            messageArgs: new object[] { type.BaseType.ToDisplayString(), type.ToDisplayString() },
                            additionalLocations: null,
                            properties: null
                        ));
                    }
                    if (!currentTypeIsDeclared)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: ProtobufFieldAnalyzer.IncludeNotDeclared,
                            location: Utils.PickLocation(ref context, type),
                            messageArgs: new object[] { type.BaseType.ToDisplayString(), type.ToDisplayString() },
                            additionalLocations: null,
                            properties: null
                        ));
                    }

                }
            }
        }
    }
}
