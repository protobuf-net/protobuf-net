
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
                + context.EncodeUInt32(SerializationContext.Zig(value));
        }

        public override int DeserializeImpl(TSource source, SerializationContext context)
        {
            return SerializationContext.Zag(context.DecodeUInt32());
        }
    }
}
