using System;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf.Property
{

    internal sealed class PropertyGuidString<TSource> : Property<TSource, Guid>
    {
        public override string DefinedType
        {
            get { return "bcl.Guid"; }
        }
        public override WireType WireType { get { return WireType.String; } }
        public override int Serialize(TSource source, SerializationContext context)
        {
            Guid value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context)
                + ProtoGuid.Serialize(value, context, true);
        }

        public override Guid DeserializeImpl(TSource source, SerializationContext context)
        {
            long restore = context.LimitByLengthPrefix();
            Guid value = ProtoGuid.Deserialize(context);
            context.MaxReadPosition = restore;
            return value;
        }
    }
}
