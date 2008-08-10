
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
            return WritePrefix(context) + context.EncodeInt32Fixed((int)value);
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
