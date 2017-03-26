using System;

namespace ProtoBuf
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ProtoSerializerAttribute : Attribute
    {
        public WireType WireType { get; set; } = WireType.String;
    }
}
