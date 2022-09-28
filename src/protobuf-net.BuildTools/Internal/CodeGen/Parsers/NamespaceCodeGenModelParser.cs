#nullable enable

using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGen.Abstractions;
using ProtoBuf.Internal.CodeGen.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal sealed class NamedTypeCodeGenModelParser : SymbolCodeGenModelParserBase<INamedTypeSymbol, object?>
{
    public NamedTypeCodeGenModelParser(SymbolCodeGenModelParserProvider parserProvider) : base(parserProvider)
    {
    }

    public override object? Parse(INamedTypeSymbol type)
    {
        switch (type.TypeKind)
        {
            case TypeKind.Struct:
            case TypeKind.Class:
                var messageParser = ParserProvider.GetMessageParser();
                return messageParser.Parse(type);
            case TypeKind.Enum:
                var enumParser = ParserProvider.GetEnumParser();
                return enumParser.Parse(type);
            case TypeKind.Interface:
                var serviceParser = ParserProvider.GetServiceParser();
                return serviceParser.Parse(type);
        }
        return null;
    }
}