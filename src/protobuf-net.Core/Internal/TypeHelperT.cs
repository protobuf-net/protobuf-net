using ProtoBuf.Meta;
using ProtoBuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ProtoBuf.Internal
{
    internal static class TypeHelper
    {
        internal static string CSName(Type type)
        {
            if (type == null) return null;
            if (type.IsEnum) return type.Name;

            var nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null) return CSName(nullable) + "?";

            if (!type.IsGenericType)
            {
                return (Type.GetTypeCode(type)) switch
                {
                    TypeCode.Boolean => "bool",
                    TypeCode.Char => "char",
                    TypeCode.SByte => "sbyte",
                    TypeCode.Byte => "byte",
                    TypeCode.Int16 => "short",
                    TypeCode.UInt16 => "ushort",
                    TypeCode.Int32 => "int",
                    TypeCode.UInt32 => "uint",
                    TypeCode.Int64 => "long",
                    TypeCode.UInt64 => "ulong",
                    TypeCode.Single => "float",
                    TypeCode.Double => "double",
                    TypeCode.Decimal => "decimal",
                    TypeCode.String => "string",
                    _ => type.Name,
                };
            }

            var withTicks = type.Name;
            var index = withTicks.IndexOf('`');
            if (index < 0) return type.Name;

            var sb = new StringBuilder();
            sb.Append(type.Name.Substring(0, index)).Append('<');
            var args = type.GetGenericArguments();
            for (int i = 0; i < args.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(CSName(args[i]));
            }
            return sb.Append('>').ToString();
        }
    }
    internal static class TypeHelper<T>
    {
        public static bool IsReferenceType = !typeof(T).IsValueType;

        public static readonly bool CanBeNull = default(T) == null;

        public static readonly Func<ISerializationContext, T> Factory = ctx => TypeModel.CreateInstance<T>(ctx);

        // make sure we don't cast null value-types to NREs
        [MethodImpl(ProtoReader.HotPath)]
        public static T FromObject(object value) => value == null ? default : (T)value;
    }
}
