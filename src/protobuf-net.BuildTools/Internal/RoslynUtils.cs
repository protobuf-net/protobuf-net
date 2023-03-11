#nullable enable
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ProtoBuf.Internal
{
    internal static class RoslynUtils
    {
        public static CompilationUnitSyntax AddUsingsIfNotExist(
            this CompilationUnitSyntax compilationUnitSyntax,
            params string[]? usingDirectiveNames)
        {
            if (usingDirectiveNames is null || usingDirectiveNames.Length == 0) return compilationUnitSyntax;

            // build a hashset for efficient lookup
            // comparison is done based on string value, because different usings can have different types of identifiers:
            // - IdentifierName
            // - QualifiedNameSyntax
            var existingUsingDirectiveNames = compilationUnitSyntax.Usings
                .Select(x => x.Name.ToString().Trim())
                .ToImmutableHashSet();

            foreach (var directive in usingDirectiveNames)
            {
                var directiveTrimmed = directive.Trim();
                if (!existingUsingDirectiveNames.Contains(directiveTrimmed))
                {
                    compilationUnitSyntax = compilationUnitSyntax.AddUsings(
                        SyntaxFactory.UsingDirective(
                            SyntaxFactory.ParseName(" " + directiveTrimmed)));
                }
            }

            return compilationUnitSyntax;
        }
        
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
            
            return namedTypeSymbol.SpecialType switch
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
        }

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

        public static object? DynamicallyParseToValue(Type? type, string? value)
        {
            if (type == null)
            {
                return null;
            }
            
            object? sConvertFromInvariantString = null;
            if (TryConvertFromInvariantString(type, value, out var convertedValue))
            {
                return convertedValue;
            }

            if (type.IsSubclassOf(typeof(Enum)) && value != null)
            {
                return Enum.Parse(type, value, true);
            }

            if (type == typeof(TimeSpan) && value != null)
            {
                return TimeSpan.Parse(value);
            }

            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);

            // Looking for ad hoc created TypeDescriptor.ConvertFromInvariantString(Type, string)
            bool TryConvertFromInvariantString(
                Type typeToConvert,
                string? stringValue,
                out object? conversionResult)
            {
                conversionResult = null;

                // lazy init reflection objects
                if (sConvertFromInvariantString == null)
                {
                    var typeDescriptorType = Type.GetType("System.ComponentModel.TypeDescriptor, System.ComponentModel.TypeConverter", throwOnError: false);
                    var mi = typeDescriptorType?.GetMethod("ConvertFromInvariantString", BindingFlags.NonPublic | BindingFlags.Static);
                    Volatile.Write(ref sConvertFromInvariantString, mi == null ? new object() : mi.CreateDelegate(typeof(Func<Type, string, object>)));
                }
                
                if (!(sConvertFromInvariantString is Func<Type, string?, object> convertFromInvariantString))
                    return false;
                
                try
                {
                    conversionResult = convertFromInvariantString(typeToConvert, stringValue);
                }
                catch
                {
                    return false;
                }

                return true;
            }
        }
    }
}