#nullable enable
using System;
using Microsoft.CodeAnalysis;

namespace ProtoBuf.Internal.Roslyn.Extensions
{
    internal static class SpecialTypeExtensions
    {
        public static Type? ToType(this SpecialType type) => type switch
        {
            SpecialType.System_Object => typeof(object),
            SpecialType.System_Boolean => typeof(bool),
            SpecialType.System_Char => typeof(char),
            SpecialType.System_SByte => typeof(sbyte),
            SpecialType.System_Byte => typeof(byte),
            SpecialType.System_Int16 => typeof(short),
            SpecialType.System_UInt16 => typeof(ushort),
            SpecialType.System_Int32 => typeof(int),
            SpecialType.System_UInt32 => typeof(uint),
            SpecialType.System_Int64 => typeof(long),
            SpecialType.System_UInt64 => typeof(ulong),
            SpecialType.System_Decimal => typeof(decimal),
            SpecialType.System_Single => typeof(float),
            SpecialType.System_Double => typeof(double),
            SpecialType.System_String => typeof(string),
            SpecialType.System_IntPtr => typeof(IntPtr),
            SpecialType.System_UIntPtr => typeof(UIntPtr),
            _ => null
        };

        public static string GetSpecialTypeCSharpKeyword(this SpecialType type) => type switch
        {
            SpecialType.System_Object => "object",
            SpecialType.System_Boolean => "bool",
            SpecialType.System_Char => "char",
            SpecialType.System_SByte => "sbyte",
            SpecialType.System_Byte => "byte",
            SpecialType.System_Int16 => "short",
            SpecialType.System_UInt16 => "ushort",
            SpecialType.System_Int32 => "int",
            SpecialType.System_UInt32 => "uint",
            SpecialType.System_Int64 => "long",
            SpecialType.System_UInt64 => "ulong",
            SpecialType.System_Decimal => "decimal",
            SpecialType.System_Single => "float",
            SpecialType.System_Double => "double",
            SpecialType.System_String => "string",
            SpecialType.System_IntPtr => "nint",
            SpecialType.System_UIntPtr => "nuint",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        /// <summary>
        /// Returns true if type is 'int' \ 'double' \ 'bool' \ 'string' or other subtypes
        /// </summary>
        public static bool IsPrimitiveValueType(this SpecialType type) => type switch
        {
            SpecialType.System_Boolean or SpecialType.System_Char or
            SpecialType.System_SByte or SpecialType.System_Byte or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or
            SpecialType.System_Int64 or SpecialType.System_UInt64 or
            SpecialType.System_Decimal or SpecialType.System_Single or
            SpecialType.System_Double or SpecialType.System_String
            => true,
            
            _ => false
        };
    }
}