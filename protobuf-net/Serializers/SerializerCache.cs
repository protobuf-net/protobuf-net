
using System;
namespace ProtoBuf
{
    internal static class SerializerCache<TValue>
    {
        static SerializerCache()
        {
            SimpleSerializers.Init();
        }
        private static ISerializer<TValue> _default, zigZag, twos, fixedSize;
        public static ISerializer<TValue> Default { get { return _default; } private set { _default = value; } }
        public static ISerializer<TValue> ZigZag { get { return zigZag; } private set { zigZag = value; } }
        public static ISerializer<TValue> TwosComplement { get { return twos; } private set { twos = value; } }
        public static ISerializer<TValue> FixedSize { get { return fixedSize; } private set { fixedSize = value; } }

        public static void Set(ISerializer<TValue> @default, ISerializer<TValue> zigZag,
            ISerializer<TValue> twosComplement, ISerializer<TValue> fixedSize)
        {
            Default = @default;
            ZigZag = zigZag;
            TwosComplement = twosComplement;
            FixedSize = fixedSize;
        }
    }
}
