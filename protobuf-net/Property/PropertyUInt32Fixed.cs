
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
            return (uint)context.DecodeInt32Fixed();
        }
    }
}
