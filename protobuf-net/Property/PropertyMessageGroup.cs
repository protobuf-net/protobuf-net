
namespace ProtoBuf.Property
{
    internal sealed class PropertyMessageGroup<TSource, TValue> : Property<TSource, TValue>
        where TValue : class, new()
    {
        public override string DefinedType
        {
            get { return Serializer.GetDefinedTypeName<TValue>(); }
        }
        public override WireType WireType { get { return WireType.StartGroup; } }

        protected override void OnAfterInit()
        {
            base.OnAfterInit();
            suffix = GetPrefix(Tag, WireType.EndGroup);
            Serializer<TValue>.Build();
        }
        
        private uint suffix;

        public override int Serialize(TSource source, SerializationContext context)
        {
            TValue value = GetValue(source);
            if (value == null) return 0;
            
            return WritePrefix(context)
                + Serializer<TValue>.Serialize(value, context)
                + context.EncodeUInt32(suffix);
        }
        public override TValue DeserializeImpl(TSource source, SerializationContext context)
        {
            TValue value = GetValue(source);
            if (value == null) value = new TValue();
            context.StartGroup(Tag); // will be ended internally
            Serializer<TValue>.Deserialize(value, context);
            return value;     
        }
     }
}
