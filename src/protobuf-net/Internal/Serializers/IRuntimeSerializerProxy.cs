using System;

namespace ProtoBuf.Internal.Serializers;

internal interface IRuntimeSerializerProxy
{
    Type ExpectedType { get; }
    IRuntimeProtoSerializerNode GetSerializer();
}