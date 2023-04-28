#nullable enable
using System;
using Microsoft.CodeAnalysis;

namespace ProtoBuf.Internal.Roslyn.Extensions
{
    internal static class TypedConstantExtensions
    {
        public static Type? GetUnderlyingType(this TypedConstant typedConstant)
        {
            var namedTypeSymbol = (typedConstant.Value as INamedTypeSymbol);
            if (namedTypeSymbol is null) return null;

            if (namedTypeSymbol is { SpecialType: SpecialType.None, EnumUnderlyingType: { } })
            {
                var type = namedTypeSymbol.ToString();
                try
                {
                    return Type.GetType(type);
                }
                catch
                {
                    return null;
                }
            }

            return namedTypeSymbol.SpecialType.ToType();
        }
    }
}