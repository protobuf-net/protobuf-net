
using System.Reflection;
namespace ProtoBuf
{
    internal sealed class NullableProperty<TEntity, TValue> : PropertyBase<TEntity, TValue?, TValue>
        where TEntity : class, new()
        where TValue : struct
    {
        public NullableProperty(PropertyInfo property)
            : base(property)
        { }

        protected override bool HasValue(TValue? value)
        {
            return value.HasValue;
        }
        public override int Serialize(TValue? value, SerializationContext context)
        {
            return SerializeValue(value.GetValueOrDefault(), context);
        }
        public override void Deserialize(TEntity instance, SerializationContext context)
        {
            TValue value = DeserializeValue(default(TValue), context);
            SetValue(instance, value);
            Trace(true, value, context);
        }
    }
}
