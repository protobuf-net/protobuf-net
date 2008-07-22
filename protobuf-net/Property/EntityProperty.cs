
using System.Reflection;
namespace ProtoBuf
{
    sealed class EntityProperty<TEntity, TValue> : PropertyBase<TEntity, TValue>
        where TEntity : class, new()
        where TValue : class, new()
    {
        private readonly ISerializer<TValue> serializer;
        public EntityProperty(PropertyInfo property)
            : base(property)
        {
            serializer = GetSerializer<TValue>(property);
        }
        public override WireType WireType { get { return serializer.WireType; } }
        public override string DefinedType { get { return serializer.DefinedType; } }

        public override int Serialize(TEntity instance, SerializationContext context)
        {
            TValue value = GetValue(instance);
            if (value == null) return 0;
            return Serialize(value, serializer, context);
        }
        public override int GetLength(TEntity instance, SerializationContext context)
        {
            TValue value = GetValue(instance);
            if (value == null) return 0;
            return GetLength(value, serializer, context);
        }
        public override void Deserialize(TEntity instance, SerializationContext context)
        {
            TValue value = GetValue(instance);
            bool set = value == null;
            value = serializer.Deserialize(value, context);
            if (set) SetValue(instance, value);
        }
    }
}
