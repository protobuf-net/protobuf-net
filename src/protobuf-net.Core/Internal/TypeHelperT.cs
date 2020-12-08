using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Internal
{
    internal static class TypeHelper
    {
        internal static string NormalizeName(this Type type)
        {
            return type?.ToString();
            //if (type is null) return null;
            //if (type.IsEnum) return type.Name;

            //var nullable = Nullable.GetUnderlyingType(type);
            //if (nullable is object) return CSName(nullable) + "?";

            //if (!type.IsGenericType)
            //{
            //    return (Type.GetTypeCode(type)) switch
            //    {
            //        TypeCode.Boolean => "bool",
            //        TypeCode.Char => "char",
            //        TypeCode.SByte => "sbyte",
            //        TypeCode.Byte => "byte",
            //        TypeCode.Int16 => "short",
            //        TypeCode.UInt16 => "ushort",
            //        TypeCode.Int32 => "int",
            //        TypeCode.UInt32 => "uint",
            //        TypeCode.Int64 => "long",
            //        TypeCode.UInt64 => "ulong",
            //        TypeCode.Single => "float",
            //        TypeCode.Double => "double",
            //        TypeCode.Decimal => "decimal",
            //        TypeCode.String => "string",
            //        _ => type.Name,
            //    };
            //}

            //var withTicks = type.Name;
            //var index = withTicks.IndexOf('`');
            //if (index < 0) return type.Name;

            //var sb = new StringBuilder();
            //sb.Append(type.Name.Substring(0, index)).Append('<');
            //var args = type.GetGenericArguments();
            //for (int i = 0; i < args.Length; i++)
            //{
            //    if (i != 0) sb.Append(',');
            //    sb.Append(CSName(args[i]));
            //}
            //return sb.Append('>').ToString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0066:Convert switch statement to expression", Justification = "Readability")]
        internal static bool CanBePacked(Type type)
        {
            if (type.IsEnum) return true;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Boolean:
                case TypeCode.Char:
                    return true;
            }
            return false;
        }

        [Obsolete("Prefer list provider")]
        internal static bool ResolveUniqueEnumerableT(Type type, out Type t)
        {
            static bool IsEnumerableT(Type type, out Type t)
            {
                if (type.IsInterface && type.IsGenericType
                    && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    t = type.GetGenericArguments()[0];
                    return true;
                }
                t = null;
                return false;
            }

            if (type is null
                || type == typeof(string) || type == typeof(byte[]) || type == typeof(object))
            {
                t = null; // don't need that kind of confusion
                return false;
            }

            if (type.IsArray)
            {
                t = type.GetElementType();
                return type == t.MakeArrayType(); // rules out multi-dimensional etc
            }

            bool haveMatch = false;
            t = null;
            try
            {
                if (IsEnumerableT(type, out t))
                    return true;

                foreach (var iType in type.GetInterfaces())
                {
                    if (IsEnumerableT(iType, out var tmp))
                    {
                        if (haveMatch && tmp != t)
                        {
                            haveMatch = false;
                            break;
                        }
                        else
                        {
                            haveMatch = true;
                            t = tmp;
                        }
                    }
                }
            }
            catch { }

            if (haveMatch) return true;

            // if it isn't a good fit; don't use "map"
            t = null;
            return false;
        }

        internal static object GetValueTypeChecker(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            return typeof(StructValueChecker<>).MakeGenericType(underlying)
                .GetField(nameof(StructValueChecker<int>.Instance))
                .GetValue(null);
        }
    }

    internal static class TypeHelper<T>
    {
        public static readonly bool IsReferenceType = !typeof(T).IsValueType;

        public static readonly bool CanBeNull = default(T) is null;

        public static readonly IValueChecker<T> ValueChecker =
            SerializerCache<PrimaryTypeProvider>.InstanceField as IValueChecker<T>
            ?? ReferenceValueChecker.Instance as IValueChecker<T>
            ?? (IValueChecker<T>)TypeHelper.GetValueTypeChecker(typeof(T));

        public static readonly bool CanBePacked = !CanBeNull && TypeHelper.CanBePacked(typeof(T));

        public static readonly T Default = typeof(T) == typeof(string) ? (T)(object)"" : default;

        // make sure we don't cast null value-types to NREs
        [MethodImpl(ProtoReader.HotPath)]
        public static T FromObject(object value) => value is null ? default : (T)value;

        public static readonly Func<ISerializationContext, T> Factory = ctx => TypeModel.CreateInstance<T>(ctx, null);
    }

    internal interface IValueChecker<in T>
    {
        bool HasNonTrivialValue(T value);
        bool IsNull(T value);
    }
    internal sealed class ReferenceValueChecker : IValueChecker<object>
    {
        private ReferenceValueChecker() { }
        public static readonly ReferenceValueChecker Instance = new ReferenceValueChecker();

        /// <summary>
        /// Indicates whether a value is non-null and needs serialization (non-zero, not an empty string, etc)
        /// </summary>
        bool IValueChecker<object>.HasNonTrivialValue(object value) => value is object;
        /// <summary>
        /// Indicates whether a value is null
        /// </summary>
        bool IValueChecker<object>.IsNull(object value) => value is null;
    }
    internal sealed class StructValueChecker<T> : IValueChecker<T?>, IValueChecker<T>
        where T : struct
    {
        private StructValueChecker() { }
        public static readonly StructValueChecker<T> Instance = new StructValueChecker<T>();
        bool IValueChecker<T?>.HasNonTrivialValue(T? value) => value.HasValue;
        bool IValueChecker<T?>.IsNull(T? value) => !value.HasValue;
        bool IValueChecker<T>.HasNonTrivialValue(T value) => true;
        bool IValueChecker<T>.IsNull(T value) => false;
    }
}
