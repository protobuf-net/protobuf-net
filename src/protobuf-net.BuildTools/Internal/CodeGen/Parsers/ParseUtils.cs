#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal static partial class ParseUtils
{
    public static bool IsProtoMember(ImmutableArray<AttributeData> attributes,
        /*[NotNullWhen(true)] can't use cleanly because of targets*/ 
        out AttributeData protoMemberAttributeData)
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

        protoMemberAttributeData = null!;
        return false;
    }

    public static (int fieldNumber, string? originalName, DataFormat? dataFormat, bool isRequired) GetProtoMemberAttributeData(AttributeData protoMemberAttribute)
    {
        int fieldNumber = default;
        string? originalName = null;
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

    public static bool IsProtoContract(ImmutableArray<AttributeData> attributes, out AttributeData protoContractAttributeData)
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

        protoContractAttributeData = null!;
        return false;
    }
}