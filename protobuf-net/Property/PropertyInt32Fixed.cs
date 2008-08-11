
namespace ProtoBuf.Property
{
    internal sealed class PropertyInt32Fixed<TSource> : Property<TSource, int>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.SFIXED32; }
        }
        public override WireType WireType { get { return WireType.Fixed32; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            int value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context) + context.EncodeInt32Fixed(value);
        }

        public override int DeserializeImpl(TSource source, SerializationContext context)
        {
            return context.DecodeInt32Fixed();
        }
    }
}
