
namespace ProtoBuf.Property
{
    internal sealed class PropertyUInt64Variant<TSource> : Property<TSource, ulong>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.UINT64; }
        }
        public override WireType WireType { get { return WireType.Variant; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            ulong value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context)
                + context.EncodeUInt64(value);
        }

        public override ulong DeserializeImpl(TSource source, SerializationContext context)
        {
            return Base128Variant.DecodeUInt64(context);
        }
    }
}
