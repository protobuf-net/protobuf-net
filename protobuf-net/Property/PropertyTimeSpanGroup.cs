using System;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf.Property
{

    internal sealed class PropertyTimeSpanGroup<TSource> : Property<TSource, TimeSpan>
    {
        public override string DefinedType
        {
            get { return "bcl.TimeSpan"; }
        }
        public override WireType WireType { get { return WireType.StartGroup; } }
        protected override void OnAfterInit()
        {
            base.OnAfterInit();
            suffix = GetPrefix(Tag, WireType.EndGroup);
        }
        private uint suffix;

        public override int Serialize(TSource source, SerializationContext context)
        {
            TimeSpan value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context)
                + ProtoTimeSpan.SerializeTimeSpan(value, context, false)
                + Base128Variant.EncodeUInt32(suffix, context);
        }

        public override TimeSpan DeserializeImpl(TSource source, SerializationContext context)
        {
            context.StartGroup(Tag); // will be ended internally
            return ProtoTimeSpan.DeserializeTimeSpan(context);
        }
    }
}
