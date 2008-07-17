
namespace ProtoBuf
{
    internal static class SerializerCache<TValue>
    {
        static SerializerCache()
        {
            SimpleSerializers.Init();
        }
        public static ISerializer<TValue> Signed
        {
            get;
            private set;
        }
        public static ISerializer<TValue> Unsigned
        {
            get;
            private set;
        }
        internal static void Set(ISerializer<TValue> signed, ISerializer<TValue> unsigned)
        {
            Signed = signed;
            Unsigned = unsigned;
        }
    }
}
