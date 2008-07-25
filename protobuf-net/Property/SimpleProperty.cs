
using System.Reflection;
using System;
namespace ProtoBuf
{
    internal sealed class SimpleProperty<TEntity, TValue> : PropertyBase<TEntity, TValue>
        where TEntity : class, new()
    {
        public SimpleProperty(PropertyInfo property)
            : base(property)
        {
            this.serializer = GetSerializer<TValue>(property);
        }
        private readonly ISerializer<TValue> serializer;

        public override string DefinedType { get { return serializer.DefinedType; } }
        public override WireType WireType { get { return serializer.WireType; } }

        public override int GetLength(TEntity instance, SerializationContext context)
        {
            return GetLength(GetValue(instance), serializer, context);
        }
        public override int Serialize(TEntity instance, SerializationContext context)
        {
            return Serialize(GetValue(instance), serializer, context);
        }
        public override void Deserialize(TEntity instance, SerializationContext context)
        {
            TValue value = serializer.Deserialize(default(TValue), context);
            SetValue(instance, value);
            Trace(true, value, context);
        }
    }
}
