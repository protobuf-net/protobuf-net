
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
        protected override int GetLengthImpl(TValue? value, SerializationContext context)
        {
            return GetValueLength(value.GetValueOrDefault(), context);
        }
        public override int Serialize(TValue? value, SerializationContext context)
        {
            return SerializeValue(value.GetValueOrDefault(), context);
        }
        public override void Deserialize(TEntity instance, SerializationContext context)
        {
            TValue value = ValueSerializer.Deserialize(default(TValue), context);
            SetValue(instance, value);
            Trace(true, value, context);
        }
        public override void DeserializeGroup(TEntity instance, SerializationContext context)
        {
            // read a single item
            TValue value = GroupSerializer.DeserializeGroup(default(TValue), context);
            SetValue(instance, value);
            Trace(true, value, context);
        }
    }
}
