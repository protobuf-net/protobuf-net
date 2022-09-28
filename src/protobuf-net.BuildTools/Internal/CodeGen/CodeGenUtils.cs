#nullable enable
using System;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen;

internal static class CodeGenUtils
{
    internal static CodeGenType ResolveCodeGenType(this ITypeSymbol symbol, DataFormat? dataFormat, CodeGenParseContext parseContext, out RepeatedKind repeated)
    {
        repeated = RepeatedKind.Single;
        
        var simpleCodeGenType = TryResolveKnownCodeGenType(symbol, dataFormat);
        if (simpleCodeGenType is not null) return simpleCodeGenType;

        if (symbol is IArrayTypeSymbol arr && arr.Rank == 1)
        {
            var inner = ResolveCodeGenType(arr.ElementType, dataFormat, parseContext, out var innerRepeated);
            if (inner is not null && innerRepeated == RepeatedKind.Single)
            {
                repeated = RepeatedKind.Array;
                return inner;
            }
        }
        if (symbol.InGenericCollectionsNamespace() && symbol is INamedTypeSymbol named)
        {
            switch (symbol.Name)
            {
                case "List" when named.TypeArguments.Length == 1:
                    var inner = ResolveCodeGenType(named.TypeArguments[0], dataFormat, parseContext, out var innerRepeated);
                    if (inner is not null && innerRepeated == RepeatedKind.Single)
                    {
                        repeated = RepeatedKind.List;
                        return inner;
                    }
                    break;
                case "Dictionary" when named.TypeArguments.Length == 2:
                    var key = ResolveCodeGenType(named.TypeArguments[0], dataFormat, parseContext, out var keyRepeated);
                    var value = ResolveCodeGenType(named.TypeArguments[1], dataFormat, parseContext, out var valueRepeated);
                    if (key is not null && value is not null && keyRepeated == RepeatedKind.Single && valueRepeated == RepeatedKind.Single)
                    {
                        var opaqueKey = named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        repeated = RepeatedKind.Dictionary;
                        return parseContext.GetMapEntryType(opaqueKey, key, value);
                    }
                    break;
            }
        }

        return parseContext.GetContractType(symbol.GetFullyQualifiedType());
    }

    internal static CodeGenType? TryResolveKnownCodeGenType(this ITypeSymbol? symbol, DataFormat? dataFormat)
    {
        if (symbol is null) return null;
        if (symbol is IArrayTypeSymbol arr)
        {
            if (arr.Rank == 1 && arr.ElementType.InNamespace("System") && arr.ElementType.Name == nameof(Byte))
            {
                return dataFormat switch
                {
                    DataFormat.Default or null => CodeGenSimpleType.Bytes,
                    _ => Invalid(),
                };
            }
        }
        else if (symbol.InNamespace("System"))
        {
            return symbol.Name switch
            {
                nameof(Int32) => dataFormat switch
                {
                    DataFormat.TwosComplement or DataFormat.Default or null => CodeGenSimpleType.Int32,
                    DataFormat.FixedSize => CodeGenSimpleType.SFixed32,
                    DataFormat.ZigZag => CodeGenSimpleType.SInt32,
                    _ => Invalid()
                },
                nameof(UInt32) => dataFormat switch
                {
                    DataFormat.TwosComplement or DataFormat.Default or null => CodeGenSimpleType.UInt32,
                    DataFormat.FixedSize => CodeGenSimpleType.Fixed32,
                    _ => Invalid()
                },
                nameof(Int64) => dataFormat switch
                {
                    DataFormat.TwosComplement or DataFormat.Default or null => CodeGenSimpleType.Int64,
                    DataFormat.FixedSize => CodeGenSimpleType.SFixed64,
                    DataFormat.ZigZag => CodeGenSimpleType.SInt64,
                    _ => Invalid()
                },
                nameof(UInt64) => dataFormat switch
                {
                    DataFormat.TwosComplement or DataFormat.Default or null => CodeGenSimpleType.UInt64,
                    DataFormat.FixedSize => CodeGenSimpleType.Fixed64,
                    _ => Invalid()
                },
                nameof(Byte) => dataFormat switch
                {
                    DataFormat.Default or null => CodeGenSimpleType.Byte,
                    _ => Invalid()
                },
                nameof(Boolean) => dataFormat switch
                {
                    DataFormat.TwosComplement or DataFormat.Default or null => CodeGenSimpleType.Boolean,
                    _ => Invalid()
                },
                nameof(Single) => dataFormat switch
                {
                    DataFormat.FixedSize or DataFormat.Default or null => CodeGenSimpleType.Float,
                    _ => Invalid()
                },
                nameof(Double) => dataFormat switch
                {
                    DataFormat.FixedSize or DataFormat.Default or null => CodeGenSimpleType.Double,
                    _ => Invalid()
                },
                nameof(String) => dataFormat switch
                {
                    DataFormat.Default or null => CodeGenSimpleType.String,
                    _ => Invalid()
                },
                nameof(Object) => CodeGenSimpleType.NetObjectProxy,
                _ => throw new InvalidOperationException($"Unhandled inbuilt type: {symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}"),
            };
        }
        return null;
        CodeGenType Invalid() => throw new InvalidOperationException($"{symbol.Name} with format {dataFormat?.ToString() ?? "(null)"} is not supported");
    }
}