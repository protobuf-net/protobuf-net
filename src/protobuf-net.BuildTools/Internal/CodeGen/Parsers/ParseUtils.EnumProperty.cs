#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal static partial class ParseUtils
{
    public static CodeGenEnum? ParseEnumProperty(in CodeGenFileParseContext ctx, IPropertySymbol symbol)
    {
        var propertyAttributes = symbol.GetAttributes();
        if (ParseUtils.IsProtoMember(propertyAttributes, out var protoMemberAttribute))
        {
            var (_, originalName, dataFormat, _) = ParseUtils.GetProtoMemberAttributeData(protoMemberAttribute);
            var codeGenField = new CodeGenEnum(symbol.Name, symbol.GetFullyQualifiedPrefix())
            {
                OriginalName = originalName,
                Type = symbol.Type.TryResolveKnownCodeGenType(dataFormat) ?? CodeGenType.Unknown,
                Emit = CodeGenGenerate.None, // nothing to emit
            };
        
            return codeGenField;
        }

        ctx.ReportDiagnostic(EnumPropertyLacksAttribute, symbol);
        return null;
    }
}