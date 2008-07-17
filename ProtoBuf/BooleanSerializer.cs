
namespace ProtoBuf
{
    sealed class BooleanSerializer : ISerializer<bool>
    {
        public string DefinedType { get { return "bool"; } }
        public WireType WireType { get { return WireType.Variant; } }
        public bool Deserialize(bool value, SerializationContext context)
        {
            return Int32VariantSerializer.ReadFromStream(context) != 0;
        }
        public int GetLength(bool value, SerializationContext context)
        {
            return 1;
        }
        public int Serialize(bool value, SerializationContext context)
        {
            context.Stream.WriteByte(value ? (byte)1 : (byte)0);
            return 1;
        }
    }
}
