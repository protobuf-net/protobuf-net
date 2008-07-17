using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf
{
    sealed class ListProperty<TEntity, TList, TValue> : PropertyBase<TEntity, TList>
        where TEntity : class, new()
        where TList : IList<TValue>
    {
        public ListProperty(PropertyInfo property)
            : base(property)
        {
            serializer = GetSerializer<TValue>(property);
        }
        private readonly ISerializer<TValue> serializer;

        public override string DefinedType { get { return serializer.DefinedType; } }
        public override WireType WireType { get { return serializer.WireType; } }

        public override int Serialize(TEntity instance, SerializationContext context)
        { // write all items in a contiguous block
            TList list = GetValue(instance);
            int total = 0;
            if (list != null && list.Count > 0)
            {
                foreach (TValue value in list)
                {
                    total += Serialize(value, serializer, context);
                }
            }
            return total;
        }
        public override int GetLength(TEntity instance, SerializationContext context)
        {
            TList list = GetValue(instance);
            int total = 0;
            if (list != null && list.Count > 0)
            {
                foreach (TValue value in list)
                {
                    total += GetLength(value, serializer, context);
                }
            }
            return total;
        }
        public override void Deserialize(TEntity instance, SerializationContext context)
        { // read a single item
            TList list = GetValue(instance);
            bool set = list == null;
            if (set) list = (TList)Activator.CreateInstance(typeof(TList));
            list.Add(serializer.Deserialize(default(TValue), context));
            if (set) SetValue(instance, list);
        }
    }
}
