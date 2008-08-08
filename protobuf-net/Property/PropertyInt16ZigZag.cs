
namespace ProtoBuf.Property
{
    internal sealed class PropertyInt16ZigZag<TSource> : Property<TSource, short>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.SINT32; }
        }
        public override WireType WireType { get { return WireType.Variant; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            short value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context)
                + Base128Variant.EncodeUInt32(Base128Variant.Zig(value), context);
        }

        public override short DeserializeImpl(TSource source, SerializationContext context)
        {
            return (short) Base128Variant.Zag(Base128Variant.DecodeUInt32(context));
        }
    }
}
