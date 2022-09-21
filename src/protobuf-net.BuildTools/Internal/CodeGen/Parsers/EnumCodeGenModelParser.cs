using System;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Models;
using ProtoBuf.Internal.CodeGen.Parsers.Common;
using ProtoBuf.Internal.CodeGen.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal sealed class EnumCodeGenModelParser : TypeCodeGenModelParserBase<CodeGenEnum>
{
    public override CodeGenEnum Parse(ITypeSymbol symbol, NamespaceParseContext parseContext)
    {
        var codeGenEnum = InitializeCodeGenEnum(symbol, parseContext);
        
        var childSymbols = symbol.GetMembers();
        foreach (var childSymbol in childSymbols)
        {
            if (childSymbol is IFieldSymbol fieldSymbol)
            {
                var enumValueParser = SymbolCodeGenModelParserProvider.GetEnumValueParser();
                var parsedEnumValue = enumValueParser.Parse(fieldSymbol, parseContext);
                codeGenEnum.EnumValues.Add(parsedEnumValue);
            }
        }

        return codeGenEnum;
    }

    private CodeGenEnum InitializeCodeGenEnum(ITypeSymbol symbol, NamespaceParseContext parseContext)
    {
        var symbolAttributes = symbol.GetAttributes();
        if (IsProtoContract(symbolAttributes, out var protoContractAttributeData))
        {
            return ParseEnum(symbol, protoContractAttributeData, parseContext);
        }
        
        return null;
    }
    
    private CodeGenEnum ParseEnum(ITypeSymbol typeSymbol, AttributeData protoContractAttributeData, NamespaceParseContext parseContext)
    {
        var codeGenEnum = new CodeGenEnum(typeSymbol.Name, typeSymbol.GetFullyQualifiedPrefix());
        // add enum inherit type here
        // codeGenEnum.Type = new WellKnown ...
        
        return codeGenEnum;
    }
}