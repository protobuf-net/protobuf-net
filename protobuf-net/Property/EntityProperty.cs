
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
        
        protected override int GetLengthImpl(TValue value, SerializationContext context)
        {
            return GetValueLength(value, context);
        }

        public override void Deserialize(TEntity instance, SerializationContext context)
        {
            TValue value = GetValue(instance);
            bool set = value == null;
            value = ValueSerializer.Deserialize(value, context);
            if (set) SetValue(instance, value);
        }

        public override void DeserializeGroup(TEntity instance, SerializationContext context)
        {
            TValue value = GetValue(instance);
            bool set = value == null;
            value = GroupSerializer.DeserializeGroup(value, context);
            if (set) SetValue(instance, value);
        }
    }
}
