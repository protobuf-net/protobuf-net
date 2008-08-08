using System;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf.Property
{

    internal sealed class PropertyTimeSpanString<TSource> : Property<TSource, TimeSpan>
    {
        public override string DefinedType
        {
            get { return "bcl.TimeSpan"; }
        }
        public override WireType WireType { get { return WireType.String; } }
        public override int Serialize(TSource source, SerializationContext context)
        {
            TimeSpan value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context)
                + ProtoTimeSpan.SerializeTimeSpan(value, context, true);
        }

        public override TimeSpan DeserializeImpl(TSource source, SerializationContext context)
        {
            long restore = context.LimitByLengthPrefix();
            TimeSpan value = ProtoTimeSpan.DeserializeTimeSpan(context);
            context.MaxReadPosition = restore;
            return value;
        }
    }
}
