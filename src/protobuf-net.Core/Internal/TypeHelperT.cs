using ProtoBuf.Meta;
using System;

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
