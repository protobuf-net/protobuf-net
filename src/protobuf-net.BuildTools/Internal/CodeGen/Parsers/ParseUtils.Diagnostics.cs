#nullable enable

using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.BuildTools.Internal;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal static partial class ParseUtils
{
    internal static readonly DiagnosticDescriptor EnumPropertyLacksAttribute = new(
            id: "PBN3001",
            title: nameof(DataContractGenerator) + "." + nameof(EnumPropertyLacksAttribute),
            messageFormat: $"Failed to find a '{nameof(ProtoMemberAttribute)}' attribute within enum property definition",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor EnumValueLacksAttribute = new(
            id: "PBN3002",
            title: nameof(DataContractGenerator) + "." + nameof(EnumValueLacksAttribute),
            messageFormat: $"Failed to find a '{nameof(ProtoEnumAttribute)}' attribute within enum value definition",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor EnumTypeLacksAttribute = new(
            id: "PBN3003",
            title: nameof(DataContractGenerator) + "." + nameof(EnumTypeLacksAttribute),
            messageFormat: $"Failed to find a '{nameof(ProtoContractAttribute)}' attribute within enum type definition",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
}
