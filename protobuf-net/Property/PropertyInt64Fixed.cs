
namespace ProtoBuf.Property
{
    internal sealed class PropertyInt64Fixed<TSource> : Property<TSource, long>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.SFIXED64; }
        }
        public override WireType WireType { get { return WireType.Fixed64; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            long value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context) + context.EncodeInt64Fixed(value);
        }

        public override long DeserializeImpl(TSource source, SerializationContext context)
        {
            return context.DecodeInt64Fixed();
        }
    }
}
