
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
            byte[] buffer = context.Workspace;
            buffer[0] = (byte)(value & 0xFF);
            buffer[1] = (byte)((value >> 8)& 0xFF);
            buffer[2] = (byte)((value >> 16) & 0xFF);
            buffer[3] = (byte)((value >> 24) & 0xFF);
            buffer[4] = (byte)((value >> 32) & 0xFF);
            buffer[5] = (byte)((value >> 40) & 0xFF);
            buffer[6] = (byte)((value >> 48) & 0xFF);
            buffer[7] = (byte)((value >> 56) & 0xFF);
            int len = WritePrefix(context);
            context.Write(buffer, 0, 8);
            return len + 8;
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
