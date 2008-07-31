using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf
{
    internal sealed class EnumerableProperty<TEntity, TList, TValue> : PropertyBase<TEntity, TList, TValue>
        where TEntity : class, new()
        where TList : class, IEnumerable<TValue>
    {

        protected override bool HasValue(TList list)
        {
            return list != null;
        }
        public EnumerableProperty(PropertyInfo property)
            : base(property)
        {
            MethodInfo addMethod = typeof(TList).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public,
                null, CallingConventions.HasThis, new Type[] { typeof(TValue) }, null);

            if (addMethod == null) throw new InvalidOperationException("For pipeline usage, the type must have a public void Add(T obj) method");

#if CF2
            add = delegate(TList list, TValue value)
            {
                addMethod.Invoke(list, new object[] { value });
            };
#else
            add = (Setter<TList, TValue>) Delegate.CreateDelegate(
                typeof(Setter<TList, TValue>), null, addMethod);
#endif
        }
        private readonly Setter<TList, TValue> add;

        public override bool IsRepeated { get { return true; } }

        public override int Serialize(TList list, SerializationContext context)
        { // write all items in a contiguous block
            int total = 0;
            foreach (TValue value in list)
            {
                total += SerializeValue(value, context);
            }
            return total;
        }

        protected override int GetLengthImpl(TList list, SerializationContext context)
        {
            if (list == null) return 0;
            if (IsGroup) return -1;
            int total = 0;
            foreach (TValue value in list)
            {
                total += GetValueLength(value, context);
            }
            return total;
        }
        private void AddItem(TEntity instance, TValue value)
        {
            TList list = GetValue(instance);
            bool set = list == null;
            if (set) list = (TList)Activator.CreateInstance(typeof(TList));
            add(list, value);
            if (set) SetValue(instance, list);
        }
        public override void Deserialize(TEntity instance, SerializationContext context)
        {   // read a single item
            TValue value = ValueSerializer.Deserialize(default(TValue), context);
            AddItem(instance, value);
            Trace(true, value, context);
        }

        public override void DeserializeGroup(TEntity instance, SerializationContext context)
        {
            // read a single item
            TValue value = GroupSerializer.DeserializeGroup(default(TValue), context);
            AddItem(instance, value);
            Trace(true, value, context);
        }
    }
}
