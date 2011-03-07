using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf.Meta;

namespace ProtoBuf
{
    internal sealed class NetObjectCache
    {
        private readonly BasicList list = new BasicList();


        internal object GetKeyedObject(int key)
        {
            return list[key];
        }

        internal void SetKeyedObject(int key, object value)
        {
            if (key != list.Add(value))
            {
                throw new ProtoException("Internal error; a key mismatch occurred");
            }
        }
        class StringMatch : BasicList.IPredicate
        {
            private readonly string value;
            public StringMatch(string value) {
                this.value = value;
            }
            public bool IsMatch(object obj)
            {
                string s;
                return (s = obj as string) != null && s == value;
            }
        }
        internal int AddObjectKey(object value, out bool existing)
        {
            if (value == null) throw new ArgumentNullException("value");
            string s;
            int index = ((s = value as string) == null) ? list.IndexOfReference(value) : list.IndexOf(new StringMatch(s));
          
            if (!(existing = index >= 0))
            {
                index = list.Add(value);
            }
            return index;
        }
    }
}
