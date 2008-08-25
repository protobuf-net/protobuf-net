
namespace ProtoBuf.Property
{
    internal sealed class PropertyMessageGroup<TSource, TValueBase, TValueActual> : Property<TSource, TValueBase>
        where TValueBase : class, new()
        where TValueActual : class, TValueBase, new()
    {
        public override System.Collections.Generic.IEnumerable<Property<TSource>> GetCompatibleReaders()
        {
            yield return CreateAlternative<PropertyMessageString<TSource, TValueBase, TValueActual>>(DataFormat.Default);
        }

        public override string DefinedType
        {
            get { return Serializer.GetDefinedTypeName<TValueBase>(); }
        }
        public override WireType WireType { get { return WireType.StartGroup; } }

        protected override void OnAfterInit()
        {
            base.OnAfterInit();
            suffix = GetPrefix(Tag, WireType.EndGroup);
            Serializer<TValueActual>.Build();
        }
        
        private uint suffix;

        public override int Serialize(TSource source, SerializationContext context)
        {
            TValueActual value = (TValueActual) GetValue(source);
            if (value == null) return 0;
            
            return WritePrefix(context)
                + Serializer<TValueActual>.Serialize(value, context)
                + context.EncodeUInt32(suffix);
        }
        public override TValueBase DeserializeImpl(TSource source, SerializationContext context)
        {
            TValueActual value = Serializer<TValueBase>.CheckSubType<TValueActual>(GetValue(source));

            context.StartGroup(Tag); // will be ended internally
            Serializer<TValueActual>.Deserialize(ref value, context);
            return value;     
        }
     }
}
