using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGen.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Abstractions;

internal abstract class SymbolCodeGenModelParserBase<TSymbol, TCodeGenModel> 
    : ISymbolCodeGenModelParser<TSymbol, TCodeGenModel> where TSymbol : ISymbol
{
    protected SymbolCodeGenModelParserProvider ParserProvider { get; }

    protected CodeGenParseContext ParseContext => ParserProvider.CodeGenParseContext;
    
    public SymbolCodeGenModelParserBase(SymbolCodeGenModelParserProvider parserProvider)
    {
        ParserProvider = parserProvider;
    }
    
    public abstract TCodeGenModel Parse(TSymbol symbol);
}