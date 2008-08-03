
using System.Reflection;
using System;
namespace ProtoBuf
{
    internal sealed class EntityProperty<TEntity, TValue> : PropertyBase<TEntity, TValue, TValue>
        where TEntity : class, new()
        where TValue : class, new()
    {
        public EntityProperty(PropertyInfo property)
            : base(property)
        {}
        
        protected override bool HasValue(TValue value)
        {
            return value != null;
        }
        public override int Serialize(TValue value, SerializationContext context)
        {
            return SerializeValue(value, context);
        }
        
        public override void Deserialize(TEntity instance, SerializationContext context)
        {
            TValue value = GetValue(instance);
            bool set = value == null;
            value = base.DeserializeValue(value, context);
            if (set) SetValue(instance, value);
        }
    }
}
