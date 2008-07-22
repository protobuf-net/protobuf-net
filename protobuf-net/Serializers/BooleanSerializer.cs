
namespace ProtoBuf
{
    sealed class BooleanSerializer : ISerializer<bool>
    {
        private BooleanSerializer() { }
        public static readonly BooleanSerializer Default = new BooleanSerializer();

        public string DefinedType { get { return ProtoFormat.BOOL; } }
        public WireType WireType { get { return WireType.Variant; } }
        public bool Deserialize(bool value, SerializationContext context)
        {
            return TwosComplementSerializer.ReadInt32(context) != 0;
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
