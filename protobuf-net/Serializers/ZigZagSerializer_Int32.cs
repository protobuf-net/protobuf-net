
namespace ProtoBuf
{
    internal partial class ZigZagSerializer : ISerializer<int>
    {
        string ISerializer<int>.DefinedType { get { return ProtoFormat.SINT32; } }

        private static long ZigInt32(int value)
        {
            return ((value << 1) ^ (value >> 31));
        }

        public int Deserialize(int value, SerializationContext context)
        {
            return ReadInt32(context);
        }

        public static int ReadInt32(SerializationContext context)
        {
            int val = TwosComplementSerializer.ReadInt32(context);
            val = (-(val & 0x01)) ^ ((val >> 1) & ~Base128Variant.Int32Msb);
            return val;
        }

        public int Serialize(int value, SerializationContext context)
        {
            return TwosComplementSerializer.WriteToStream(ZigInt32(value), context);
        }

        public int GetLength(int value, SerializationContext context)
        {
            return GetLength(value);
        }
    }
}
