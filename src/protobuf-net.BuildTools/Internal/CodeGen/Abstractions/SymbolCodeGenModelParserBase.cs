using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGen.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;
using ProtoBuf.Reflection.Internal.CodeGen.Error;

namespace ProtoBuf.Internal.CodeGen.Abstractions;

internal abstract class SymbolCodeGenModelParserBase<TSymbol, TCodeGenModel> 
    : ISymbolCodeGenModelParser<TSymbol, TCodeGenModel> where TSymbol : ISymbol
{
    protected SymbolCodeGenModelParserProvider ParserProvider { get; }

    protected CodeGenParseContext ParseContext => ParserProvider.ParseContext;
    
    protected CodeGenErrorContainer ErrorContainer => ParserProvider.ErrorContainer;
    
    public SymbolCodeGenModelParserBase(SymbolCodeGenModelParserProvider parserProvider)
    {
        ParserProvider = parserProvider;
    }
    
    public abstract TCodeGenModel Parse(TSymbol symbol);
}