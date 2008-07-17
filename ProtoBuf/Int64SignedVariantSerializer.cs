
namespace ProtoBuf
{
    sealed class Int64SignedVariantSerializer : ISerializer<long>
    {
        private static uint WrapMsb(long value)
        {
            // strip the msb, left-shift all by one, and use the old msb as the new lsb
            unchecked
            {
                return (uint)((value << 1) ^ (value >> 63));
            }
        }



        public string DefinedType { get { return "sint64"; } }
        public WireType WireType { get { return WireType.Variant; } }
        public long Deserialize(long value, SerializationContext context)
        {
            return ReadFromStream(context);
        }
        public static long ReadFromStream(SerializationContext context)
        {
            // strip the lsb, right-shift all by one, and use the old lsb as the new msb
            ulong value = UInt64VariantSerializer.ReadFromStream(context);
            unchecked
            {
                return (long)((value >> 1) ^ (value << 63));
            }
        }
        public static int WriteToStream(long value, SerializationContext context)
        {
            return UInt64VariantSerializer.WriteToStream(WrapMsb(value), context);
        }
        public int Serialize(long value, SerializationContext context)
        {
            return WriteToStream(value, context);
        }
        public static int GetLength(long value)
        {
            return UInt64VariantSerializer.GetLength(WrapMsb(value));
        }
        public int GetLength(long value, SerializationContext context)
        {
            return GetLength(value);
        }
    }
}
