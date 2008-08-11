
namespace ProtoBuf.Property
{
    internal sealed class PropertyByte<TSource> : Property<TSource, byte>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.INT32; }
        }
        public override WireType WireType { get { return WireType.Variant; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            byte value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context)
                + context.EncodeUInt32(value);
        }

        public override byte DeserializeImpl(TSource source, SerializationContext context)
        {
            return (byte) context.DecodeInt32();
        }
    }
}
