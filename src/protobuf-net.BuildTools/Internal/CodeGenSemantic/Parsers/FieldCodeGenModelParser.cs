using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGenSemantic.Abstractions;
using ProtoBuf.Internal.CodeGenSemantic.Models;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGenSemantic.Parsers;

internal sealed class FieldCodeGenModelParser : ISymbolCodeGenModelParser<IPropertySymbol, CodeGenField>
{
    public CodeGenField Parse(IPropertySymbol symbol, NamespaceParseContext parseContext)
    {
        var propertyAttributes = symbol.GetAttributes();
        if (IsProtoMember(propertyAttributes, out var protoMemberAttribute))
        {
            return ParseMember(symbol, protoMemberAttribute);
        }

        return null;
    }
    
    private static CodeGenField ParseMember(IPropertySymbol propertySymbol, AttributeData protoMemberAttribute)
    {
        var (fieldNumber, originalName) = GetFieldNumberAndOriginalName(protoMemberAttribute);
        var codeGenField = new CodeGenField(fieldNumber, propertySymbol.Name)
        {
            OriginalName = originalName,
            Type = propertySymbol.GetCodeGenType()
        };
        
        return codeGenField;
    }
    
    private static (int fieldNumber, string originalName) GetFieldNumberAndOriginalName(AttributeData protoMemberAttribute)
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
                    // TODO pass property symbol
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
    
    private static bool IsProtoMember(ImmutableArray<AttributeData> attributes, out AttributeData protoMemberAttributeData)
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
}