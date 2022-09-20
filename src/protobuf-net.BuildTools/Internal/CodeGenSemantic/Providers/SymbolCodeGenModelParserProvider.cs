using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGenSemantic.Abstractions;
using ProtoBuf.Internal.CodeGenSemantic.Parsers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGenSemantic.Providers;

internal static class SymbolCodeGenModelParserProvider
{
    public static ISymbolCodeGenModelParser<INamespaceSymbol, CodeGenFile> GetFileParser()
    {
        return new NamespaceCodeGenModelParser();
    }

    public static ISymbolCodeGenModelParser<ITypeSymbol, CodeGenMessage> GetMessageParser()
    {
        return new MessageCodeGenModelParser();
    }
    
    public static ISymbolCodeGenModelParser<ITypeSymbol, CodeGenEnum> GetEnumParser()
    {
        return new EnumCodeGenModelParser();
    }
    
    public static ISymbolCodeGenModelParser<IPropertySymbol, CodeGenField> GetFieldParser()
    {
        return new FieldPropertyCodeGenModelParser();
    }
    
    public static ISymbolCodeGenModelParser<IPropertySymbol, CodeGenEnum> GetEnumPropertyParser()
    {
        return new EnumPropertyCodeGenModelParser();
    }
    
    public static ISymbolCodeGenModelParser<IFieldSymbol, CodeGenEnumValue> GetEnumValueParser()
    {
        return new EnumValueCodeGenModelParser();
    }
}