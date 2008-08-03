
namespace ProtoBuf
{
    internal interface ISerializer<TValue>
    {
        TValue Deserialize(TValue value, SerializationContext context);
        int Serialize(TValue value, SerializationContext context);
        
        WireType WireType { get; }
        string DefinedType { get; }
    }

    internal interface ILengthSerializer<TValue> : ISerializer<TValue>
    {
        int UnderestimateLength(TValue value);
        bool CanBeGroup { get; }
    }
}
