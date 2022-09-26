using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Abstractions;
using ProtoBuf.Internal.CodeGen.Providers;

namespace ProtoBuf.Internal.CodeGen.Parsers.Common;

internal abstract class PropertyCodeGenModelParserBase<TCodeGenModel> : SymbolCodeGenModelParserBase<IPropertySymbol, TCodeGenModel>
{
    protected PropertyCodeGenModelParserBase(SymbolCodeGenModelParserProvider parserProvider) : base(parserProvider)
    {
    }
    
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

    protected (int fieldNumber, string originalName, DataFormat? dataFormat, bool isRequired) GetProtoMemberAttributeData(AttributeData protoMemberAttribute)
    {
        int fieldNumber = default;
        string originalName = null;
        DataFormat? dataFormat = null;
        bool isRequired = false;

        var attributeClass = protoMemberAttribute.AttributeClass;
        if (attributeClass is null) return (fieldNumber, originalName, dataFormat, isRequired);
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
                    case nameof(ProtoMemberAttribute.IsRequired) when namedArgument.Value.TryGetBoolean(out var boolValue):
                        isRequired = boolValue;
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected named arg: {attributeClass.Name}.{namedArgument.Key}");
                }
            }
        }

        return (fieldNumber, originalName, dataFormat, isRequired);
    }
}