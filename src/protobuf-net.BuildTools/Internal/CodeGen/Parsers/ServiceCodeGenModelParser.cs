using System;
using System.ServiceModel;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Abstractions;
using ProtoBuf.Internal.CodeGen.Models;
using ProtoBuf.Internal.CodeGen.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal sealed class ServiceCodeGenModelParser : SymbolCodeGenModelParserBase<ITypeSymbol, CodeGenService>
{
    private readonly CodeGenNamespaceParseContext _namespaceParseContext;
    
    public ServiceCodeGenModelParser(SymbolCodeGenModelParserProvider parserProvider, CodeGenNamespaceParseContext namespaceParseContext) 
        : base(parserProvider)
    {
        _namespaceParseContext = namespaceParseContext;
    }

    public override CodeGenService Parse(ITypeSymbol symbol)
    {
        var codeGenService = InitializeCodeGenService(symbol);
        
        // go through child nodes and attach nested members
        var childSymbols = symbol.GetMembers();
        foreach (var childSymbol in childSymbols)
        {
            if (childSymbol is IMethodSymbol methodSymbol)
            {
                var serviceMethodParser = ParserProvider.GetServiceMethodParser();
                var codeGenServiceMethod = serviceMethodParser.Parse(methodSymbol);
                codeGenService.ServiceMethods.Add(codeGenServiceMethod);
            }
        }
        
        return codeGenService;
    }
    
    private CodeGenService InitializeCodeGenService(ITypeSymbol symbol)
    {
        if (TryGetServiceContractAttributeData(symbol, out var protoContractAttributeData))
        {
            var codeGenService = ParseService(symbol, protoContractAttributeData);
            ParseContext.Register(symbol.GetFullyQualifiedType(), codeGenService);
            return codeGenService;
        }
        
        return null;
    }
    
    private CodeGenService ParseService(ITypeSymbol typeSymbol, AttributeData protoContractAttributeData)
    {
        var codeGenMessage = new CodeGenService(typeSymbol.Name, typeSymbol.GetFullyQualifiedPrefix())
        {
            Package = _namespaceParseContext.NamespaceName,
            Emit = CodeGenGenerate.ServiceContract | CodeGenGenerate.ServiceProxy, // everything else is in the existing code
        };

        // var protoContractAttributeClass = protoContractAttributeData.AttributeClass!;
        //
        // // we expect to recognize and handle all of these; if not: bomb (or better: report an error cleanly)
        // switch (protoContractAttributeClass.Name)
        // {
        //     case nameof(ProtoContractAttribute):
        //         if (protoContractAttributeData.AttributeConstructor?.Parameters is { Length: > 0 })
        //         {
        //             throw new InvalidOperationException("Unexpected parameters for " + protoContractAttributeClass.Name);
        //         }
        //         foreach (var namedArg in protoContractAttributeData.NamedArguments)
        //         {
        //             switch (namedArg.Key)
        //             {
        //                 case nameof(ProtoContractAttribute.Name) when namedArg.Value.TryGetString(out var s):
        //                     codeGenMessage.OriginalName = s;
        //                     break;
        //                 default:
        //                     throw new InvalidOperationException($"Unexpected named arg: {protoContractAttributeClass.Name}.{namedArg.Key}");
        //             }
        //         }
        //         break;
        // }

        return codeGenMessage;
    }
    
    private static bool TryGetServiceContractAttributeData(ITypeSymbol symbol, out AttributeData serviceContractAttribute)
    {
        var attributes = symbol.GetAttributes();
        foreach (var attribute in attributes)
        {
            var ac = attribute.AttributeClass;
            if (ac?.Name == ServiceContractAttributeName || ac?.Name == ServiceContractAttributeShortenedName)
            {
                serviceContractAttribute = attribute;
                return true;
            }
        }

        serviceContractAttribute = null;
        return false;
    }
    
    static string ServiceContractAttributeName => nameof(ServiceContractAttribute);
    static string ServiceContractAttributeShortenedName
    {
        get
        {
            var attributeName = nameof(ServiceContractAttribute);
            return attributeName.Substring(0, attributeName.Length - "Attribute".Length);
        }
    }
}