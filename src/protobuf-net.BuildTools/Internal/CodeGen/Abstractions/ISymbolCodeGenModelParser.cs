#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGen.Models;

namespace ProtoBuf.Internal.CodeGen.Abstractions;

internal interface ISymbolCodeGenModelParser<TSymbol, TCodeGenModel> where TSymbol : ISymbol
{
    TCodeGenModel Parse(TSymbol symbol, NamespaceParseContext parseContext);
}