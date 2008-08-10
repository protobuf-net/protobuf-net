
namespace ProtoBuf.Property
{
    internal sealed class PropertyInt64Variant<TSource> : Property<TSource, long>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.INT64; }
        }
        public override WireType WireType { get { return WireType.Variant; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            long value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context)
                + context.EncodeInt64(value);
        }

        public override long DeserializeImpl(TSource source, SerializationContext context)
        {
            return Base128Variant.DecodeInt64(context);
        }
    }
}
