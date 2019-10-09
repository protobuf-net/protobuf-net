namespace ProtoBuf.Internal.Serializers
{
    interface ISerializerProxy
    {
        IRuntimeProtoSerializerNode Serializer { get; }
    }
}