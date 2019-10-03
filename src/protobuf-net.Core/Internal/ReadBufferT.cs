using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProtoBuf.Internal
{

    // kinda like List<T>, but with some array-pool love
    internal sealed class ReadBuffer<T> : IDisposable, ICollection<T>
    {
        public void Clear() => _count = 0;
        bool ICollection<T>.IsReadOnly => false;
        public void CopyTo(T[] array, int arrayIndex = 0)
            => Array.Copy(_arr, 0, array, arrayIndex, _count);

        public T[] ToArray()
        {
            if (_count == 0) return Array.Empty<T>();
            var arr = new T[_count];
            CopyTo(arr);
            return arr;
        }

        bool ICollection<T>.Contains(T item)
            => Array.IndexOf(_arr, item, 0, _count) >= 0;
        bool ICollection<T>.Remove(T item)
        {
            int index = Array.IndexOf(_arr, item, 0, _count);
            if (index < 0) return false;

            _count--;
            Array.Copy(_arr, index + 1, _arr, index, _count - index);
            return true;
        }
        public IEnumerator<T> GetEnumerator() => _arr.Take(_count).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        private T[] _arr;
        private int _count;

        public ReadBuffer(int minimumLength = 16)
        {
            _arr = ArrayPool<T>.Shared.Rent(minimumLength);
            _count = 0;
        }

        private static void Recyle(ref T[] array)
        {
            if (array != null)
            {
#if PLAT_ISREF
                        bool clearArray = System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>();
#else
                bool clearArray = !typeof(T).IsValueType; // yes, this could still leave refs in the buffer; tough
#endif
                ArrayPool<T>.Shared.Return(array, clearArray);
                array = null;
            }
        }

        public T[] GetBuffer() => _arr;
        public int Count => _count;

        public void Dispose() => Recyle(ref _arr);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Grow()
        {
            // double the capacity, taking into account max possible length
            var oldCapacity = (uint)_arr.Length;
            var newCapacity = Math.Min(oldCapacity * 2, 0X7FEFFFFF);
            if (oldCapacity == newCapacity) ThrowHelper.ThrowInvalidOperationException("maximum array size exceeded");

            var newArr = ArrayPool<T>.Shared.Rent((int)newCapacity);
            Array.Copy(_arr, 0, newArr, 0, _arr.Length);
            Recyle(ref _arr);
            _arr = newArr;
        }

        [MethodImpl(ProtoReader.HotPath)]
        public void Add(T value)
        {
            int index = _count++;
            if (index == _arr.Length) Grow();
            _arr[index] = value;
        }

    }
}
