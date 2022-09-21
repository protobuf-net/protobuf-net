using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Parsers.Common;
using ProtoBuf.Internal.CodeGen.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal sealed class EnumCodeGenModelParser : TypeCodeGenModelParserBase<CodeGenEnum>
{
    public EnumCodeGenModelParser(SymbolCodeGenModelParserProvider parserProvider) : base(parserProvider)
    {
    }
    
    public override CodeGenEnum Parse(ITypeSymbol symbol)
    {
        var codeGenEnum = InitializeCodeGenEnum(symbol);
        
        var childSymbols = symbol.GetMembers();
        foreach (var childSymbol in childSymbols)
        {
            if (childSymbol is IFieldSymbol fieldSymbol)
            {
                var enumValueParser = ParserProvider.GetEnumValueParser();
                var parsedEnumValue = enumValueParser.Parse(fieldSymbol);
                codeGenEnum.EnumValues.Add(parsedEnumValue);
            }
        }

        return codeGenEnum;
    }

    private CodeGenEnum InitializeCodeGenEnum(ITypeSymbol symbol)
    {
        var symbolAttributes = symbol.GetAttributes();
        if (IsProtoContract(symbolAttributes, out var protoContractAttributeData))
        {
            return ParseEnum(symbol, protoContractAttributeData);
        }
        
        return null;
    }
    
    private CodeGenEnum ParseEnum(ITypeSymbol typeSymbol, AttributeData protoContractAttributeData)
    {
        var codeGenEnum = new CodeGenEnum(typeSymbol.Name, typeSymbol.GetFullyQualifiedPrefix());
        // add enum inherit type here
        // codeGenEnum.Type = new WellKnown ...
        
        return codeGenEnum;
    }
}