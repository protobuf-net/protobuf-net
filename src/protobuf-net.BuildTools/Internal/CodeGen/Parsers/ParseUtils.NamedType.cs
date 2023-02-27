#nullable enable

using Microsoft.CodeAnalysis;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal static partial class ParseUtils
{
    public static object? ParseNamedType(in CodeGenFileParseContext ctx, INamedTypeSymbol type)
    {
        switch (type.TypeKind)
        {
            case TypeKind.Struct:
            case TypeKind.Class:
                return ParseUtils.ParseMessage(in ctx,type);
            case TypeKind.Enum:
                return ParseUtils.ParseEnum(in ctx, type);
            case TypeKind.Interface:
                return ParseUtils.ParseService(in ctx, type);
        }
        return null;
    }
}