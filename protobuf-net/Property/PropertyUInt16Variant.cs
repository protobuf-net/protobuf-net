
using System;
namespace ProtoBuf.Property
{
    internal sealed class PropertyUInt16Variant<TSource> : Property<TSource, ushort>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.UINT32; }
        }
        public override WireType WireType { get { return WireType.Variant; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            ushort value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context)
                + context.EncodeUInt32(value);
        }

        public override ushort DeserializeImpl(TSource source, SerializationContext context)
        {
            uint value = context.DecodeUInt32();
            checked { return (ushort)value; }
        }
    }
}
