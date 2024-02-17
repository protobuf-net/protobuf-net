#nullable enable

using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.BuildTools.Internal;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal static partial class ParseUtils
{
    internal static readonly DiagnosticDescriptor UnhandledAttribute = new(
            id: "PBN3001",
            title: nameof(DataContractGenerator) + "." + nameof(UnhandledAttribute),
            messageFormat: "The attribute {0} is not currently handled",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor UnhandledAttributeValue = new(
            id: "PBN3002",
            title: nameof(DataContractGenerator) + "." + nameof(UnhandledAttributeValue),
            messageFormat: "The attribute value {0}.{1} is not currently handled",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
}
