
namespace ProtoBuf.Property
{
    internal sealed class PropertySByte<TSource> : Property<TSource, sbyte>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.SINT32; }
        }
        public override WireType WireType { get { return WireType.Variant; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            sbyte value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context)
                + Base128Variant.EncodeUInt32(Base128Variant.Zig((int)value), context);
        }

        public override sbyte DeserializeImpl(TSource source, SerializationContext context)
        {
            return (sbyte) Base128Variant.Zag(Base128Variant.DecodeUInt32(context));
        }
    }
}
