using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf
{
    internal sealed class ListProperty<TEntity, TList, TValue> : MultiProperty<TEntity, TList, TValue>
        where TEntity : class, new()
        where TList : class, IList<TValue>
    {
        public ListProperty(PropertyInfo property)
            : base(property)
        {}

        protected override bool HasValue(TList list)
        {
            return list != null && list.Count > 0;
        }
        protected override bool AddItem(ref TList list, TValue value)
        {
            bool set = list == null;
            if (set) list = (TList)Activator.CreateInstance(typeof(TList));
            list.Add(value);
            return set;
        }

    }
}
