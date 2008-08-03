using System.Reflection;
using System;
using System.Globalization;
using System.ComponentModel;

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
                try
                {
                    return (TValue)Convert.ChangeType(
                        untypedDefault, typeof(TValue), CultureInfo.InvariantCulture);
                }
                catch
                {
#if !SILVERLIGHT && !CF
                    TypeConverter tc = TypeDescriptor.GetConverter(typeof(TValue));
                    if(tc.CanConvertFrom(untypedDefault.GetType()))
                    {
                        return (TValue)tc.ConvertFrom(null, CultureInfo.InvariantCulture, untypedDefault);
                    }
#endif
                    throw;
                }
            }
        }
   
        public sealed override int Serialize(TValue value, SerializationContext context)
        {
            return SerializeValue(value, context);
        }
        public sealed override void Deserialize(TEntity instance, SerializationContext context)
        {
            TValue value = DeserializeValue(default(TValue), context);
            SetValue(instance, value);
            Trace(true, value, context);
        }
    }
}
