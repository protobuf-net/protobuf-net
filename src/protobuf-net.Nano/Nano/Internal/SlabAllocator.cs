using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProtoBuf.Nano.Internal;

internal static class SlabAllocator<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Memory<T> Expand(ReadOnlyMemory<T> value, int sizeHint)
    {
        int countHint, length;
        if (MemoryMarshal.TryGetMemoryManager<T, SlabAllocator<T>.PerThreadSlab>(value, out var manager, out var start, out length))
        {
            countHint = Math.Max(length, sizeHint); // double, or size hint: whichever is bigger
            if (manager.TryExpandForCurrentThread(start, length, countHint))
            {
                return manager.Memory.Slice(start, length + countHint);
            }
            var newValue = Rent(value.Length + countHint);
            value.CopyTo(newValue);
            manager.Release();
            return newValue;
        }
        else
        {
            length = value.Length;
            countHint = Math.Max(length, sizeHint); // double, or size hint: whichever is bigger
            if (length == 0)
            {
                return Rent(countHint);
            }
            var newValue = Rent(value.Length + countHint);
            value.CopyTo(newValue);
            return newValue;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Memory<T> Rent(int length)
    {
        if (length == 0) return default;
        var slab = s_ThreadLocal;
        if (length > 0 && slab is not null && slab.TryRent(length, out var value)) return value;
        return RentSlow(length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Memory<T> RentSlow(int length)
    {
        if (length < 0) ThrowOutOfRange();

        if (length > SlabSize) return new T[length]; // give up for over-sized

        s_ThreadLocal?.Release();
        if (!(s_ThreadLocal = new PerThreadSlab()).TryRent(length, out var value)) ThrowUnableToRent();
        return value;


        static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(length));
        static void ThrowUnableToRent() => throw new InvalidOperationException("Unable to allocate from slab!");
    }

    [ThreadStatic]
    private static PerThreadSlab? s_ThreadLocal;

    readonly static int SlabSize = (512 * 1024) / Unsafe.SizeOf<T>(); // , MaxChunkSize = 64 * 1024;

    internal sealed class PerThreadSlab : RefCountedMemoryManager<T>
    {
        public override Span<T> GetSpan() => _array;
        protected override bool TryGetArray(out ArraySegment<T> segment)
        {
            segment = new ArraySegment<T>(_array);
            return true;
        }
        public override MemoryHandle Pin(int elementIndex = 0)
            => throw new NotSupportedException(); // can do if needed; I'm just being lazy

        public override void Unpin()
            => throw new NotSupportedException(); // can do if needed; I'm just being lazy

        public PerThreadSlab()
        {
            _array = ArrayPool<T>.Shared.Rent(SlabSize);
#if DEBUG
                Console.Write("+");
#endif
            _remaining = _array.Length;
            _memory = base.Memory; // snapshot the underlying memory value, as this is non-trivial and we use it a lot
        }

        public override Memory<T> Memory => _memory;

        private readonly Memory<T> _memory;
        private readonly T[] _array;
        private int _remaining;

        protected override void OnRelease()
        {
            ArrayPool<T>.Shared.Return(_array);
#if DEBUG
            Console.Write("-");
#endif
        }

      

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRent(int size, out Memory<T> value)
        {
            if (size <= _remaining)
            {
                Preserve();
                value = _memory.Slice(_array.Length - _remaining, size);
                _remaining -= size;
                return true;
            }
            value = default;
            return false;
        }


        internal void TryRecoverForCurrentThread(int start, int length, int count)
        {
            if (count < length && ReferenceEquals(this, s_ThreadLocal))
            {
                var localEnd = _array.Length - _remaining;
                var remoteEnd = start + length;
                if (localEnd == remoteEnd)
                {
                    // then we can claw some back!
                    _remaining += length - count;
                }
            }
        }

        internal bool TryExpandForCurrentThread(int start, int length, int count)
        {
            if (ReferenceEquals(this, s_ThreadLocal) && count <= _remaining)
            {
                var localEnd = _array.Length - _remaining;
                var remoteEnd = start + length;
                if (localEnd == remoteEnd)
                {
                    // then we can claw some back!
                    _remaining -= count;
                    return true;
                }
            }
            return false;
        }
    }
}