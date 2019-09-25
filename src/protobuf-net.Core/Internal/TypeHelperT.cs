using ProtoBuf.Meta;
using System;
using System.Text;

namespace ProtoBuf.Internal
{
    internal static class TypeHelper
    {
        public static bool UseFallback(Type type)
        {
            if (type == null) return false;
            if (Nullable.GetUnderlyingType(type) != null) return true;
            if (type.IsArray) return true;
            if (type == typeof(object)) return true;
            if (type.IsEnum) return true;
            if (TypeModel.GetWireType(null, Helpers.GetTypeCode(type), DataFormat.Default, ref type, out int modelKey) != WireType.None && modelKey < 0)
                return true;
            if (TypeModel.GetListItemType(type) != null)
                return true;

            return false;
        }
        internal static string CSName(Type type)
        {
            if (type == null) return null;
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
        public static readonly bool IsObjectType = !typeof(T).IsValueType;
        public static readonly Func<ISerializationContext, T> Factory = ctx => TypeModel.CreateInstance<T>(ctx);

        public static readonly bool IsReferenceOrContainsReferences = GetContainsReferences();

        // these are things that require special primitive handling that is not implemented in the <T> versions
        public static readonly bool UseFallback = TypeHelper.UseFallback(typeof(T));

        // make sure we don't cast null value-types to NREs
        public static T FromObject(object value) => value == null ? default : (T)value;

#if PLAT_ISREF
        private static bool GetContainsReferences()
            => System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>();
#else
        private static bool GetContainsReferences()
        {
            if (typeof(T).IsValueType)
            {
                try
                {
                    if (Activator.CreateInstance(typeof(DirtyTestForReferences<>).MakeGenericType(typeof(T)), nonPublic: true) is object)
                        return false;
                }
                catch { }
            }
            return true;
        }
#endif
    }

#if !PLAT_ISREF
    internal sealed class DirtyTestForReferences<T> where T : unmanaged { }
#endif
}
