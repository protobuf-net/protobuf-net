namespace ProtoBuf.Property
{
    internal sealed class PropertyBlob<TSource> : Property<TSource, byte[]>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.BYTES; }
        }
        public override WireType WireType { get { return WireType.String; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            byte[] value = GetValue(source);
            if (value == null) return 0;
            int count = value.Length;

            int len = WritePrefix(context)
                + context.EncodeUInt32((uint)count);
            if (count > 0)
            {
                context.WriteBlock(value, 0, count);
            }
            return len + count;
        }

        public override byte[] DeserializeImpl(TSource source, SerializationContext context)
        {
            int count = (int) context.DecodeUInt32();
            byte[] value = new byte[count];
            if (count > 0)
            {
                context.ReadBlock(value, 0, count);
            }
            return value;
        }
    }
}
