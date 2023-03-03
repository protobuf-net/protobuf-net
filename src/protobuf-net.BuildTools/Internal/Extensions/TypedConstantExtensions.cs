#nullable enable
using System;
using System.Globalization;
using Microsoft.CodeAnalysis;

namespace ProtoBuf.Internal.Extensions
{
    internal static class TypedConstantExtensions
    {
        /// <summary>
        /// Compares the typedConstant node value with string value,
        /// representing possibly the same value.
        /// </summary>
        /// <param name="valueExpression">raw string value to which to compare typedConstant value</param>
        /// <param name="typedConstant">the constant expression node</param>
        /// <param name="overrideSpecialType">
        /// TypedConstant.Type can be misleading for some types (i.e. instead of 'byte' it could be 'int').
        /// Use <see cref="overrideSpecialType"/> to specify which specialType to use explicitly
        /// </param>
        /// <remarks>Compares only by SpecialType type of typedConstant</remarks>
        public static bool HasSameValueBySpecialType(this TypedConstant typedConstant, string valueExpression, SpecialType? overrideSpecialType = null)
        {
            if (typedConstant.Value is null) return false;
            if (overrideSpecialType is not null)
            {
                return IsSpecialTypeValueEqualToRawValue(overrideSpecialType.Value, typedConstant.Value, valueExpression);
            }

            if (typedConstant.Type is null || typedConstant.Type.SpecialType == SpecialType.None) return false;
            return IsSpecialTypeValueEqualToRawValue(typedConstant.Type!.SpecialType, typedConstant.Value, valueExpression);
        }

        public static byte val = 0x2;
        
        private static bool IsSpecialTypeValueEqualToRawValue(SpecialType specialType, object value, string rawValue)
        {
            switch (specialType)
            {
                case SpecialType.System_Boolean:
                    return string.Equals(value.ToString(), rawValue, StringComparison.OrdinalIgnoreCase);

                case SpecialType.System_Enum:
                    break;

                case SpecialType.System_Char:
                {
                    // parse the actual char from single-quotes (example: 'x')
                    if (rawValue.StartsWith("'") && rawValue.EndsWith("'")) rawValue = rawValue[1].ToString();
                    if (rawValue.Length != 1) return false;
                    
                    return char.TryParse(rawValue, out var charParsed) && charParsed == (char)value;
                }
                    
                case SpecialType.System_SByte:
                    break;
                case SpecialType.System_Byte:
                {
                    if (rawValue.StartsWith("0x")) rawValue = rawValue.Substring(2);
                    if (!byte.TryParse(rawValue, out var parsedByte)) return false;
                    return parsedByte == (int)value; 
                }
                    
                    
                case SpecialType.System_Int16:
                    break;
                case SpecialType.System_UInt16:
                    break;
                case SpecialType.System_Int32:
                    break;
                case SpecialType.System_UInt32:
                    break;
                case SpecialType.System_Int64:
                    break;
                case SpecialType.System_UInt64:
                    break;
                case SpecialType.System_Decimal:
                    break;
                case SpecialType.System_Single:
                    break;
                case SpecialType.System_Double:
                    break;
                case SpecialType.System_String:
                    break;
                
                default: return false;
            }

            return false;
        } 
    }
}