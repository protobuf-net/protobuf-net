
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
                + context.EncodeUInt32(SerializationContext.Zig(value));
        }

        public override short DeserializeImpl(TSource source, SerializationContext context)
        {
            return (short)SerializationContext.Zag(context.DecodeUInt32());
        }
    }
}
