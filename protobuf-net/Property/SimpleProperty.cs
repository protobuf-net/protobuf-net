using System.Reflection;
using System;
using System.Globalization;

namespace ProtoBuf
{
    internal abstract class SimpleProperty<TEntity, TValue> : PropertyBase<TEntity, TValue, TValue>
        where TEntity : class, new()
    {
        public SimpleProperty(PropertyInfo property)
            : base(property)
        {}

        protected TValue GetDefaultValue()
        {
            object untypedDefault = DefaultValue;
            if (untypedDefault == null)
            {
                return default(TValue);
            }
            else
            {
                return (TValue)Convert.ChangeType(
                    untypedDefault, typeof(TValue), CultureInfo.InvariantCulture);
            }
        }
   
        protected sealed override int GetLengthImpl(TValue value, SerializationContext context)
        {
            return GetValueLength(value, context);
        }
        public sealed override int Serialize(TValue value, SerializationContext context)
        {
            return SerializeValue(value, context);
        }
        public sealed override void Deserialize(TEntity instance, SerializationContext context)
        {
            TValue value = ValueSerializer.Deserialize(default(TValue), context);
            SetValue(instance, value);
            Trace(true, value, context);
        }
        public sealed override void DeserializeGroup(TEntity instance, SerializationContext context)
        {
            // read a single item
            TValue value = GroupSerializer.DeserializeGroup(default(TValue), context);
            SetValue(instance, value);
            Trace(true, value, context);
        }
    }
}
