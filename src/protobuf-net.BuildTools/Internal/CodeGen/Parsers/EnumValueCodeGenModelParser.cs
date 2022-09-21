using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGen.Abstractions;
using ProtoBuf.Internal.CodeGen.Models;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal sealed class EnumValueCodeGenModelParser : ISymbolCodeGenModelParser<IFieldSymbol, CodeGenEnumValue>
{
    public CodeGenEnumValue Parse(IFieldSymbol symbol, NamespaceParseContext parseContext)
    {
        // finish the implementation here
        // we need to parse construction below to `CodeGenEnumValue`
        //
        // global::ProtoBuf.ProtoEnum(Name = @"CORPUS_UNSPECIFIED")]
        // CorpusUnspecified = 0,

        return null;
    }
}