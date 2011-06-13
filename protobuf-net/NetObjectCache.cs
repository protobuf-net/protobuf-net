using System;
using ProtoBuf.Meta;

namespace ProtoBuf
{
    internal sealed class NetObjectCache
    {
        internal const int Root = 0;
        private BasicList underlyingList;

        private BasicList List { get {
            if (underlyingList == null) underlyingList = new BasicList();
            return underlyingList;
        } }


        internal object GetKeyedObject(int key)
        {
            if (key-- == Root)
            {
                if (rootObject == null) throw new ProtoException("No root object assigned");
                return rootObject;
            }
            BasicList list = List;

            if (key < 0 || key >= list.Count)
            {
                Helpers.DebugWriteLine("Missing key: " + key);
                throw new ProtoException("Internal error; a missing key occurred");
            }
            
            return list[key];
        }

        internal void SetKeyedObject(int key, object value)
        {
            if (key-- == Root)
            {
                if (value == null) throw new ArgumentNullException("value");
                if (rootObject != null && ((object)rootObject != (object)value)) throw new ProtoException("The root object cannot be reassigned");
                rootObject = value;
            }
            else
            {
                BasicList list = List;
                if (key < list.Count)
                {
                    if (!ReferenceEquals(list[key], value)) throw new ProtoException("Reference-tracked objects cannot change reference");
                }
                else if (key != list.Add(value))
                {
                    throw new ProtoException("Internal error; a key mismatch occurred");
                }
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
        private object rootObject;
        internal int AddObjectKey(object value, out bool existing)
        {
            if (value == null) throw new ArgumentNullException("value");

            if ((object)value == (object)rootObject) // (object) here is no-op, but should be
            {                                        // preserved even if this was typed - needs ref-check
                existing = true;
                return Root;
            }
            
            string s;
            BasicList list = List;
            int index = ((s = value as string) == null) ? list.IndexOfReference(value) : list.IndexOf(new StringMatch(s));
          
            if (!(existing = index >= 0))
            {
                index = list.Add(value);
            }
            return index + 1;
        }

        internal void ProposeRoot(object value)
        {
            if (rootObject == null) rootObject = value;
        }
    }
}
