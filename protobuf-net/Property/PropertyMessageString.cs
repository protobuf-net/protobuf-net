
namespace ProtoBuf.Property
{
    internal sealed class PropertyMessageString<TSource, TValue> : Property<TSource, TValue>, ILengthProperty<TValue>
        where TValue : class, new()
    {
        public override string DefinedType
        {
            get { return Serializer.GetDefinedTypeName<TValue>(); }
        }

        public override WireType WireType { get { return WireType.String; } }

        protected override void OnAfterInit()
        {
            base.OnAfterInit();
            Serializer<TValue>.Build();
        }

        public override int Serialize(TSource source, SerializationContext context)
        {
            TValue value = GetValue(source);
            if (value == null) return 0;

            return WritePrefix(context)
                + context.WriteLengthPrefixed(value, 0, this);
        }

        public override TValue DeserializeImpl(TSource source, SerializationContext context)
        {
            TValue value = GetValue(source);
            if (value == null) value = new TValue();

            long restore = context.LimitByLengthPrefix();
            Serializer<TValue>.Deserialize(value, context);
            // restore the max-pos
            context.MaxReadPosition = restore;

            return value;
        }

        int ILengthProperty<TValue>.Serialize(TValue value, SerializationContext context)
        {
            return Serializer<TValue>.Serialize(value, context);
        }
    }
}
