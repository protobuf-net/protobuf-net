using System;
using System.Collections;
using ProtoBuf.Meta;

namespace ProtoBuf
{
    internal sealed class NetObjectCache
    {
        internal const int Root = 0;
        private IObjectCache underlyingCache;

        private IObjectCache GetCacheForWrite()
        {
            if (underlyingCache == null)
            {
                underlyingCache = new CacheShard();
            }
#if !(CF || PORTABLE)
            else if (underlyingCache.IsFull)
            {
                underlyingCache = new CacheMultiShard((CacheShard)underlyingCache);
            }
#endif
            return underlyingCache;
        }


        internal object GetKeyedObject(int key)
        {
            if (key-- == Root)
            {
                if (rootObject == null) throw new ProtoException("No root object assigned");
                return rootObject;
            }
            IObjectCache cache = underlyingCache;
            object tmp = cache == null ? null : cache[key];
            if(tmp == null) throw new ProtoException("A deferred key does not have a value yet");
            return tmp;
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
                IObjectCache cache = GetCacheForWrite();
                if (key < cache.Count)
                {
                    cache.SetObjectKey(value, key);
                }
                else
                {
                    if(key != cache.Append(value))
                    {
                        throw new ProtoException("Internal error; a key mismatch occurred");
                    }
                }
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

            string s = value as string;
            IObjectCache cache = GetCacheForWrite();

            int key = cache.GetObjectKey(value);
            if (!(existing = key >= 0))
            {
                key = cache.Append(value);
            }
            return key + 1;
        }

        private int trapStartIndex; // defaults to 0 - optimization for RegisterTrappedObject
                                    // to make it faster at seeking to find deferred-objects

        internal void RegisterTrappedObject(object value)
        {
            if (rootObject == null)
            {
                rootObject = value;
            }
            else
            {
                if(underlyingCache != null)
                {
                    for (int i = trapStartIndex; i < underlyingCache.Count; i++)
                    {
                        trapStartIndex = i + 1; // things never *become* null; whether or
                                                // not the next item is null, it will never
                                                // need to be checked again

                        if(underlyingCache[i] == null)
                        {
                            underlyingCache.SetObjectKey(value, i);
                            break;
                        }
                    }
                }
            }
        }

        interface IObjectCache
        {
            bool IsFull { get; }
            int Count { get; }
            int GetObjectKey(object value);
            void SetObjectKey(object value, int key);
            object this[int key] { get; }
            int Append(object obj);
        }
#if !(CF || PORTABLE)
        sealed class CacheMultiShard : IObjectCache
        {
            bool IObjectCache.IsFull { get { return false; } }
            readonly BasicList shards = new BasicList();

            public const int ShardSize = 64 * 1024;
            public CacheMultiShard(CacheShard first)
            {
                if (first == null) throw new ArgumentNullException("first");
                count = first.Count;
                if (count != ShardSize) throw new ArgumentException("First shard must be exactly full", "first");
                shards.Add(first);
            }
            int count;
            int IObjectCache.Count { get { return count; } }
            int IObjectCache.Append(object obj)
            {
                CacheShard shard;
                if (count % ShardSize == 0)
                {
                    // last shard is full
                    shard = new CacheShard();
                    shards.Add(shard);
                }
                else
                {
                    shard = (CacheShard)shards[shards.Count - 1];
                }
                shard.Append(obj, count);
                return count++; // note this is post-additive; if we have 10 items, the index of the new (11th) is 10
            }

