
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
                + context.EncodeUInt32(SerializationContext.Zig((int)value));
        }

        public override sbyte DeserializeImpl(TSource source, SerializationContext context)
        {
            return (sbyte)SerializationContext.Zag(context.DecodeUInt32());
        }
    }
}
