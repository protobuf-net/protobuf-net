namespace ProtoBuf.Serializers
{
    interface ISerializerProxy
    {
        IRuntimeProtoSerializerNode Serializer { get; }
    }
}