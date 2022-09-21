using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Abstractions;
using ProtoBuf.Internal.CodeGen.Models;

namespace ProtoBuf.Internal.CodeGen.Parsers.Common;

internal abstract class TypeCodeGenModelParserBase<TCodeGenModel> : ISymbolCodeGenModelParser<ITypeSymbol, TCodeGenModel>
{
    public abstract TCodeGenModel Parse(ITypeSymbol symbol, NamespaceParseContext parseContext);
    
    protected bool IsProtoContract(ImmutableArray<AttributeData> attributes, out AttributeData protoContractAttributeData)
    {
        foreach (var attribute in attributes)
        {
            var ac = attribute.AttributeClass;
            if (ac?.Name == nameof(ProtoContractAttribute) && ac.InProtoBufNamespace())
            {
                protoContractAttributeData = attribute;
                return true;
            }
                
        }

        protoContractAttributeData = null;
        return false;
    }
}