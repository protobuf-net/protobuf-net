#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGenSemantic.Models;

namespace ProtoBuf.Internal.CodeGenSemantic.Abstractions;

internal interface ISymbolCodeGenModelParser<TSymbol, TCodeGenModel> where TSymbol : ISymbol
{
    TCodeGenModel Parse(TSymbol symbol, NamespaceParseContext parseContext);
}