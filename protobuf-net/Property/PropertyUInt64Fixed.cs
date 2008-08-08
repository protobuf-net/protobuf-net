
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