            object IObjectCache.this[int key]
            {
                get
                {
                    try
                    {
                        return ((CacheShard)shards[key / ShardSize])[key % ShardSize];
                    }
                    catch (ProtoException) { throw; }
                    catch (Exception ex)
                    {
                        throw new ProtoException("Internal error; a missing key occurred", ex);
                    }
                }
            }
            public int GetObjectKey(object obj)
            {
                int offset = 0;
                foreach (CacheShard shard in shards)
                {
                    int key = shard.GetObjectKey(obj);
                    if (key >= 0) return offset + key;
                    offset += ShardSize;
                }
                return -1;
            }
            void IObjectCache.SetObjectKey(object obj, int key)
            {
                ((CacheShard)shards[key / ShardSize]).SetObjectKey(obj, key % ShardSize, key);
            }
        }
#endif
        sealed class CacheShard : IObjectCache
        {
            bool IObjectCache.IsFull { get {
#if CF || PORTABLE
                return false;
#else
                return list.Count == CacheMultiShard.ShardSize;
#endif
            } }
            public int Count { get { return list.Count; } }
            private readonly MutableList list = new MutableList();
            public object this[int index]
            {
                get
                {
                    try
                    {
                        return list[index];
                    }
                    catch (Exception ex)
                    {
                        throw new ProtoException("Internal error; a missing key occurred", ex);
                    }
                }
            }

            void IObjectCache.SetObjectKey(object obj, int key)
            {
                SetObjectKey(obj, key, key);
            }
            public void SetObjectKey(object obj, int index, int key)
            {
                object oldVal = list[index];
                if (oldVal == null)
                {
                    list[index] = obj;
                    StoreObjectKey(obj, key);
                }
                else if (!ReferenceEquals(oldVal, obj))
                {
                    throw new ProtoException("Reference-tracked objects cannot change reference");
                } // otherwise was already the same; nothing to do
            }

            int IObjectCache.Append(object obj)
            {
                int index = list.Add(obj);
                StoreObjectKey(obj, index);
                return index;
            }
            public int Append(object obj, int key)
            {
                int index = list.Add(obj);
                StoreObjectKey(obj, key);
                return index;
            }


            public int GetObjectKey(object obj)
            {
                string s = obj as string;
#if NO_GENERICS            
                if(s == null)
                {
                    object tmp = objectKeys[value];
                    return tmp == null ? -1 : (int) tmp;
                }
                else
                {
                    object tmp = stringKeys[s];
                    return tmp == null ? -1 : (int) tmp;
                }
#else

                if (s == null)
                {
#if CF || PORTABLE // CF has very limited proper object ref-tracking; so instead, we'll search it the hard way
                    return list.IndexOfReference(obj);
#else
                    int key;
                    return objectKeys.TryGetValue(obj, out key) ? key : -1;
#endif
                }
                else
                {
                    int key;
                    return stringKeys.TryGetValue(s, out key) ? key : -1;
                }
#endif
            }

            private void StoreObjectKey(object obj, int key)
            {
                string s;
                if (obj == null)
                {
                    // do nothing
                }
                else if ((s = obj as string) == null)
                {
#if !CF && !PORTABLE // CF can't handle the object keys very well
                    try
                    {
                        objectKeys.Add(obj, key);
                    }
                    catch (OutOfMemoryException oom)
                    {
                        throw new ProtoException("wtf? " + objectKeys.Count, oom);
                    }
#endif
                }
                else
                {
                    stringKeys.Add(s, key);
                }
            }


#if NO_GENERICS
            private readonly ReferenceHashtable objectKeys = new ReferenceHashtable();
            private readonly System.Collections.Hashtable stringKeys = new System.Collections.Hashtable();
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

            private readonly System.Collections.Generic.Dictionary<string, int> stringKeys = new System.Collections.Generic.Dictionary<string, int>();

#if !CF && !PORTABLE // CF lacks the ability to get a robust reference-based hash-code, so we'll do it the harder way instead
            private readonly System.Collections.Generic.Dictionary<object, int> objectKeys = new System.Collections.Generic.Dictionary<object,int>(ReferenceComparer.Default);

            private sealed class ReferenceComparer : System.Collections.Generic.IEqualityComparer<object>
            {
                public readonly static ReferenceComparer Default = new ReferenceComparer();
                private ReferenceComparer() { }

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
#endif

        }

    }
}
