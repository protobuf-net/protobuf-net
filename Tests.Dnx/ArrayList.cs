#if COREFX
using System.Collections.Generic;

namespace System.Collections
{
    // fake it until you can make it
    public class ArrayList : IList, IEnumerable
    {
        private readonly IList items = new List<object>();
        public int Add(object obj) { return items.Add(obj); }
        public int Count {  get { return items.Count; } }

        public bool IsFixedSize
        {
            get { return items.IsFixedSize; }
        }

        public bool IsReadOnly
        {
            get { return items.IsReadOnly; }
        }

        public bool IsSynchronized
        {
            get { return items.IsSynchronized; }
        }

        public object SyncRoot
        {
            get { return items.SyncRoot; }
        }

        public object this[int index]
        {
            get { return items[index]; }
            set { items[index] = value; }
        }

        public IEnumerator GetEnumerator()
        {
            return items.GetEnumerator();
        }

        public void Clear()
        {
            items.Clear();
        }

        public bool Contains(object value)
        {
            return items.Contains(value);
        }

        public int IndexOf(object value)
        {
            return items.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            items.Insert(index, value);
        }

        public void Remove(object value)
        {
            items.Remove(value);
        }

        public void RemoveAt(int index)
        {
            items.RemoveAt(index);
        }

        public void CopyTo(Array array, int index)
        {
            items.CopyTo(array, index);
        }
    }
}

#endif