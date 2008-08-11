
namespace ProtoBuf.Property
{
    internal sealed class PropertyBoolean<TSource> : Property<TSource, bool>
    {
        public override string DefinedType { get { return ProtoFormat.BOOL; } }
        public override WireType WireType { get { return WireType.Variant; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            bool value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            int len = WritePrefix(context);
            context.WriteByte(value ? (byte)1 : (byte)0);
            return len + 1;
        }

        public override bool DeserializeImpl(TSource source, SerializationContext context)
        {
            int i = context.DecodeInt32();
            return i != 0;
        }
    }
}
