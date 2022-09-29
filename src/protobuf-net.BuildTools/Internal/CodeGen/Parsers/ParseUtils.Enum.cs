#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal static partial class ParseUtils
{    
    public static CodeGenEnum? ParseEnum(in CodeGenFileParseContext ctx, ITypeSymbol symbol)
    {
        var codeGenEnum = InitializeCodeGenEnum(in ctx, symbol);
        if (codeGenEnum is null) return null;
        
        var childSymbols = symbol.GetMembers();
        foreach (var childSymbol in childSymbols)
        {
            if (childSymbol is IFieldSymbol fieldSymbol)
            {
                var parsedEnumValue = ParseUtils.ParseEnumValue(in ctx, fieldSymbol);
                if (parsedEnumValue is not null)
                {
                    codeGenEnum.EnumValues.Add(parsedEnumValue);
                }
            }
        }

        return codeGenEnum;
    }

    private static CodeGenEnum? InitializeCodeGenEnum(in CodeGenFileParseContext ctx, ITypeSymbol symbol)
    {
        if (ctx.HasConsidered(symbol)) return null;
        var symbolAttributes = symbol.GetAttributes();
        if (ParseUtils.IsProtoContract(symbolAttributes, out var protoContractAttributeData))
        {
            var codeGenEnum = ParseEnum(symbol, protoContractAttributeData);
            ctx.Context.Register(symbol.GetFullyQualifiedType(), codeGenEnum);
            return codeGenEnum;
        }

        ctx.ReportDiagnostic(EnumTypeLacksAttribute, symbol);
        return null;
    }
    
    private static CodeGenEnum ParseEnum(ITypeSymbol typeSymbol, AttributeData protoContractAttributeData)
    {
        var codeGenEnum = new CodeGenEnum(typeSymbol.Name, typeSymbol.GetFullyQualifiedPrefix(), typeSymbol)
        {
            Emit = CodeGenGenerate.None, // nothing to emit
        };
        
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