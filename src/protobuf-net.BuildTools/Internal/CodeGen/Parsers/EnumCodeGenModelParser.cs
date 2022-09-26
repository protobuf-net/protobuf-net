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
        if (codeGenEnum is null) return null;
        
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
            var codeGenEnum = ParseEnum(symbol, protoContractAttributeData);
            ParseContext.Register(symbol.GetFullyQualifiedType(), codeGenEnum);
            return codeGenEnum;
        }
        
        return ErrorContainer.SaveWarning<CodeGenEnum>(
            $"Failed to find a '{nameof(ProtoContractAttribute)}' attribute within enum type definition", 
            symbol.GetFullTypeName(), 
            symbol.GetLocation());
    }
    
    private CodeGenEnum ParseEnum(ITypeSymbol typeSymbol, AttributeData protoContractAttributeData)
    {
        var codeGenEnum = new CodeGenEnum(typeSymbol.Name, typeSymbol.GetFullyQualifiedPrefix());
        
        // enum can have an underlying type such as
        // 'public enum MyValues : byte'
        if (typeSymbol is INamedTypeSymbol enumNamedSymbol)
        {
            codeGenEnum.Type = enumNamedSymbol.EnumUnderlyingType.TryResolveKnownCodeGenType(DataFormat.Default)
                ?? throw new System.InvalidOperationException("Unable to resolve enum type");
        }
        
        return codeGenEnum;
    }
}