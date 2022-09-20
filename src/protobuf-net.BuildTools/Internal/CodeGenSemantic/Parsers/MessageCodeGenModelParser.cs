using System;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGenSemantic.Models;
using ProtoBuf.Internal.CodeGenSemantic.Parsers.Common;
using ProtoBuf.Internal.CodeGenSemantic.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGenSemantic.Parsers;

internal sealed class MessageCodeGenModelParser : TypeCodeGenModelParserBase<CodeGenMessage>
{
    public override CodeGenMessage Parse(ITypeSymbol symbol, NamespaceParseContext parseContext)
    {
        var codeGenMessage = InitializeCodeGenMessage(symbol, parseContext);
        
        // go through child nodes and attach nested members
        var childSymbols = symbol.GetMembers();
        foreach (var childSymbol in childSymbols)
        {
            if (childSymbol is IPropertySymbol propertySymbol)
            {
                switch (propertySymbol.Type.TypeKind)
                {
                    case TypeKind.Struct:
                    case TypeKind.Class:
                        var fieldParser = SymbolCodeGenModelParserProvider.GetFieldParser();
                        var codeGenField = fieldParser.Parse(propertySymbol, parseContext);
                        codeGenMessage.Fields.Add(codeGenField);
                        break;
                    
                    case TypeKind.Enum:
                        var enumPropertyParser = SymbolCodeGenModelParserProvider.GetEnumPropertyParser();
                        var codeGenEnum = enumPropertyParser.Parse(propertySymbol, parseContext);
                        codeGenMessage.Enums.Add(codeGenEnum);
                        break;
                }
            }
            
            if (childSymbol is ITypeSymbol typeSymbol)
            {
                switch (typeSymbol.TypeKind)
                {
                    case TypeKind.Struct:
                    case TypeKind.Class:
                        var messageParser = SymbolCodeGenModelParserProvider.GetMessageParser();
                        var nestedMessage = messageParser.Parse(typeSymbol, parseContext);
                        codeGenMessage.Messages.Add(nestedMessage);
                        break;
                    
                    case TypeKind.Enum:
                        var enumParser = SymbolCodeGenModelParserProvider.GetEnumParser();
                        var nestedEnum = enumParser.Parse(typeSymbol, parseContext);
                        codeGenMessage.Enums.Add(nestedEnum);
                        break;
                }
            }
        }

        return codeGenMessage;
    }

    private CodeGenMessage InitializeCodeGenMessage(ITypeSymbol symbol, NamespaceParseContext parseContext)
    {
        var symbolAttributes = symbol.GetAttributes();
        if (IsProtoContract(symbolAttributes, out var protoContractAttributeData))
        {
            return ParseMessage(symbol, protoContractAttributeData, parseContext);
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
}