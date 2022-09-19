using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal
{
    internal static partial class CodeGenSemanticModelParser
    {
        private static object ParseProperty(CodeGenMessage message, ISymbol symbol, IPropertySymbol propertySymbol)
        {
            if (message is null)
            {
                throw new InvalidOperationException($"No parent type found for {propertySymbol.Name}");
            }
            
            var propertyAttributes = propertySymbol.GetAttributes();
            if (IsProtoMember(propertyAttributes, out var protoMemberAttribute))
            {
                return ParseMember(message, propertySymbol, protoMemberAttribute);
            }

            return null;
        }
        
        private static CodeGenField ParseMember(CodeGenMessage message, IPropertySymbol propertySymbol, AttributeData protoMemberAttribute)
        {
            var (fieldNumber, originalName) = GetFieldNumberAndOriginalName(protoMemberAttribute);
            var codeGenField = new CodeGenField(fieldNumber, propertySymbol.Name)
            {
                OriginalName = originalName,
                Type = propertySymbol.GetCodeGenType()
            };

            message.Fields.Add(codeGenField);
            return codeGenField;
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
    }
}
