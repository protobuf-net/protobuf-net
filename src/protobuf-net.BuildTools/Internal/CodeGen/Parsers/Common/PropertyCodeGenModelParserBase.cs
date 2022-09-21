using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Abstractions;
using ProtoBuf.Internal.CodeGen.Models;

namespace ProtoBuf.Internal.CodeGen.Parsers.Common;

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

    protected (int fieldNumber, string originalName, DataFormat? dataFormat) GetProtoMemberAttributeData(AttributeData protoMemberAttribute)
    {
        int fieldNumber = default;
        string originalName = null;
        DataFormat? dataFormat = null;

        var attributeClass = protoMemberAttribute.AttributeClass;
        if (attributeClass is null) return (fieldNumber, originalName, dataFormat);
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
                    case nameof(ProtoMemberAttribute.DataFormat) when namedArgument.Value.TryGetInt32(out var dataFormat32):
                        dataFormat = (DataFormat)dataFormat32;
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected named arg: {attributeClass.Name}.{namedArgument.Key}");
                }
            }
        }

        return (fieldNumber, originalName, dataFormat);
    }
}