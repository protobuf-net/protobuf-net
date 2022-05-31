using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Nano.Internal;

internal static class SimpleSlabAllocator<T>
{
    [ThreadStatic]
    private static PerThreadSlab? s_ThreadLocal;

    private readonly static int SlabSize = (512 * 1024) / Unsafe.SizeOf<T>(); // , MaxChunkSize = 64 * 1024;

    internal static Memory<T> Rent(int length)
    {
        if (length == 0) return default;
        var slab = s_ThreadLocal;
        if (length > 0 && slab is not null && slab.TryRent(length, out var value)) return value;
        return RentSlow(length);
    }

    private static Memory<T> RentSlow(int length)
    {
        if (length < 0) ThrowOutOfRange();

        if (length > SlabSize) return new T[length]; // give up for over-sized

        if (!(s_ThreadLocal = new PerThreadSlab()).TryRent(length, out var value)) ThrowUnableToRent();
        return value;


        static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(length));
        static void ThrowUnableToRent() => throw new InvalidOperationException("Unable to allocate from slab!");
    }

    internal sealed class PerThreadSlab : MemoryManager<T>
    {
        private readonly T[] _array;
        private int _remaining;
        private readonly Memory<T> _memory;

        public PerThreadSlab()
        {
             _array = new T[SlabSize];
            _remaining = _array.Length;
            _memory = base.Memory;
        }

        public override Memory<T> Memory => _memory;

        public override Span<T> GetSpan() => _array;

        protected override bool TryGetArray(out ArraySegment<T> segment)
        {
            segment = new ArraySegment<T>(_array);
            return true;
        }

        public override MemoryHandle Pin(int elementIndex = 0)
            => throw new NotImplementedException();
        public override void Unpin()
            => throw new NotImplementedException();

        protected override void Dispose(bool disposing) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRent(int size, out Memory<T> value)
        {
            if (size <= _remaining)
            {
                value = _memory.Slice(_array.Length - _remaining, size);
                _remaining -= size;
                return true;
            }
            value = default;
            return false;
        }
    }
}
