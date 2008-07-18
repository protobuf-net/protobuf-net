
namespace ProtoBuf
{
    partial class ZigZagSerializer : ISerializer<int>
    {
        string ISerializer<int>.DefinedType { get { return ProtoFormat.SINT32; } }


        private static uint WrapMsb(int value)
        {
            // strip the msb, left-shift all by one, and use the old msb as the new lsb
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
            // strip the lsb, right-shift all by one, and use the old lsb as the new msb
            uint uVal = TwosComplementSerializer.ReadUInt32(context);
            unchecked
            {
                return (int)((uVal >> 1) ^ (uVal << 31));
            }
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
