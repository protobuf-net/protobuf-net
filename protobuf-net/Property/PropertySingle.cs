
using System;
namespace ProtoBuf.Property
{
    internal sealed class PropertySingle<TSource> : Property<TSource, float>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.FIXED32; }
        }
        public override WireType WireType { get { return WireType.Fixed32; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            float value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context) + context.EncodeSingle(value);
        }

        public override float DeserializeImpl(TSource source, SerializationContext context)
        {
            return context.DecodeSingle();
        }
    }
}
