namespace ProtoBuf
{
    public interface ISerializer<T>
    {
        void Read(ProtoReader reader, SerializationContext context, ref T value);

        void Write(ProtoWriter writer, SerializationContext context, ref T value);

        WireType WireType { get; }
    }

}
