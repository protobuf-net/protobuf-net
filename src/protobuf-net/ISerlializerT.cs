namespace ProtoBuf
{
    public interface ISerializer<T>
    {
        void Read(ProtoReader reader, ref T value);

        void Write(ProtoWriter writer, ref T value);
    }

}
