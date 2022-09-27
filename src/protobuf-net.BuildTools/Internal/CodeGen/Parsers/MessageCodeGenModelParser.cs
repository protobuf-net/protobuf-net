using System;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Models;
using ProtoBuf.Internal.CodeGen.Parsers.Common;
using ProtoBuf.Internal.CodeGen.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal sealed class MessageCodeGenModelParser : TypeCodeGenModelParserBase<CodeGenMessage>
{
    private readonly CodeGenNamespaceParseContext _namespaceParseContext;
    
    public MessageCodeGenModelParser(SymbolCodeGenModelParserProvider parserProvider, CodeGenNamespaceParseContext namespaceParseContext) 
        : base(parserProvider)
    {
        _namespaceParseContext = namespaceParseContext;
    }
    
    public override CodeGenMessage Parse(ITypeSymbol symbol)
    {
        var codeGenMessage = InitializeCodeGenMessage(symbol);
        
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
                        var fieldParser = ParserProvider.GetFieldParser();
                        var codeGenField = fieldParser.Parse(propertySymbol);
                        codeGenMessage.Fields.Add(codeGenField);
                        break;
                    
                    case TypeKind.Enum:
                        var enumPropertyParser = ParserProvider.GetEnumPropertyParser();
                        var codeGenEnum = enumPropertyParser.Parse(propertySymbol);
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
                        var messageParser = ParserProvider.GetMessageParser(_namespaceParseContext);
                        var nestedMessage = messageParser.Parse(typeSymbol);
                        codeGenMessage.Messages.Add(nestedMessage);
                        break;
                    
                    case TypeKind.Enum:
                        var enumParser = ParserProvider.GetEnumParser();
                        var nestedEnum = enumParser.Parse(typeSymbol);
                        codeGenMessage.Enums.Add(nestedEnum);
                        break;
                }
            }
        }

        return codeGenMessage;
    }

    private CodeGenMessage InitializeCodeGenMessage(ITypeSymbol symbol)
    {
        var symbolAttributes = symbol.GetAttributes();
        if (IsProtoContract(symbolAttributes, out var protoContractAttributeData))
        {
            var codeGenMessage = ParseMessage(symbol, protoContractAttributeData);
            ParseContext.Register(symbol.GetFullyQualifiedType(), codeGenMessage);
            return codeGenMessage;
        }
        
        return null;
    }

    private CodeGenMessage ParseMessage(ITypeSymbol typeSymbol, AttributeData protoContractAttributeData)
    {
        var codeGenMessage = new CodeGenMessage(typeSymbol.Name, typeSymbol.GetFullyQualifiedPrefix())
        {
            Package = _namespaceParseContext.NamespaceName,
            Emit = CodeGenGenerate.DataContract | CodeGenGenerate.DataSerializer, // everything else is in the existing code
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