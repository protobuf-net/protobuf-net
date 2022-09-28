#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;
using System.Collections.Immutable;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal static partial class ParseUtils
{    
    public static CodeGenEnumValue? ParseEnumValue(in CodeGenFileParseContext ctx, IFieldSymbol symbol)
    {
        var propertyAttributes = symbol.GetAttributes();
        if (IsProtoEnum(propertyAttributes, out var protoEnumAttributeData))
        {
            var originalName = GetProtoEnumAttributeNameValue(protoEnumAttributeData);
            var codeGenEnumValue = new CodeGenEnumValue(symbol.GetConstantValue(), symbol.Name);
            if (originalName is not null)
            {
                codeGenEnumValue.OriginalName = originalName;
            }
        
            return codeGenEnumValue;   
        }

        ctx.SaveWarning(
            $"Failed to find a '{nameof(ProtoEnumAttribute)}' attribute within enum value definition", 
            symbol);
        return null;
    }

    private static string? GetProtoEnumAttributeNameValue(AttributeData protoEnumAttributeData)
    {
        foreach (var namedArg in protoEnumAttributeData.NamedArguments)
        {
            return namedArg.Key switch
            {
                nameof(ProtoEnumAttribute.Name) when namedArg.Value.TryGetString(out var NamePropertyValue) => NamePropertyValue,
                _ => throw new InvalidOperationException($"Unexpected named arg: {protoEnumAttributeData.AttributeClass?.Name}.{namedArg.Key}")
            };
        }
        
        // throw exception here?
        return null;
    } 
    
    private static bool IsProtoEnum(ImmutableArray<AttributeData> attributes, out AttributeData protoEnumAttributeData)
    {
        foreach (var attribute in attributes)
        {
            var ac = attribute.AttributeClass;
            if (ac?.Name == nameof(ProtoEnumAttribute) && ac.InProtoBufNamespace())
            {
                protoEnumAttributeData = attribute;
                return true;
            }
        }

        protoEnumAttributeData = null!;
        return false;
    }
}