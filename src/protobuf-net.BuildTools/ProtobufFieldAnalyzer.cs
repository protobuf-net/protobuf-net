#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.BuildTools.Internal;
using System.Collections.Immutable;
using System.Linq;

namespace ProtoBuf.BuildTools
{
    /// <summary>
    /// Reports common usage errors in code that uses protobuf-net
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ProtoBufFieldAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor InvalidFieldNumber = new(
            id: "PBN0001",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(InvalidFieldNumber),
            messageFormat: "The specified field number {0} is invalid; the valid range is 1-536870911, omitting 19000-19999.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor MemberNotFound = new(
            id: "PBN0002",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(MemberNotFound),
            messageFormat: "The specified type member '{0}' could not be resolved.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateFieldNumber = new(
            id: "PBN0003",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(DuplicateFieldNumber),
            messageFormat: "The specified field number {0} is duplicated; field numbers must be unique between all declared members and includes on a single type.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ReservedFieldName = new(
            id: "PBN0004",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(ReservedFieldName),
            messageFormat: "The specified field name '{0}' is explicitly reserved.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ReservedFieldNumber = new(
            id: "PBN0005",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(ReservedFieldNumber),
            messageFormat: "The specified field number {0} is explicitly reserved.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateFieldName = new(
            id: "PBN0006",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(DuplicateFieldName),
            messageFormat: "The specified field name '{0}' is duplicated; field names should be unique between all declared members on a single type.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateReservation = new(
            id: "PBN0007",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(DuplicateReservation),
            messageFormat: "The reservations {0} and {1} overlap each-other.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateMemberName = new(
            id: "PBN0008",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(DuplicateMemberName),
            messageFormat: "The underlying member '{0}' is described multiple times.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ShouldBeProtoContract = new(
            id: "PBN0009",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(ShouldBeProtoContract),
            messageFormat: "The type is not marked as a proto-contract; additional annotations will be ignored.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DeclaredAndIgnored = new(
            id: "PBN0010",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(DeclaredAndIgnored),
            messageFormat: "The member '{0}' is marked to be ignored; additional annotations will be ignored.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateInclude = new(
            id: "PBN0011",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(DuplicateInclude),
            messageFormat: "The type '{0}' is declared as an include multiple times.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor IncludeNonDerived = new(
            id: "PBN0012",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(IncludeNonDerived),
            messageFormat: "The type '{0}' is declared as an include, but is not a direct sub-type.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);


        internal static readonly DiagnosticDescriptor IncludeNotDeclared = new(
            id: "PBN0013",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(IncludeNotDeclared),
            messageFormat: "The base-type '{0}' is a proto-contract, but no include is declared for '{1}' and the " + nameof(ProtoContractAttribute.IgnoreUnknownSubTypes) + " flag is not set.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor SubTypeShouldBeProtoContract = new(
            id: "PBN0014",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(SubTypeShouldBeProtoContract),
            messageFormat: "The base-type '{0}' is a proto-contract and the " + nameof(ProtoContractAttribute.IgnoreUnknownSubTypes) + " flag is not set; '{1}' should also be a proto-contract.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ConstructorMissing = new(
            id: "PBN0015",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(ConstructorMissing),
            messageFormat: "There is no suitable (parameterless) constructor available for the proto-contract, and the " + nameof(ProtoContractAttribute.SkipConstructor) + " flag is not set.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor MissingCompatibilityLevel = new(
            id: "PBN0016",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(MissingCompatibilityLevel),
            messageFormat: "It is recommended to declare a module or assembly level " + nameof(CompatibilityLevel) + " (or declare it for each contract type); new projects should use the highest currently available - old projects should use " + nameof(CompatibilityLevel.Level200) + " unless fully considered.",
            helpLinkUri: "https://protobuf-net.github.io/protobuf-net/compatibilitylevel.html",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = Utils.GetDeclared(typeof(ProtoBufFieldAnalyzer));

        private static readonly ImmutableArray<SyntaxKind> s_syntaxKinds =
            ImmutableArray.Create(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext ctx)
        {
            ctx.EnableConcurrentExecution();
            ctx.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            ctx.RegisterSyntaxNodeAction(context => ConsiderPossibleProtoBufType(context), s_syntaxKinds);
            ctx.RegisterCompilationAction(context => ConsiderCompilation(context));
        }

        private void ConsiderCompilation(CompilationAnalysisContext context)
        {
            var attribType = context.Compilation.GetTypeByMetadataName(Utils.ProtoBufNamespace + "." + nameof(CompatibilityLevelAttribute));
            if (attribType is not null && !IsDeclaredAtModuleOrAssembly(context.Compilation, attribType)
                && HasProtoContractsWithoutCompatibilityLevel(context.Compilation, attribType))
            {
                // so: it is available, but not used, and there are proto-contracts that do not declare it
                context.ReportDiagnostic(Diagnostic.Create(
                        descriptor: ProtoBufFieldAnalyzer.MissingCompatibilityLevel,
                        location: null,
                        messageArgs: null,
                        additionalLocations: null,
                        properties: null
                    ));
            }

            static bool HasProtoContractsWithoutCompatibilityLevel(Compilation compilation, INamedTypeSymbol attribType)
            {
                foreach(var typeName in compilation.Assembly.TypeNames)
                {
                    var type = compilation.Assembly.GetTypeByMetadataName(typeName);
                    if (type is not null)
                    {
                        bool hasCompatLevel = false, isProtoContract = false;
                        foreach (var attrib in type.GetAttributes())
                        {
                            var ac = attrib.AttributeClass;
                            if (ac == null) continue;
                            if (SymbolEqualityComparer.Default.Equals(ac, attribType))
                            {
                                hasCompatLevel = true;
                                break; // we don't need more
                            }
                            if (ac.Name == nameof(ProtoContractAttribute) && ac.InProtoBufNamespace())
                            {
                                isProtoContract = true;
                            }
                        }
                        if (isProtoContract && !hasCompatLevel) return true;
                    }
                }
                return false;
            }

            static bool IsDeclaredAtModuleOrAssembly(Compilation compilation, INamedTypeSymbol attribType)
            {
                foreach (var attrib in compilation.SourceModule.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attrib.AttributeClass, attribType))
                    {
                        return true;
                    }
                }
                foreach (var attrib in compilation.Assembly.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attrib.AttributeClass, attribType))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private static void ConsiderPossibleProtoBufType(SyntaxNodeAnalysisContext context)
        {
            if (context.ContainingSymbol is not ITypeSymbol type) return;
            
            var attribs = type.GetAttributes();

            TypeContext? typeContext = null;
            TypeContext Context() => typeContext ??= new TypeContext();
            bool hasAnyConstructor = false, hasParameterlessConstructor = false;
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
                        Context();
                        break;
                }
            }

