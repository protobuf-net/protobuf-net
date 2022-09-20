using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGenSemantic.Abstractions;
using ProtoBuf.Internal.CodeGenSemantic.Models;

namespace ProtoBuf.Internal.CodeGenSemantic.Parsers.Common;

internal abstract class PropertyCodeGenModelParserBase<TCodeGenModel> : ISymbolCodeGenModelParser<IPropertySymbol, TCodeGenModel>
{
    public abstract TCodeGenModel Parse(IPropertySymbol symbol, NamespaceParseContext parseContext);

    protected bool IsProtoMember(ImmutableArray<AttributeData> attributes, out AttributeData protoMemberAttributeData)
    {
        foreach (var attribute in attributes)
        {
            var ac = attribute.AttributeClass;
            if (ac?.Name == nameof(ProtoMemberAttribute) && ac.InProtoBufNamespace())
            {
                protoMemberAttributeData = attribute;
                return true;
            }
        }

        protoMemberAttributeData = null;
        return false;
    }
    
    protected (int fieldNumber, string originalName) GetProtoMemberAttributeData(AttributeData protoMemberAttribute)
    {
        int fieldNumber = default;
        string originalName = null;
            
        var attributeClass = protoMemberAttribute.AttributeClass;
        if (attributeClass is null) return (fieldNumber, originalName);
        if (attributeClass.InProtoBufNamespace())
        {
            // default constructor parameters
            {
                if (protoMemberAttribute.ConstructorArguments.IsDefaultOrEmpty)
                {
                    throw new InvalidOperationException($"Missing constructor parameters ... TODO pass property symbol");
                }
                    
                var fieldNumberCtorArg = protoMemberAttribute.ConstructorArguments.FirstOrDefault();
                if (!fieldNumberCtorArg.TryGetInt32(out fieldNumber))
                {
                    throw new InvalidOperationException($"Unexpected constructor arg: {attributeClass.Name}.");
                }
            }

            // named constructor parameters
            foreach (var namedArgument in protoMemberAttribute.NamedArguments)
            {
                switch (namedArgument.Key)
                {
                    case nameof(ProtoMemberAttribute.Name) when namedArgument.Value.TryGetString(out var stringValue):
                        originalName = stringValue;
                        break;
                                
                    default:
                        throw new InvalidOperationException($"Unexpected named arg: {attributeClass.Name}.{namedArgument.Key}");
                }
            }
        }

        return (fieldNumber, originalName);
    }
}