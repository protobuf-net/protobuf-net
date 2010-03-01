#if !NO_RUNTIME
using System;
using System.Collections;

namespace ProtoBuf.Meta
{

    internal sealed class MutableList : BasicList
    {
        /*  Like BasicList, but allows existing values to be changed
         */ 
        public new object this[int index] {
            get { return head[index]; }
            set { head[index] = value; }
        }
    }
    internal class BasicList : IEnumerable
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
        protected Node head = nil;
        public int Add(object value)
        {
            return (head = head.Append(value)).Length - 1;
        }
        public object this[int index] { get { return head[index]; } }
        public object TryGet(int index)
        {
            return head.TryGet(index);
        }
        public void Trim() { head = head.Trim(); }
        public int Count { get { return head.Length; } }
        public IEnumerator GetEnumerator() { return new NodeEnumerator(head); }

        private sealed class NodeEnumerator : IEnumerator
        {
            private int position = -1;
            private readonly Node node;
            public NodeEnumerator(Node node)
            {
                this.node = node;
            }
            void IEnumerator.Reset() { position = -1; }
            public object Current { get { return node[position]; } }
            public bool MoveNext()
            {
                return (position <= node.Length) && (++position < node.Length);
            }
        }
        protected sealed class Node
        {
            public object this[int index]
            {
                get {
                    if (index >= 0 && index < Length)
                    {
                        return data[index];
                    }
                    throw new ArgumentOutOfRangeException("index");
                }
                set
                {
                    if (index >= 0 && index < Length)
                    {
                        data[index] = value;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("index");
                    }
                }
            }
            public object TryGet(int index)
            {
                return (index >= 0 && index < Length) ? data[index] : null;
            }
            private readonly object[] data;
            public readonly int Length;
            internal Node(object[] data, int length)
            {
                Helpers.DebugAssert((data == null && length == 0) ||
                    (data != null && length > 0 && length <= data.Length));
                this.data = data;

                this.Length = length;
            }
            public Node Append(object value)
            {
                object[] newData;
                int newLength = Length + 1;
                if (Length == 0)
                {
                    newData = new object[10];
                }
                else if (Length == data.Length)
                {
                    newData = new object[data.Length * 2];
                    Array.Copy(data, newData, Length);
                } else
                {
                    newData = data;
                }
                newData[Length] = value;
                return new Node(newData, newLength);
            }
            public Node Trim()
            {
                if (Length == 0 || Length == data.Length) return this;
                object[] newData = new object[Length];
                Array.Copy(data, newData, Length);
                return new Node(newData, Length);
            }

            internal int IndexOf(IPredicate predicate)
            {
                for (int i = 0; i < Length; i++)
                {
                    if (predicate.IsMatch(data[i])) return i;
                }
                return -1;
            }
        }

        internal int IndexOf(IPredicate predicate)
        {
            return head.IndexOf(predicate);
        }

        internal interface IPredicate
        {
            bool IsMatch(object obj);
        }
    }
}
#endif