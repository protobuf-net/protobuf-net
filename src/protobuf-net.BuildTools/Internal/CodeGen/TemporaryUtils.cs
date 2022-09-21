using Microsoft.CodeAnalysis;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoBuf.Internal.CodeGen
{
    // will be deleted - just to merge a compile-ready PR
    internal static class TemporaryUtils
    {
        internal static CodeGenType GetCodeGenType(this IPropertySymbol symbol)
        {
            var symbolTypeName = symbol.Type.Name;
            if (Enum.TryParse<CodeGenWellKnownType>(symbolTypeName, ignoreCase: true, out var wellKnownType))
            {
                return new CodeGenSimpleType.WellKnown(wellKnownType, symbolTypeName, "System.");
            }

            return new CodeGenCustomType(symbolTypeName, symbol.Type?.BaseType?.Name ?? string.Empty);
        }
    }
}
