using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf
{
    internal sealed class EnumerableProperty<TEntity, TList, TValue> : MultiProperty<TEntity, TList, TValue>
        where TEntity : class, new()
        where TList : class, IEnumerable<TValue>
    {

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

        protected override bool AddItem(ref TList list, TValue value)
        {
            bool set = list == null;
            if (set) list = (TList)Activator.CreateInstance(typeof(TList));
            add(list, value);
            return set;
        }
        
    }
}
