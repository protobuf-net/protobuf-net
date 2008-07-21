
namespace ProtoBuf
{
    partial class ZigZagSerializer : ISerializer<int>
    {
        string ISerializer<int>.DefinedType { get { return ProtoFormat.SINT32; } }


        private static uint WrapMsb(int value)
        {
            unchecked
            {
                return (uint)((value << 1) ^ (value >> 31));
            }
        }
        public int Deserialize(int value, SerializationContext context)
        {
            return ReadInt32(context);
        }
        public static int ReadInt32(SerializationContext context)
        {
            int val = TwosComplementSerializer.ReadInt32(context);
            val = (-(val & 0x01)) ^ ((val >> 1) & ~Base128Variant.INT32_MSB);
            return val;
        }
        public int Serialize(int value, SerializationContext context)
        {
            return TwosComplementSerializer.WriteToStream(WrapMsb(value), context);
        }
        public int GetLength(int value, SerializationContext context)
        {
            return GetLength(value);
        }
    }
}
