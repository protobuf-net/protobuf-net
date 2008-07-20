
namespace ProtoBuf
{
    partial class ZigZagSerializer : ISerializer<int>, ISerializer<long>
    {
        private static ulong WrapMsb(long value)
        {
            unchecked
            {
                return (ulong)((value << 1) ^ (value >> 63));
            }
        }

        public int Serialize(long value, SerializationContext context)
        {
            return WriteToStream(value, context);
        }
        public long Deserialize(long value, SerializationContext context)
        {
            return ReadInt64(context);
        }


        string ISerializer<long>.DefinedType { get { return ProtoFormat.SINT64; } }
        public int GetLength(long value, SerializationContext context)
        {
            return GetLength(value);
        }
        public static int GetLength(int value)
        {
            return TwosComplementSerializer.GetLength(WrapMsb(value));
        }
        public static int GetLength(long value)
        {
            return TwosComplementSerializer.GetLength(WrapMsb(value));
        }
        public static long ReadInt64(SerializationContext context)
        {
            ulong uVal = TwosComplementSerializer.ReadUInt64(context);
            unchecked
            {
                return (long)((uVal >> 1) ^ (uVal << 63));
            }
        }

        internal static int WriteToStream(long value, SerializationContext context)
        {
            return TwosComplementSerializer.WriteToStream(WrapMsb(value), context);
        }
    }
}
