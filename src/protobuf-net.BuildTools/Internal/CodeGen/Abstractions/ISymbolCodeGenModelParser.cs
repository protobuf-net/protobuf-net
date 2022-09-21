#nullable enable
using Microsoft.CodeAnalysis;

namespace ProtoBuf.Internal.CodeGen.Abstractions;

internal interface ISymbolCodeGenModelParser<TSymbol, TCodeGenModel> where TSymbol : ISymbol
{
    TCodeGenModel Parse(TSymbol symbol);
}