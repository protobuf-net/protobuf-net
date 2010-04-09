using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoBuf.Serializers
{
    interface ISerializerProxy
    {
        IProtoSerializer Serializer { get; }
    }
}
