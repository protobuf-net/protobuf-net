using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Reflection;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ProtoBuf.Internal.CodeGen
{
    // will be deleted - just to merge a compile-ready PR
    internal static class TemporaryUtils
    {
        internal static CodeGenType GetCodeGenType(this IPropertySymbol symbol, DataFormat? dataFormat)
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
            return new CodeGenCustomType(symbol.Type.Name, symbol.Type?.BaseType?.Name ?? string.Empty);

            CodeGenType Invalid() => throw new InvalidOperationException($"{symbol.Name} with format {dataFormat?.ToString() ?? "(null)"} is not supported");
        }
    }
}
