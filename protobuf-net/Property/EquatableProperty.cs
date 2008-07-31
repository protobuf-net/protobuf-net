using System;
using System.Reflection;

namespace ProtoBuf
{
    internal sealed class EquatableProperty<TEntity, TValue> : SimpleProperty<TEntity, TValue>
        where TEntity : class, new()
        where TValue : struct, IEquatable<TValue>
    {
        public EquatableProperty(PropertyInfo property)
            : base(property)
        {
            defaultValue = GetDefaultValue();
        }
        readonly TValue defaultValue;

        protected override bool HasValue(TValue value)
        {
            return !defaultValue.Equals(value);
        }
    }
}
