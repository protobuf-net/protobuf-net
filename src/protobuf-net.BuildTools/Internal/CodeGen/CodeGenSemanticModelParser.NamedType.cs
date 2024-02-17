#nullable enable

using Microsoft.CodeAnalysis;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen;

internal partial class CodeGenSemanticModelParser
{
    public CodeGenType ParseNamedType(INamedTypeSymbol type)
    {
        if (TryGetType(type, out CodeGenType result))
        {
            return result; // re-use existing
        }
        switch (type.TypeKind)
        {
            case TypeKind.Struct:
            case TypeKind.Class:
                return ParseMessage(type);
            case TypeKind.Enum:
                return ParseEnum(type);
            case TypeKind.Interface:
                return ParseService(type);
        }
        return CodeGenType.Unknown;
    }
}