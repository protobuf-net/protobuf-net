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
            switch (named.Name)
            {
                case nameof(Int32):
                    return dataFormat switch
                    {
                        DataFormat.TwosComplement or DataFormat.Default or null => CodeGenSimpleType.Int32,
                        DataFormat.FixedSize => CodeGenSimpleType.SFixed32,
                        DataFormat.ZigZag => CodeGenSimpleType.SInt32,
                        _ => Invalid()
                    };
                case nameof(UInt32):
                    return dataFormat switch
                    {
                        DataFormat.TwosComplement or DataFormat.Default or null => CodeGenSimpleType.UInt32,
                        DataFormat.FixedSize => CodeGenSimpleType.Fixed32,
                        _ => Invalid()
                    };
                case nameof(Int64):
                    return dataFormat switch
                    {
                        DataFormat.TwosComplement or DataFormat.Default or null => CodeGenSimpleType.Int64,
                        DataFormat.FixedSize => CodeGenSimpleType.SFixed64,
                        DataFormat.ZigZag => CodeGenSimpleType.SInt64,
                        _ => Invalid()
                    };
                case nameof(UInt64):
                    return dataFormat switch
                    {
                        DataFormat.TwosComplement or DataFormat.Default or null => CodeGenSimpleType.UInt64,
                        DataFormat.FixedSize => CodeGenSimpleType.Fixed64,
                        _ => Invalid()
                    };
                case nameof(Boolean):
                    return dataFormat switch
                    {
                        DataFormat.TwosComplement or DataFormat.Default or null => CodeGenSimpleType.Boolean,
                        _ => Invalid()
                    };
                case nameof(Single):
                    return dataFormat switch
                    {
                        DataFormat.FixedSize or DataFormat.Default or null => CodeGenSimpleType.Float,
                        _ => Invalid()
                    };
                case nameof(Double):
                    return dataFormat switch
                    {
                        DataFormat.FixedSize or DataFormat.Default or null => CodeGenSimpleType.Double,
                        _ => Invalid()
                    };
                case nameof(String):
                    return dataFormat switch
                    {
                        DataFormat.Default or null => CodeGenSimpleType.String,
                        _ => Invalid()
                    };
                case nameof(Object):
                    return CodeGenSimpleType.NetObjectProxy;
                
                default:
                    throw new InvalidOperationException($"Unhandled primitive: System.{named.Name}");
            }
        }
        
        return null;

        CodeGenType Invalid() =>
            throw new InvalidOperationException(
                $"{symbol.Name} with format {dataFormat?.ToString() ?? "(null)"} is not supported");
    }
}