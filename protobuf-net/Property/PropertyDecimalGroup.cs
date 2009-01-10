using System;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf.Property
{

    internal sealed class PropertyDecimalGroup<TSource> : Property<TSource, Decimal>
    {
        public override string DefinedType
        {
            get { return "bcl.Decimal"; }
        }
        public override System.Collections.Generic.IEnumerable<Property<TSource>> GetCompatibleReaders()
        {
            yield return CreateAlternative<PropertyDecimalString<TSource>>(DataFormat.Default);
        }
        public override WireType WireType { get { return WireType.StartGroup; } }

        protected override void OnAfterInit()
        {
            base.OnAfterInit();
            suffix = Serializer.GetFieldToken(Tag, WireType.EndGroup);
        }
        private uint suffix;

        public override int Serialize(TSource source, SerializationContext context)
        {
            Decimal value = GetValue(source);
            if (IsOptional && value == DefaultValue) return 0;
            return WritePrefix(context)
                + ProtoDecimal.SerializeDecimal(value, context, false)
                + context.EncodeUInt32(suffix);
        }

        public override Decimal DeserializeImpl(TSource source, SerializationContext context)
        {
            context.StartGroup(Tag); // will be ended internally
            return ProtoDecimal.DeserializeDecimal(context);
        }
    }
}
