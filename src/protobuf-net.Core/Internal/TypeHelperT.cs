using ProtoBuf.Meta;
using System;
using System.Text;

namespace ProtoBuf.Internal
{
    internal static class TypeHelper
    {
        public static bool IsLegacyType(Type type)
        {
            if (type == null) return false;
            if (type == typeof(object)) return true;
            if (type == typeof(byte[])) return true;
            if (type.IsEnum) return true;
            if (TypeModel.GetWireType(null, Helpers.GetTypeCode(type), DataFormat.Default, ref type, out int modelKey) != WireType.None && modelKey < 0)
                return true;
            return false;
        }
        internal static string CSName(Type type)
        {
            if (type == null) return null;
            if (!type.IsGenericType)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean: return "bool";
                    case TypeCode.Char: return "char";
                    case TypeCode.SByte: return "sbyte";
                    case TypeCode.Byte: return "byte";
                    case TypeCode.Int16: return "short";
                    case TypeCode.UInt16: return "ushort";
                    case TypeCode.Int32: return "int";
                    case TypeCode.UInt32: return "uint";
                    case TypeCode.Int64: return "long";
                    case TypeCode.UInt64: return "ulong";
                    case TypeCode.Single: return "float";
                    case TypeCode.Double: return "double";
                    case TypeCode.Decimal: return "decimal";
                    case TypeCode.String: return "string";
                }
                return type.Name;
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

        // "legacy types" are things that require special primitive handling that is not implemented in the <T> versions
        public static readonly bool IsLegacyType = TypeHelper.IsLegacyType(typeof(T));

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
