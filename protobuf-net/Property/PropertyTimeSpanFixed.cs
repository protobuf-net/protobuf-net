
using System;
using ProtoBuf.ProtoBcl;
namespace ProtoBuf.Property
{
    internal sealed class PropertyTimeSpanFixed<TSource> : Property<TSource, TimeSpan>
    {
        public override string DefinedType
        {
            get { return ProtoFormat.SFIXED64; }
        }
        public override WireType WireType { get { return WireType.Fixed64; } }

        public override System.Collections.Generic.IEnumerable<Property<TSource>> GetCompatibleReaders()
        {
            yield return CreateAlternative<PropertyTimeSpanString<TSource>>(DataFormat.Default);
            yield return CreateAlternative<PropertyTimeSpanGroup<TSource>>(DataFormat.Group);
        }

        public override int Serialize(TSource source, SerializationContext context)
        {
            TimeSpan value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context) + context.EncodeInt64Fixed(value.Ticks);
        }

        public override TimeSpan DeserializeImpl(TSource source, SerializationContext context)
        {
            long ticks = context.DecodeInt64Fixed();
            return TimeSpan.FromTicks(ticks);
        }
    }
}
