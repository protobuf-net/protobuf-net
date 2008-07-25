
using System.Reflection;
using System;
namespace ProtoBuf
{
    internal sealed class EntityProperty<TEntity, TValue> : PropertyBase<TEntity, TValue>, IGroupProperty<TEntity>
        where TEntity : class, new()
        where TValue : class, new()
    {
        private readonly IGroupSerializer<TValue> serializer;
        public EntityProperty(PropertyInfo property)
            : base(property)
        {
            // entity serializers can always handle groups
            serializer = (IGroupSerializer<TValue>)GetSerializer<TValue>(property);
        }

        public override WireType WireType { get { return serializer.WireType; } }
        public override string DefinedType { get { return serializer.DefinedType; } }

        public override int Serialize(TEntity instance, SerializationContext context)
        {
            TValue value = GetValue(instance);
            if (value == null)
            {
                return 0;
            }
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

        public void DeserializeGroup(TEntity instance, SerializationContext context)
        {
            TValue value = GetValue(instance);
            bool set = value == null;
            value = serializer.DeserializeGroup(value, context);
            if (set) SetValue(instance, value);
        }
    }
}
