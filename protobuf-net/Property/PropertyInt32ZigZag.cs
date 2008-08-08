
namespace ProtoBuf.Property
{
    internal sealed class PropertyInt32ZigZag<TSource> : Property<TSource, int>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.SINT32; }
        }
        public override WireType WireType { get { return WireType.Variant; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            int value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context)
                + Base128Variant.EncodeUInt32(Base128Variant.Zig(value), context);
        }

        public override int DeserializeImpl(TSource source, SerializationContext context)
        {
            return Base128Variant.Zag(Base128Variant.DecodeUInt32(context));
        }
    }
}
