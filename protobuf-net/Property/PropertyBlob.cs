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
                + Base128Variant.EncodeInt32(count, context);
            if (count > 0)
            {
                context.Write(value, 0, count);
            }
            return len + count;
        }

        public override byte[] DeserializeImpl(TSource source, SerializationContext context)
        {
            int count = Base128Variant.DecodeInt32(context);
            byte[] value = new byte[count];
            if (count > 0)
            {
                context.ReadBlock(value, count);
            }
            return value;
        }
    }
}
