using System;

namespace ProtoBuf
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ProtoSerializerAttribute : Attribute
    {
        public ProtoSerializerAttribute(bool isMessage = true, WireType wireType = WireType.String)
        {
            IsMessage = isMessage;
            WireType = wireType;
        }
        public bool IsMessage { get; }
        public WireType WireType { get; }
    }
}
