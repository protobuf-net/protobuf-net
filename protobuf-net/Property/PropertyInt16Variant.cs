
namespace ProtoBuf.Property
{
    internal sealed class PropertyInt16Variant<TSource> : Property<TSource, short>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.INT32; }
        }
        public override WireType WireType { get { return WireType.Variant; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            short value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context)
                + context.EncodeInt32(value);
        }

        public override short DeserializeImpl(TSource source, SerializationContext context)
        {
            return (short) Base128Variant.DecodeInt32(context);
        }
    }
}
