using System;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf.Property
{

    internal sealed class PropertyDateTimeGroup<TSource> : Property<TSource, DateTime>
    {
        public override string DefinedType
        {
            get { return "bcl.DateTime"; }
        }
        public override System.Collections.Generic.IEnumerable<Property<TSource>> GetCompatibleReaders()
        {
            yield return CreateAlternative<PropertyDateTimeString<TSource>>(DataFormat.Default);
        }
        private uint suffix;
        public override WireType WireType { get { return WireType.StartGroup; } }

        protected override void OnAfterInit()
        {
            base.OnAfterInit();
            suffix = GetPrefix(Tag, WireType.EndGroup);
        }
        
        public override int Serialize(TSource source, SerializationContext context)
        {
            DateTime value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context)
                + ProtoTimeSpan.SerializeDateTime(value, context, false)
                + context.EncodeUInt32(suffix);
        }

        public override DateTime DeserializeImpl(TSource source, SerializationContext context)
        {
            context.StartGroup(Tag); // will be ended internally
            return ProtoTimeSpan.DeserializeDateTime(context);
        }
    }
}
