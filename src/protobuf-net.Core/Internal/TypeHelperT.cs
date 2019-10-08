using ProtoBuf.Meta;
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
            //if (type == null) return null;
            //if (type.IsEnum) return type.Name;

            //var nullable = Nullable.GetUnderlyingType(type);
            //if (nullable != null) return CSName(nullable) + "?";

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

            if (type == null
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
    }

    internal static class TypeHelper<T>
    {
        public static readonly bool IsReferenceType = !typeof(T).IsValueType;

        public static readonly bool CanBeNull = default(T) == null;

        public static readonly bool CanBePacked = !IsReferenceType && TypeHelper.CanBePacked(typeof(T));

        public static readonly T Default = typeof(T) == typeof(string) ? (T)(object)"" : default;

        // make sure we don't cast null value-types to NREs
        [MethodImpl(ProtoReader.HotPath)]
        public static T FromObject(object value) => value == null ? default : (T)value;

        public static readonly Func<ISerializationContext, T> Factory = ctx => TypeModel.CreateInstance<T>(ctx, null);
    }
}
