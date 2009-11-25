
using System;
using ProtoBuf.ProtoBcl;
namespace ProtoBuf.Property
{
    internal sealed class PropertyDateTimeFixed<TSource> : Property<TSource, DateTime>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.SFIXED64; }
        }
        public override WireType WireType { get { return WireType.Fixed64; } }

        public override System.Collections.Generic.IEnumerable<Property<TSource>> GetCompatibleReaders()
        {
            yield return CreateAlternative<PropertyDateTimeString<TSource>>(DataFormat.Default);
            yield return CreateAlternative<PropertyDateTimeGroup<TSource>>(DataFormat.Group);
        }
        public override int Serialize(TSource source, SerializationContext context)
        {
            DateTime value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            long ticks = (value - ProtoTimeSpan.EpochOrigin).Ticks;
            return WritePrefix(context) + context.EncodeInt64Fixed(ticks);
        }

        public override DateTime DeserializeImpl(TSource source, SerializationContext context)
        {
            long ticks = context.DecodeInt64Fixed();
            DateTime value = ProtoTimeSpan.EpochOrigin.AddTicks(ticks);
            return value;
        }
    }
}
