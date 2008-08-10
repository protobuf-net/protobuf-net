
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
            context.ReadBlock(8);
            byte[] buffer = context.Workspace;

            uint lo = (((uint)buffer[3]) << 24)
                | (((uint)buffer[2]) << 16)
                | (((uint)buffer[1]) << 8)
                | (((uint)buffer[0])),
                hi = (((uint)buffer[7]) << 24)
                | (((uint)buffer[6]) << 16)
                | (((uint)buffer[5]) << 8)
                | (((uint)buffer[4]));

            ulong loL = (ulong)lo, hiL = (ulong)hi;
            return (hiL << 32) | loL;
        }
    }
}
