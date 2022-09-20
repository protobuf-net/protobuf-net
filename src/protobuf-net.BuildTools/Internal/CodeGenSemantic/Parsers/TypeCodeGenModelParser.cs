using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGenSemantic.Abstractions;
using ProtoBuf.Internal.CodeGenSemantic.Models;
using ProtoBuf.Internal.CodeGenSemantic.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGenSemantic.Parsers;

internal sealed class TypeCodeGenModelParser : ISymbolCodeGenModelParser<ITypeSymbol, CodeGenType>
{
    public CodeGenType Parse(ITypeSymbol symbol, NamespaceParseContext parseContext)
    {
        // firstly parse the symbol itself.
        // it can be either a 'message' or an 'enum'
        var codeGenType = InitializeCodeGenType(symbol, parseContext);

        void AttachField(CodeGenField codeGenField)
        {
            if (codeGenType is CodeGenMessage codeGenMessage)
            {
                codeGenMessage.Fields.Add(codeGenField);
            }
            
            // throw some kind of exception here
        }

        void AttachNestedCodeGenType(CodeGenType nestedCodeGenType)
        {
            if (codeGenType is CodeGenMessage codeGenMessage)
            {
                if (nestedCodeGenType is CodeGenMessage nestedMessage)
                {
                    codeGenMessage.Messages.Add(nestedMessage);
                    return;
                }
                    
                if (nestedCodeGenType is CodeGenEnum nestedEnum)
                {
                    codeGenMessage.Enums.Add(nestedEnum);
                    return;
                }
                    
                // throw some kind of exception here
            }
        }

        // go through child nodes and attach nested members
        var childSymbols = symbol.GetMembers();
        foreach (var childSymbol in childSymbols)
        {
            if (childSymbol is IPropertySymbol propertySymbol)
            {
                var fieldParser = SymbolCodeGenModelParserProvider.GetFieldParser();
                var parsedField = fieldParser.Parse(propertySymbol, parseContext);
                if (parsedField is null)
                {
                    // throw some kind of exception here
                }
                
                AttachField(parsedField);
            }
            
            if (childSymbol is ITypeSymbol typeSymbol)
            {
                var nestedCodeGenType = Parse(typeSymbol, parseContext);
                if (nestedCodeGenType is null)
                {
                    // throw some kind of exception here
                }

                AttachNestedCodeGenType(nestedCodeGenType);
            }
        }

        return codeGenType;
    }

    private CodeGenType InitializeCodeGenType(ITypeSymbol symbol, NamespaceParseContext parseContext)
    {
        var symbolAttributes = symbol.GetAttributes();
        if (IsProtoContract(symbolAttributes, out var protoContractAttributeData))
        {
            var codeGenMessage = ParseMessage(symbol, protoContractAttributeData, parseContext);
            return codeGenMessage;
        }
        
        return null;
    }

    private static CodeGenMessage ParseMessage(ITypeSymbol typeSymbol, AttributeData protoContractAttributeData, NamespaceParseContext parseContext)
    {
        var codeGenMessage = new CodeGenMessage(typeSymbol.Name, typeSymbol.GetFullyQualifiedPrefix())
        {
            Package = parseContext.NamespaceName
        };

        var protoContractAttributeClass = protoContractAttributeData.AttributeClass!;
        
        // we expect to recognize and handle all of these; if not: bomb (or better: report an error cleanly)
        switch (protoContractAttributeClass.Name)
        {
            case nameof(ProtoContractAttribute):
                if (protoContractAttributeData.AttributeConstructor?.Parameters is { Length: > 0 })
                {
                    throw new InvalidOperationException("Unexpected parameters for " + protoContractAttributeClass.Name);
                }
                foreach (var namedArg in protoContractAttributeData.NamedArguments)
                {
                    switch (namedArg.Key)
                    {
                        case nameof(ProtoContractAttribute.Name) when namedArg.Value.TryGetString(out var s):
                            codeGenMessage.OriginalName = s;
                            break;
                        default:
                            throw new InvalidOperationException($"Unexpected named arg: {protoContractAttributeClass.Name}.{namedArg.Key}");
                    }
                }
                break;
        }
        
            
        return codeGenMessage;
    }

    private static bool IsProtoContract(ImmutableArray<AttributeData> attributes, out AttributeData protoContractAttributeData)
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