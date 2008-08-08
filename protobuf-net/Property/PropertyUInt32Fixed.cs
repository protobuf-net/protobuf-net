
namespace ProtoBuf.Property
{
    internal sealed class PropertyUInt32Fixed<TSource> : Property<TSource, uint>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.FIXED32; }
        }
        public override WireType WireType { get { return WireType.Fixed32; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            uint value = GetValue(source);
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

        public override uint DeserializeImpl(TSource source, SerializationContext context)
        {
            context.ReadBlock(4);
            byte[] buffer = context.Workspace;
            return (((uint)buffer[3]) << 24)
                | (((uint)buffer[2]) << 16)
                | (((uint)buffer[1]) << 8)
                | (((uint)buffer[0]));
        }
    }
}
