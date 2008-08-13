
using System.Reflection;
namespace ProtoBuf.Property
{
    internal sealed class PropertyNullable<TSource, TValue> : Property<TSource, TValue?>
        where TValue : struct
    {
        public override string DefinedType
        {
            get { return ProtoFormat.INT32; }
        }
        private Property<TValue, TValue> innerProperty;

        protected override void OnBeforeInit(int tag)
        {
            innerProperty = PropertyFactory.CreatePassThru<TValue>(tag, DataFormat);
            base.OnBeforeInit(tag);
        }
        public override WireType WireType
        {
            get { return innerProperty.WireType; }
        }
        public override int Serialize(TSource source, SerializationContext context)
        {
            TValue? value = GetValue(source);
            return value.HasValue ? innerProperty.Serialize(value.GetValueOrDefault(), context) : 0;
        }
        public override TValue? DeserializeImpl(TSource source, SerializationContext context)
        {
            return innerProperty.DeserializeImpl(default(TValue), context);
        }
    }
}
