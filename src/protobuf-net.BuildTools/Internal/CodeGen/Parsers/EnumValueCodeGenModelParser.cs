using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Abstractions;
using ProtoBuf.Internal.CodeGen.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal sealed class EnumValueCodeGenModelParser : SymbolCodeGenModelParserBase<IFieldSymbol, CodeGenEnumValue>
{
    public EnumValueCodeGenModelParser(SymbolCodeGenModelParserProvider parserProvider) : base(parserProvider)
    {
    }
    
    public override CodeGenEnumValue Parse(IFieldSymbol symbol)
    {
        var propertyAttributes = symbol.GetAttributes();
        if (IsProtoEnum(propertyAttributes, out var protoEnumAttributeData))
        {
            var originalName = GetProtoEnumAttributeNameValue(protoEnumAttributeData);
            var codeGenEnumValue = new CodeGenEnumValue(symbol.GetConstantValue(), symbol.Name)
            {
                OriginalName = originalName
            };
        
            return codeGenEnumValue;   
        }

        // throw exception here ?
        return null;
    }

    private static string GetProtoEnumAttributeNameValue(AttributeData protoEnumAttributeData)
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

        protoEnumAttributeData = null;
        return false;
    }
}