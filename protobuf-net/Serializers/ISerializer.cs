
namespace ProtoBuf
{
    interface ISerializer<TValue>
    {
        TValue Deserialize(TValue value, SerializationContext context);
        int Serialize(TValue value, SerializationContext context);
        int GetLength(TValue value, SerializationContext context);
        WireType WireType { get; }
        string DefinedType { get; }
    }
}
