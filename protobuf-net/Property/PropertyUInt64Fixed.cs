
namespace ProtoBuf.Property
{
    internal sealed class PropertyUInt64Fixed<TSource> : Property<TSource, ulong>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.FIXED64; }
        }
        public override WireType WireType { get { return WireType.Fixed64; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            ulong value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;

            return WritePrefix(context) + context.EncodeInt64Fixed((long)value);
        }

        public override ulong DeserializeImpl(TSource source, SerializationContext context)
        {
            return (ulong) context.DecodeInt64Fixed();
        }
    }
}
