using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ProtoBuf.Internal
{
    internal sealed class BasicList : IEnumerable
    {
        /* Requirements:
         *   - Fast access by index
         *   - Immutable in the tail, so a node can be read (iterated) without locking
         *   - Lock-free tail handling must match the memory mode; struct for Node
         *     wouldn't work as "read" would not be atomic
         *   - Only operation required is append, but this shouldn't go out of its
         *     way to be inefficient
         *   - Assume that the caller is handling thread-safety (to co-ordinate with
         *     other code); no attempt to be thread-safe
         *   - Assume that the data is private; internal data structure is allowed to
         *     be mutable (i.e. array is fine as long as we don't screw it up)
         */
        private static readonly Node nil = new Node(null, 0);

        private Node head = nil;

        public int Add(object value)
        {
            return (head = head.Append(value)).Length - 1;
        }

        public object this[int index] => head[index];

        //public object TryGet(int index)
        //{
        //    return head.TryGet(index);
        //}

        public int Count => head.Length;

        IEnumerator IEnumerable.GetEnumerator() => new NodeEnumerator(head);

        public NodeEnumerator GetEnumerator() => new NodeEnumerator(head);

        [StructLayout(LayoutKind.Auto)]
        public struct NodeEnumerator : IEnumerator
        {
            private int position;
            private readonly Node node;
            internal NodeEnumerator(Node node)
            {
                this.position = -1;
                this.node = node;
            }
            void IEnumerator.Reset() { position = -1; }
            public readonly object Current { get { return node[position]; } }
            public bool MoveNext()
            {
                int len = node.Length;
                return (position <= len) && (++position < len);
            }
        }
        [StructLayout(LayoutKind.Auto)]
        internal readonly struct Node
        {
            public object this[int index]
            {
                get
                {
                    if (index >= 0 && index < Length)
                    {
                        return data[index];
                    }
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                set
                {
                    if (index >= 0 && index < Length)
                    {
                        data[index] = value;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                }
            }
            //public object TryGet(int index)
            //{
            //    return (index >= 0 && index < length) ? data[index] : null;
            //}
            private readonly object[] data;

            public int Length { get; }

            internal Node(object[] data, int length)
            {
                Debug.Assert((data is null && length == 0) ||
                    (data is not null && length > 0 && length <= data.Length));
                this.data = data;

                this.Length = length;
            }

            public Node Append(object value)
            {
                object[] newData;
                int newLength = Length + 1;
                if (data is null)
                {
                    newData = new object[10];
                }
                else if (Length == data.Length)
                {
                    newData = new object[data.Length * 2];
                    Array.Copy(data, newData, Length);
                }
                else
                {
                    newData = data;
                }
                newData[Length] = value;
                return new Node(newData, newLength);
            }

            internal int IndexOfReference(object instance)
            {
                for (int i = 0; i < Length; i++)
                {
                    if ((object)instance == (object)data[i]) return i;
                } // ^^^ (object) above should be preserved, even if this was typed; needs
                  // to be a reference check
                return -1;
            }

            internal int IndexOf(MatchPredicate predicate, object ctx)
            {
                for (int i = 0; i < Length; i++)
                {
                    if (predicate(data[i], ctx)) return i;
                }
                return -1;
            }
        }

        internal int IndexOf(MatchPredicate predicate, object ctx)
        {
            return head.IndexOf(predicate, ctx);
        }

        internal delegate bool MatchPredicate(object value, object ctx);

        internal bool Contains(object value)
        {
            foreach (object obj in this)
            {
                if (object.Equals(obj, value)) return true;
            }
            return false;
        }

        [StructLayout(LayoutKind.Auto)]
        internal readonly struct Group<T>
        {
            public readonly int First;
            public readonly List<T> Items;
            public bool IsEmpty => Items is null;
            public Group(int first)
            {
                this.First = first;
                this.Items = new List<T>();
            }
        }

        internal static List<Group<T>> GetContiguousGroups<T>(int[] keys, T[] values)
        {
            if (keys is null) throw new ArgumentNullException(nameof(keys));
            if (values is null) throw new ArgumentNullException(nameof(values));
            if (values.Length < keys.Length) throw new ArgumentException("Not all keys are covered by values", nameof(values));
            var outer = new List<Group<T>>();
            Group<T> group = default;
            for (int i = 0; i < keys.Length; i++)
            {
                if (i == 0 || keys[i] != keys[i - 1]) { group = default; }
                if (group.IsEmpty)
                {
                    group = new Group<T>(keys[i]);
                    outer.Add(group);
                }
                group.Items.Add(values[i]);
            }
            return outer;
        }

        internal bool Any() => Count != 0;
    }
}