using System;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf.Property
{

    internal sealed class PropertyDateTimeString<TSource> : Property<TSource, DateTime>
    {
        public override string DefinedType
        {
            get { return "bcl.DateTime"; }
        }
        public override WireType WireType { get { return WireType.String; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            DateTime value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context)
                + ProtoTimeSpan.SerializeDateTime(value, context, true);
        }

        public override DateTime DeserializeImpl(TSource source, SerializationContext context)
        {
            long restore = context.LimitByLengthPrefix();
            DateTime value = ProtoTimeSpan.DeserializeDateTime(context);
            context.MaxReadPosition = restore;
            return value;
        }
    }
}
