
using System.Reflection;
using System.Collections.Generic;
namespace ProtoBuf
{
    internal abstract class MultiProperty<TEntity, TList, TValue> : PropertyBase<TEntity, TList, TValue>
        where TEntity : class, new()
        where TList : class, IEnumerable<TValue>
    {
        public MultiProperty(PropertyInfo property)
            : base(property)
        {}

        public override sealed bool IsRepeated { get { return true; } }

        public override int Serialize(TList list, SerializationContext context)
        { // write all items in a contiguous block
            int total = 0;
            foreach (TValue value in list)
            {
                total += SerializeValue(value, context);
            }
            return total;
        }

        protected override bool HasValue(TList list)
        {
            return list != null;
        }
        protected abstract bool AddItem(ref TList list, TValue value);

        public override void Deserialize(TEntity instance, SerializationContext context)
        {   // read a single item
            TValue value = DeserializeValue(default(TValue), context);
            TList list = GetValue(instance);
            if (AddItem(ref list, value))
            {
                SetValue(instance, list);
            }
            Trace(true, value, context);
        }
    }
}
