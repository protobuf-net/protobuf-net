using System;
using System.Collections;
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
        //class StringMatch : BasicList.IPredicate
        //{
        //    private readonly string value;
        //    public StringMatch(string value) {
        //        this.value = value;
        //    }
        //    public bool IsMatch(object obj)
        //    {
        //        string s;
        //        return (s = obj as string) != null && s == value;
        //    }
        //}
        private object rootObject;
        internal int AddObjectKey(object value, out bool existing)
        {
            if (value == null) throw new ArgumentNullException("value");

            if ((object)value == (object)rootObject) // (object) here is no-op, but should be
            {                                        // preserved even if this was typed - needs ref-check
                existing = true;
                return Root;
            }

            string s = value as string;
            BasicList list = List;
            int index;
            
#if NO_GENERICS
            //index = s == null ? list.IndexOfReference(value) : list.IndexOf(new StringMatch(s));
            if(s == null)
            {
                if (objectKeys == null)
                {
                    objectKeys = new ReferenceHashtable();
                    index = -1;
                }
                else
                {
                    object tmp = objectKeys[value];
                    index = tmp == null ? -1 : (int) tmp;
                }
            }
            else
            {
                if (stringKeys == null)
                {
                    stringKeys = new Hashtable();
                    index = -1;
                }
                else
                {
                    object tmp = stringKeys[s];
                    index = tmp == null ? -1 : (int) tmp;
                }
            }
#else

            if(s == null)
            {
                if (objectKeys == null) 
                {
                    objectKeys = new System.Collections.Generic.Dictionary<object, int>(ReferenceComparer.Default);
                    index = -1;
                }
                else
                {
                    if (!objectKeys.TryGetValue(value, out index)) index = -1;
                }
            }
            else
            {
                if (stringKeys == null)
                {
                    stringKeys = new System.Collections.Generic.Dictionary<string, int>();
                    index = -1;
                } 
                else
                {
                    if (!stringKeys.TryGetValue(s, out index)) index = -1;
                }
            }
#endif

            if (!(existing = index >= 0))
            {
                index = list.Add(value);

                if (s == null)
                {
                    objectKeys.Add(value, index);
                }
                else
                {
                    stringKeys.Add(s, index);
                }
            }
            return index + 1;
        }

        internal void ProposeRoot(object value)
        {
            if (rootObject == null) rootObject = value;
        }
#if NO_GENERICS
        private ReferenceHashtable objectKeys;
        private System.Collections.Hashtable stringKeys;
        private class ReferenceHashtable : System.Collections.Hashtable
        {
            protected override int GetHash(object key)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(key);
            }
            protected override bool KeyEquals(object item, object key)
            {
                return item == key;
            }
        }   
#else
        private System.Collections.Generic.Dictionary<object, int> objectKeys;
        private System.Collections.Generic.Dictionary<string, int> stringKeys;
        private sealed class ReferenceComparer : System.Collections.Generic.IEqualityComparer<object>
        {
            public readonly static ReferenceComparer Default = new ReferenceComparer();
            private ReferenceComparer() {}

            bool System.Collections.Generic.IEqualityComparer<object>.Equals(object x, object y)
            {
                return x == y; // ref equality
            }

            int System.Collections.Generic.IEqualityComparer<object>.GetHashCode(object obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }

#endif
    }
}
