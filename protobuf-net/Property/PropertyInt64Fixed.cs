
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
            context.ReadBlock(8);
            byte[] buffer = context.Workspace;

            int lo = (((int)buffer[3]) << 24)
                | (((int)buffer[2]) << 16)
                | (((int)buffer[1]) << 8)
                | (((int)buffer[0])),
                hi = (((int)buffer[7]) << 24)
                | (((int)buffer[6]) << 16)
                | (((int)buffer[5]) << 8)
                | (((int)buffer[4]));

            long loL = (long)lo, hiL = (long)hi;
            return (hiL << 32) | loL;
        }
    }
}
