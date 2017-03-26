using System;

namespace ProtoBuf.Meta
{
    public sealed class CustomSerializer
    {
        public Type Type { get; }
        public bool IsMessage { get; }
        public WireType WireType { get; }
        public CustomSerializer(Type type, bool isMessage, WireType wireType)
        {
            Type = type;
            IsMessage = isMessage;
            WireType = wireType;
        }
    }
}
