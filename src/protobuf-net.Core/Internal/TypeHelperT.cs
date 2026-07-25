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
            return type?.ToString() ?? "(null)";
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
            type = Nullable.GetUnderlyingType(type) ?? type;
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
        internal static bool IsBytesLike(Type type)
        {
            if (type == typeof(byte[])) return true;
            if (type == typeof(Memory<byte>)) return true;
            if (type == typeof(ReadOnlyMemory<byte>)) return true;
            if (type == typeof(ArraySegment<byte>)) return true;
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
                || type == typeof(string) || IsBytesLike(type) || type == typeof(object))
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

        internal static object CreateNonTrivialDefault(Type type)
        {
            if (type.IsValueType) return Activator.CreateInstance(Nullable.GetUnderlyingType(type) ?? type);
            if (type == typeof(string)) return "";
            if (type == typeof(byte[])) return Array.Empty<byte>();
            return null;
        }
    }

    internal static class TypeHelper<T>
    {
        public static readonly bool IsReferenceType = !typeof(T).IsValueType;

        public static readonly bool CanBeNull = default(T) is null;

        public static readonly IValueChecker<T> ValueChecker =
            SerializerCache<PrimaryTypeProvider>.InstanceField as IValueChecker<T>
            ?? ReferenceValueChecker.Instance as IValueChecker<T>
            // Anything reaching here is a value type: a reference type was taken by the checker
            // above, since IValueChecker<in T> is contravariant. So this is either a plain struct -
            // always present, never null, both answers constant - or a Nullable<T>, where both
            // answers are just HasValue.
            //
            // Both are named *statically*, which is what keeps them alive under AOT. This used to go
            // through MakeGenericType, which ILC cannot see: StructValueChecker<TStruct> was never
            // generated, and the first serialize of an affected member threw "missing native code".
            ?? (CanBeNull ? NullableValueChecker<T>.Instance : NonNullValueChecker<T>.Instance);

        public static readonly bool CanBePacked = !IsReferenceType && TypeHelper.CanBePacked(typeof(T));

        public static readonly T Default = typeof(T) == typeof(string) ? (T)(object)"" : default;

        public static readonly T NonTrivialDefault = Default ?? (T)TypeHelper.CreateNonTrivialDefault(typeof(T));

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
        bool IValueChecker<object>.HasNonTrivialValue(object value) => value is not null;
        /// <summary>
        /// Indicates whether a value is null
        /// </summary>
        bool IValueChecker<object>.IsNull(object value) => value is null;
    }
    /// <summary>
    /// The checker for a non-nullable value type, where both answers are constants.
    /// </summary>
    /// <remarks>
    /// Deliberately unconstrained, so <see cref="TypeHelper{T}"/> can name it without knowing that
    /// T is a struct — which is what makes it statically reachable, and so AOT-safe.
    /// </remarks>
    internal sealed class NonNullValueChecker<T> : IValueChecker<T>
    {
        private NonNullValueChecker() { }
        public static readonly NonNullValueChecker<T> Instance = new NonNullValueChecker<T>();
        bool IValueChecker<T>.HasNonTrivialValue(T value) => true;
        bool IValueChecker<T>.IsNull(T value) => false;
    }

    /// <summary>
    /// The checker for a <see cref="Nullable{T}"/>, where both answers are just <c>HasValue</c>.
    /// </summary>
    /// <remarks>
    /// Unconstrained for the same reason <see cref="NonNullValueChecker{T}"/> is: so that
    /// <see cref="TypeHelper{T}"/> can name it without knowing the underlying type, which is what
    /// makes it statically reachable and so AOT-safe. The <c>is null</c> test looks like it would
    /// box, but a box of a nullable followed by a null comparison is a JIT peephole that reads
    /// <c>HasValue</c> directly — and this generic is specialised per value type, so it is seen.
    /// </remarks>
    internal sealed class NullableValueChecker<T> : IValueChecker<T>
    {
        private NullableValueChecker() { }
        public static readonly NullableValueChecker<T> Instance = new NullableValueChecker<T>();
        bool IValueChecker<T>.HasNonTrivialValue(T value) => value is not null;
        bool IValueChecker<T>.IsNull(T value) => value is null;
    }
}
