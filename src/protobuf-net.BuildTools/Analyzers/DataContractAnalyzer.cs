#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.BuildTools.Internal;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace ProtoBuf.BuildTools.Analyzers
{
    /// <summary>
    /// Reports common usage errors in code that uses protobuf-net
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DataContractAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor InvalidFieldNumber = new(
            id: "PBN0001",
            title: nameof(DataContractAnalyzer) + "." + nameof(InvalidFieldNumber),
            messageFormat: "The specified field number {0} is invalid; the valid range is 1-536870911, omitting 19000-19999.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor MemberNotFound = new(
            id: "PBN0002",
            title: nameof(DataContractAnalyzer) + "." + nameof(MemberNotFound),
            messageFormat: "The specified type member '{0}' could not be resolved.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateFieldNumber = new(
            id: "PBN0003",
            title: nameof(DataContractAnalyzer) + "." + nameof(DuplicateFieldNumber),
            messageFormat: "The specified field number {0} is duplicated; field numbers must be unique between all declared members and includes on a single type.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ReservedFieldName = new(
            id: "PBN0004",
            title: nameof(DataContractAnalyzer) + "." + nameof(ReservedFieldName),
            messageFormat: "The specified field name '{0}' is explicitly reserved.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ReservedFieldNumber = new(
            id: "PBN0005",
            title: nameof(DataContractAnalyzer) + "." + nameof(ReservedFieldNumber),
            messageFormat: "The specified field number {0} is explicitly reserved.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateFieldName = new(
            id: "PBN0006",
            title: nameof(DataContractAnalyzer) + "." + nameof(DuplicateFieldName),
            messageFormat: "The specified field name '{0}' is duplicated; field names should be unique between all declared members on a single type.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateReservation = new(
            id: "PBN0007",
            title: nameof(DataContractAnalyzer) + "." + nameof(DuplicateReservation),
            messageFormat: "The reservations {0} and {1} overlap each-other.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateMemberName = new(
            id: "PBN0008",
            title: nameof(DataContractAnalyzer) + "." + nameof(DuplicateMemberName),
            messageFormat: "The underlying member '{0}' is described multiple times.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ShouldBeProtoContract = new(
            id: "PBN0009",
            title: nameof(DataContractAnalyzer) + "." + nameof(ShouldBeProtoContract),
            messageFormat: "The type is not marked as a proto-contract; additional annotations will be ignored.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DeclaredAndIgnored = new(
            id: "PBN0010",
            title: nameof(DataContractAnalyzer) + "." + nameof(DeclaredAndIgnored),
            messageFormat: "The member '{0}' is marked to be ignored; additional annotations will be ignored.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateInclude = new(
            id: "PBN0011",
            title: nameof(DataContractAnalyzer) + "." + nameof(DuplicateInclude),
            messageFormat: "The type '{0}' is declared as an include multiple times.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor IncludeNonDerived = new(
            id: "PBN0012",
            title: nameof(DataContractAnalyzer) + "." + nameof(IncludeNonDerived),
            messageFormat: "The type '{0}' is declared as an include, but is not a direct sub-type.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);


        internal static readonly DiagnosticDescriptor IncludeNotDeclared = new(
            id: "PBN0013",
            title: nameof(DataContractAnalyzer) + "." + nameof(IncludeNotDeclared),
            messageFormat: "The base-type '{0}' is a proto-contract, but no include is declared for '{1}' and the " + nameof(ProtoContractAttribute.IgnoreUnknownSubTypes) + " flag is not set.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor SubTypeShouldBeProtoContract = new(
            id: "PBN0014",
            title: nameof(DataContractAnalyzer) + "." + nameof(SubTypeShouldBeProtoContract),
            messageFormat: "The base-type '{0}' is a proto-contract and the " + nameof(ProtoContractAttribute.IgnoreUnknownSubTypes) + " flag is not set; '{1}' should also be a proto-contract.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ConstructorMissing = new(
            id: "PBN0015",
            title: nameof(DataContractAnalyzer) + "." + nameof(ConstructorMissing),
            messageFormat: "There is no suitable (parameterless) constructor available for the proto-contract, and the " + nameof(ProtoContractAttribute.SkipConstructor) + " flag is not set.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor MissingCompatibilityLevel = new(
            id: "PBN0016",
            title: nameof(DataContractAnalyzer) + "." + nameof(MissingCompatibilityLevel),
            messageFormat: "It is recommended to declare a module or assembly level " + nameof(CompatibilityLevel) + " (or declare it for each contract type); new projects should use the highest currently available - old projects should use " + nameof(CompatibilityLevel.Level200) + " unless fully considered.",
            helpLinkUri: "https://protobuf-net.github.io/protobuf-net/compatibilitylevel.html",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor EnumValueRedundant = new(
            id: "PBN0017",
            title: nameof(DataContractAnalyzer) + "." + nameof(EnumValueRedundant),
            messageFormat: "This " + nameof(ProtoEnumAttribute) + "." + nameof(ProtoEnumAttribute.Value) + " declaration is redundant; it is recommended to omit it.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor EnumValueNotSupported = new(
            id: "PBN0018",
            title: nameof(DataContractAnalyzer) + "." + nameof(EnumValueNotSupported),
            messageFormat: "This " + nameof(ProtoEnumAttribute) + "." + nameof(ProtoEnumAttribute.Value) + " declaration conflicts with the underlying value; this scenario is not supported from protobuf-net v3 onwards.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor EnumNameRedundant = new(
            id: "PBN0019",
            title: nameof(DataContractAnalyzer) + "." + nameof(EnumNameRedundant),
            messageFormat: "This " + nameof(ProtoEnumAttribute) + "." + nameof(ProtoEnumAttribute.Name) + " declaration is redundant; it is recommended to omit it.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        private static readonly ImmutableArray<DiagnosticDescriptor> s_SupportedDiagnostics = Utils.GetDeclared(typeof(DataContractAnalyzer));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => s_SupportedDiagnostics;

        private static readonly ImmutableArray<SyntaxKind> s_syntaxKinds =
            ImmutableArray.Create(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.EnumDeclaration);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext ctx)
        {
            ctx.EnableConcurrentExecution();
            ctx.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            ctx.RegisterSyntaxNodeAction(context => ConsiderPossibleProtoBufType(ref context), s_syntaxKinds);
            ctx.RegisterCompilationAction(context => ConsiderCompilation(ref context));
        }

        private void ConsiderCompilation(ref CompilationAnalysisContext context)
        {
            var pbVer = context.Compilation.GetProtobufNetVersion();
            if (pbVer is not null && pbVer.Major >= 3
                && !IsCompatibilityLevelDeclaredAtModuleOrAssembly(context.Compilation)
                && HasProtoContractsWithoutCompatibilityLevel(context.Compilation))
            {
                // so: it should be available, but not used, and there are proto-contracts that do not declare it
                context.ReportDiagnostic(Diagnostic.Create(
                        descriptor: DataContractAnalyzer.MissingCompatibilityLevel,
                        location: null,
                        messageArgs: null,
                        additionalLocations: null,
                        properties: null
                    ));
            }

            static bool HasProtoContractsWithoutCompatibilityLevel(Compilation compilation)
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
                            if (ac?.Name == nameof(CompatibilityLevelAttribute) && ac.InProtoBufNamespace())
                            {
                                hasCompatLevel = true;
                                break; // we don't need more
                            }
                            if (ac?.Name == nameof(ProtoContractAttribute) && ac.InProtoBufNamespace())
                            {
                                isProtoContract = true;
                            }
                        }
                        if (isProtoContract && !hasCompatLevel) return true;
                    }
                }
                return false;
            }

            static bool IsCompatibilityLevelDeclaredAtModuleOrAssembly(Compilation compilation)
            {
                foreach (var attrib in compilation.SourceModule.GetAttributes())
                {
                    var ac = attrib.AttributeClass;
                    if (ac?.Name == nameof(CompatibilityLevelAttribute) && ac.InProtoBufNamespace())
                        return true;
                }
                foreach (var attrib in compilation.Assembly.GetAttributes())
                {
                    var ac = attrib.AttributeClass;
                    if (ac?.Name == nameof(CompatibilityLevelAttribute) && ac.InProtoBufNamespace())
                        return true;
                }
                return false;
            }
        }

        private static void ConsiderPossibleProtoBufType(ref SyntaxNodeAnalysisContext context)
        {
            if (context.ContainingSymbol is not INamedTypeSymbol type) return;

            switch (type?.TypeKind)
            {
                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Interface:
                    ConsiderPossibleDataContractType(ref context, type);
                    break;
                case TypeKind.Enum:
                    ConsiderEnumType(ref context, type);
                    break;
            }
        }

        private static void ConsiderEnumType(ref SyntaxNodeAnalysisContext context, INamedTypeSymbol type)
        {
            foreach (var member in type.GetMembers())
            {
                switch (member.Kind)
                {
                    case SymbolKind.Field when member is IFieldSymbol field:
                        bool ignore = false;
                        int? overrideValue = null;
                        string? overrideName = null;
                        foreach (var attrib in field.GetAttributes())
                        {
                            var ac = attrib.AttributeClass;
                            
                            switch (ac?.Name)
                            {
                                case null: continue;
                                case nameof(ProtoIgnoreAttribute) when ac.InProtoBufNamespace():
                                    ignore = true;
                                    break;
                                case nameof(ProtoEnumAttribute) when ac.InProtoBufNamespace():
                                    if (attrib.TryGetInt32ByName(nameof(ProtoEnumAttribute.Value), out int value))
                                        overrideValue = value;
                                    if (attrib.TryGetStringByName(nameof(ProtoEnumAttribute.Name), out string s))
                                        overrideName = s;
                                    break;
                            }
                            if (ignore) break; // no point checking the rest
                        }
                        if (ignore) continue;

                        if (overrideValue is not null)
                        {
                            // - if the value is the same: it is redundant
                            // - if the value is different: it is not supported in v3
                            bool isMatch = field.ConstantValue switch
                            {
                                int i32 => i32 == overrideValue,
                                long i64 => i64 == (long)overrideValue,
                                short i16 => (int)i16 == overrideValue,
                                sbyte i8 => (int)i8 == overrideValue,
                                uint u32 => (long)u32 == (long)overrideValue,
                                ulong u64 => u64 <= int.MaxValue && (long)u64 == (long)overrideValue,
                                ushort u16 => (int)u16 == overrideValue,
                                byte u8 => (int)u8 == overrideValue,
                                char c => (int)c == overrideValue,
                                _ => false,
                            };

                            if (isMatch)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(
                                    descriptor: DataContractAnalyzer.EnumValueRedundant,
                                    location: Utils.PickLocation(ref context, field)
                                ));
                            }
                            else
                            {
                                var v = context.Compilation.GetProtobufNetVersion();
                                context.ReportDiagnostic(Diagnostic.Create(
                                    descriptor: DataContractAnalyzer.EnumValueNotSupported,
                                    location: Utils.PickLocation(ref context, field),
                                    // this is much more of a problem if the user is targeting v3
                                    effectiveSeverity: v?.Major >= 3 ? DiagnosticSeverity.Error : DiagnosticSeverity.Info,
                                    additionalLocations: null,
                                    properties: null
                                ));

                            }
                        }

                        if (overrideName is not null && overrideName == field.Name)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                    descriptor: DataContractAnalyzer.EnumNameRedundant,
                                    location: Utils.PickLocation(ref context, field)
                                ));
                        }
                        break;
                }
            }
        }

        private static void ConsiderPossibleDataContractType(ref SyntaxNodeAnalysisContext context, INamedTypeSymbol type)
        {
            var attribs = type.GetAttributes();

            DataContractContext? typeContext = null;
            DataContractContext Context() => typeContext ??= new DataContractContext();
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
                    && typeContext.HasFlag(DataContractContextFlags.IsProtoContract)
                    && !typeContext.HasFlag(DataContractContextFlags.SkipConstructor)
                )
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor: DataContractAnalyzer.ConstructorMissing,
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
                    if (typeContext is null || !typeContext.HasFlag(DataContractContextFlags.IsProtoContract))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: DataContractAnalyzer.SubTypeShouldBeProtoContract,
                            location: Utils.PickLocation(ref context, type),
                            messageArgs: new object[] { type.BaseType.ToDisplayString(), type.ToDisplayString() },
                            additionalLocations: null,
                            properties: null
                        ));
                    }
                    if (!currentTypeIsDeclared)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: DataContractAnalyzer.IncludeNotDeclared,
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
