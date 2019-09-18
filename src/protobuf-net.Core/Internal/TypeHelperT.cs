using ProtoBuf.Meta;
using System;

namespace ProtoBuf.Internal
{
    internal static class TypeHelper<T>
    {
        public static readonly bool IsObjectType = !typeof(T).IsValueType;
        public static readonly Func<ISerializationContext, T> Factory = ctx => TypeModel.CreateInstance<T>(ctx);

        public static bool IsReferenceOrContainsReferences = GetContainsReferences();

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
