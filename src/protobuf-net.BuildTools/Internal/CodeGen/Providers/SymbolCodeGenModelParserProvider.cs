using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGen.Abstractions;
using ProtoBuf.Internal.CodeGen.Parsers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Providers;

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