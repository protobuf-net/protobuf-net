using ProtoBuf.Meta;

namespace ProtoBuf
{
    public interface ISerializationContext
    {
        TypeModel Model { get; }
        SerializationContext Context { get; }
    }
}
