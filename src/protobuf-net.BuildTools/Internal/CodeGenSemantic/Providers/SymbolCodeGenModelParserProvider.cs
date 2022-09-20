using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGenSemantic.Abstractions;
using ProtoBuf.Internal.CodeGenSemantic.Parsers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGenSemantic.Providers;

internal sealed class SymbolCodeGenModelParserProvider
{
    public static ISymbolCodeGenModelParser<INamespaceSymbol, CodeGenFile> GetNamespaceParser()
    {
        return new NamespaceCodeGenModelParser();
    }
    
    public static ISymbolCodeGenModelParser<ITypeSymbol, CodeGenType> GetTypeParser()
    {
        return new TypeCodeGenModelParser();
    }
    
    public static ISymbolCodeGenModelParser<IPropertySymbol, CodeGenField> GetFieldParser()
    {
        return new FieldCodeGenModelParser();
    }
}