            Location? ctorLocation = null;
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
                                    Context();
                                    break;
                                case nameof(CompatibilityLevelAttribute) when ac.InProtoBufNamespace():
                                    Context();
                                    break;
                            }
                        }
                        break;
                    case IMethodSymbol method when method.MethodKind == MethodKind.Constructor:
                        hasAnyConstructor = true;
                        if (method.Parameters.Any())
                        {
                            if (ctorLocation is null && method.Locations.Length != 0)
                            {
                                ctorLocation = method.Locations[0];
                            }
                        }
                        else
                        {
                            hasParameterlessConstructor = true;
                        }
                        break;
                }
            }
            if (typeContext is not null)
            {
                if (!type.IsAbstract // the library won't be directly creating it, so: N/A
                    && hasAnyConstructor && !hasParameterlessConstructor
                    && typeContext.HasFlag(TypeContextFlags.IsProtoContract)
                    && !typeContext.HasFlag(TypeContextFlags.SkipConstructor)
                )
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor: ProtoBufFieldAnalyzer.ConstructorMissing,
                        location: ctorLocation ?? Utils.PickLocation(ref context, type),
                        messageArgs: null,
                        additionalLocations: null,
                        properties: null
                    ));
                }
                typeContext.ReportProblems(context, type);
            }

            if (type.BaseType is not null)
            {
                bool baseIsContract = false, currentTypeIsDeclared = false, baseSkipsUnknownSubtypes = false;
                foreach (var attrib in type.BaseType.GetAttributes())
                {
                    var ac = attrib.AttributeClass;
                    if (ac is null) continue;
                    switch (ac.Name)
                    {
                        case nameof(ProtoContractAttribute) when ac.InProtoBufNamespace():
                            if (attrib.TryGetBooleanByName(nameof(ProtoContractAttribute.IgnoreUnknownSubTypes), out var b))
                                baseSkipsUnknownSubtypes = b;
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
                if (baseIsContract && !baseSkipsUnknownSubtypes)
                {
                    if (typeContext is null || !typeContext.HasFlag(TypeContextFlags.IsProtoContract))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: ProtoBufFieldAnalyzer.SubTypeShouldBeProtoContract,
                            location: Utils.PickLocation(ref context, type),
                            messageArgs: new object[] { type.BaseType.ToDisplayString(), type.ToDisplayString() },
                            additionalLocations: null,
                            properties: null
                        ));
                    }
                    if (!currentTypeIsDeclared)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: ProtoBufFieldAnalyzer.IncludeNotDeclared,
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
