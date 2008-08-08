
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
            byte[] buffer = context.Workspace;
            buffer[0] = (byte)(value & 0xFF);
            buffer[1] = (byte)((value >> 8)& 0xFF);
            buffer[2] = (byte)((value >> 16) & 0xFF);
            buffer[3] = (byte)((value >> 24) & 0xFF);
            int len = WritePrefix(context);
            context.Write(buffer, 0, 4);
            return len + 4;
        }

        public override int DeserializeImpl(TSource source, SerializationContext context)
        {
            context.ReadBlock(4);
            byte[] buffer = context.Workspace;
            return (((int)buffer[3]) << 24)
                | (((int)buffer[2]) << 16)
                | (((int)buffer[1]) << 8)
                | (((int)buffer[0]));
        }
    }
}
