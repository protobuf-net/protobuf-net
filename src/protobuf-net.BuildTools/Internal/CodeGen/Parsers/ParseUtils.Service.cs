#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Reflection.Internal.CodeGen;
using System.ServiceModel;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal static partial class ParseUtils
{

    public static CodeGenService? ParseService(in CodeGenFileParseContext ctx, ITypeSymbol symbol)
    {
        if (ctx.HasConsidered(symbol)) return null;
        var codeGenService = InitializeCodeGenService(in ctx, symbol);
        if (codeGenService is not null)
        {
            // go through child nodes and attach nested members
            var childSymbols = symbol.GetMembers();
            foreach (var childSymbol in childSymbols)
            {
                if (childSymbol is IMethodSymbol methodSymbol)
                {
                    var codeGenServiceMethod = ParseUtils.ParseOperation(in ctx, methodSymbol);
                    if (codeGenServiceMethod is not null)
                    {
                        codeGenService.ServiceMethods.Add(codeGenServiceMethod);
                    }
                }
            }
        }
        return codeGenService;
    }
    
    private static CodeGenService? InitializeCodeGenService(in CodeGenFileParseContext ctx, ITypeSymbol symbol)
    {
        if (TryGetServiceContractAttributeData(symbol, out var protoContractAttributeData))
        {
            var codeGenService = ParseService(symbol, protoContractAttributeData);
            ctx.Context.Register(symbol.GetFullyQualifiedType(), codeGenService);
            return codeGenService;
        }
        
        return null;
    }
    
    private static CodeGenService ParseService(ITypeSymbol typeSymbol, AttributeData protoContractAttributeData)
    {
        var codeGenMessage = new CodeGenService(typeSymbol.Name, typeSymbol.GetFullyQualifiedPrefix())
        {
            Package = typeSymbol.GetFullyQualifiedPrefix(trimFinal: true),
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

        serviceContractAttribute = null!;
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