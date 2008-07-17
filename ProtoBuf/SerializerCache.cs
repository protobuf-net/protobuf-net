
using System;
namespace ProtoBuf
{
    internal static class SerializerCache<TValue>
    {
        static SerializerCache()
        {
            SimpleSerializers.Init();
        }
        public static ISerializer<TValue> Default {get; private set; }
        public static ISerializer<TValue> ZigZag { get; private set; }
        public static ISerializer<TValue> TwosComplement { get; private set; }
        public static ISerializer<TValue> FixedSize { get; private set; }

        public static void Set(ISerializer<TValue> @default, ISerializer<TValue> zigZag,
            ISerializer<TValue> twosComplement, ISerializer<TValue> fixedSize)
        {
            if (@default == null) throw new ArgumentNullException("default");
            Default = @default;
            ZigZag = zigZag;
            TwosComplement = twosComplement;
            FixedSize = fixedSize;
        }
    }
}
