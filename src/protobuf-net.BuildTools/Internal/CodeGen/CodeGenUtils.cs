using System;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen;

internal static class CodeGenUtils
{
    internal static CodeGenType ResolveCodeGenType(this IPropertySymbol symbol, DataFormat? dataFormat, CodeGenParseContext parseContext)
    {
        var simpleCodeGenType = symbol.ResolveKnownCodeGenType(dataFormat);
        return simpleCodeGenType ?? parseContext.GetContractType(symbol.GetFullyQualifiedType());
    }

    internal static CodeGenType ResolveKnownCodeGenType(this IPropertySymbol symbol, DataFormat? dataFormat)
    {
        if (symbol.Type is INamedTypeSymbol named && named.InNamespace("System"))
        {
            return named.ResolveKnownCodeGenType(dataFormat);
        }
        
        return null;
    }
    
    internal static CodeGenType ResolveKnownCodeGenType(this INamedTypeSymbol symbol, DataFormat? dataFormat)
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
            _ => throw new InvalidOperationException($"Unhandled primitive: System.{symbol.Name}")
        };

        CodeGenType Invalid() =>
            throw new InvalidOperationException(
                $"{symbol.Name} with format {dataFormat?.ToString() ?? "(null)"} is not supported");
    }
}