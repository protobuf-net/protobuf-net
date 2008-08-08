
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
            byte[] raw = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian) SerializationContext.Reverse4(raw);
            int len = WritePrefix(context);
            context.Write(raw, 0, 4);
            return len + 4;
        }

        public override float DeserializeImpl(TSource source, SerializationContext context)
        {
            context.ReadBlock(4);
            if (!BitConverter.IsLittleEndian) SerializationContext.Reverse4(context.Workspace);
            return BitConverter.ToSingle(context.Workspace, 0);
        }
    }
}
