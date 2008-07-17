
namespace ProtoBuf
{
    sealed class Int32SignedVariantSerializer : ISerializer<int>
    {
        private static uint WrapMsb(int value)
        {
            // strip the msb, left-shift all by one, and use the old msb as the new lsb
            unchecked
            {
                return (uint)((value << 1) ^ (value >> 31));
            }
        }
        public string DefinedType { get { return "sint32"; } }
        public WireType WireType { get { return WireType.Variant; } }
        public int Deserialize(int value, SerializationContext context)
        {
            // strip the lsb, right-shift all by one, and use the old lsb as the new msb
            uint uVal = UInt32VariantSerializer.ReadFromStream(context);
            unchecked
            {
                return (int)((value >> 1) ^ (value << 31));
            }
        }
        public int Serialize(int value, SerializationContext context)
        {
            return UInt32VariantSerializer.WriteToStream(WrapMsb(value), context);
        }
        public int GetLength(int value, SerializationContext context)
        {
            return UInt32VariantSerializer.GetLength(WrapMsb(value));
        }
    }
}
