
namespace ProtoBuf.Property
{
    internal sealed class PropertyUInt32Variant<TSource> : Property<TSource, uint>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.UINT32; }
        }
        public override WireType WireType { get { return WireType.Variant; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            uint value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context)
                + context.EncodeUInt32(value);
        }

        public override uint DeserializeImpl(TSource source, SerializationContext context)
        {
            return Base128Variant.DecodeUInt32(context);
        }
    }
}
