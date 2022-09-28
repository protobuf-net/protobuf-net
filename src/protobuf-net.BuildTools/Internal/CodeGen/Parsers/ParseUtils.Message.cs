#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal static partial class ParseUtils
{
    public static CodeGenMessage? ParseMessage(in CodeGenFileParseContext ctx, ITypeSymbol symbol)
    {
        var codeGenMessage = InitializeCodeGenMessage(in ctx, symbol);
        if (codeGenMessage is null) return null;

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
                        var codeGenField = ParseUtils.ParseField(in ctx, propertySymbol);
                        if (codeGenField is not null)
                        {
                            codeGenMessage.Fields.Add(codeGenField);
                        }
                        break;
                    
                    case TypeKind.Enum:
                        var codeGenEnum = ParseEnumProperty(in ctx, propertySymbol);
                        if (codeGenEnum is not null)
                        {
                            codeGenMessage.Enums.Add(codeGenEnum);
                        }
                        break;
                }
            }
            
            if (childSymbol is ITypeSymbol typeSymbol)
            {
                switch (typeSymbol.TypeKind)
                {
                    case TypeKind.Struct:
                    case TypeKind.Class:
                        var nestedMessage = ParseUtils.ParseMessage(in ctx, typeSymbol);
                        if (nestedMessage is not null)
                        {
                            codeGenMessage.Messages.Add(nestedMessage);
                        }
                        break;
                    
                    case TypeKind.Enum:
                        var nestedEnum = ParseUtils.ParseEnum(in ctx, typeSymbol);
                        if (nestedEnum is not null)
                        {
                            codeGenMessage.Enums.Add(nestedEnum);
                        }
                        break;
                }
            }
        }

        return codeGenMessage;
    }

    private static CodeGenMessage? InitializeCodeGenMessage(in CodeGenFileParseContext ctx, ITypeSymbol symbol)
    {
        if (ctx.HasConsidered(symbol)) return null;
        var symbolAttributes = symbol.GetAttributes();
        if (ParseUtils.IsProtoContract(symbolAttributes, out var protoContractAttributeData))
        {
            var codeGenMessage = ParseMessage(symbol, protoContractAttributeData);
            ctx.Context.Register(symbol.GetFullyQualifiedType(), codeGenMessage);
            return codeGenMessage;
        }
        
        return null;
    }



    static bool IsObsolete(ITypeSymbol symbol)
    {
        foreach(var attrib in symbol.GetAttributes())
        {
            var ac = attrib.AttributeClass;
            if (ac?.Name == nameof(ObsoleteAttribute) && ac.InNamespace("System"))
                return true;
        }
        return false;
    }
    private static CodeGenMessage ParseMessage(ITypeSymbol typeSymbol, AttributeData protoContractAttributeData)
    {
        var codeGenMessage = new CodeGenMessage(typeSymbol.Name, typeSymbol.GetFullyQualifiedPrefix())
        {
            Package = typeSymbol.GetFullyQualifiedPrefix(trimFinal: true),
            IsValueType = typeSymbol.IsValueType,
            IsReadOnly = typeSymbol.IsReadOnly,
            IsDeprecated = IsObsolete(typeSymbol),
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