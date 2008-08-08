using System;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf.Property
{

    internal sealed class PropertyDecimalString<TSource> : Property<TSource, Decimal>
    {
        public override string DefinedType
        {
            get { return "bcl.Decimal"; }
        }
        public override WireType WireType { get { return WireType.String; } }
        public override int Serialize(TSource source, SerializationContext context)
        {
            Decimal value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context)
                + ProtoDecimal.SerializeDecimal(value, context, true);
        }

        public override Decimal DeserializeImpl(TSource source, SerializationContext context)
        {
            long restore = context.LimitByLengthPrefix();
            Decimal value = ProtoDecimal.DeserializeDecimal(context);
            context.MaxReadPosition = restore;
            return value;
        }
    }
}
