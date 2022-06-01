using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Nano.Internal;

internal static class SimpleSlabAllocator<T>
{
    //[ThreadStatic]
    //private static PerThreadSlab? s_perThreadSlab;

    private readonly static int SlabSize = (512 * 1024) / Unsafe.SizeOf<T>();

    //private sealed class PerThreadSlab
    //{
#if NETCOREAPP3_1_OR_GREATER
        [ThreadStatic]
        private static Memory<T> _memory;
#else
        [ThreadStatic]
        private static T[]? _array;
#endif
    [ThreadStatic]
    private static int _remaining;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Memory<T> Rent(int length)
        {
            if (length > 0 && length <= _remaining)
            {
                // measures show: on .NET Core etc, Slice is 2x faster than new (.45 vs .26 ns)
                // on netfx, new is faster (.45 vs .68 ns)
#if NETCOREAPP3_1_OR_GREATER
                var mem = _memory.Slice(SlabSize - _remaining, length);
#else
                var mem = new Memory<T>(_array, SlabSize - _remaining, length);
#endif
                _remaining -= length;
                return mem;
            }
            return RentSlow(length);
        }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Rent(out Memory<T> value, int length)
    {
        if (length > 0 && length <= _remaining)
        {
            // measures show: on .NET Core etc, Slice is 2x faster than new (.45 vs .26 ns)
            // on netfx, new is faster (.45 vs .68 ns)
#if NETCOREAPP3_1_OR_GREATER
            value = _memory.Slice(SlabSize - _remaining, length);
#else
            value = new Memory<T>(_array, SlabSize - _remaining, length);
#endif
            _remaining -= length;
        }
        else
        {
            RentSlow(out value, length);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
        private static Memory<T> RentSlow(int length)
        {
            if (length == 0) return default;
            if (length < 0) ThrowOutOfRange();

            if (length > SlabSize)
            {   // give up for over-sized
#if NET5_0_OR_GREATER
                return GC.AllocateUninitializedArray<T>(length);
#else
                return new T[length];
#endif
            }
            _remaining = SlabSize - length;
#if NET5_0_OR_GREATER
            _memory = GC.AllocateUninitializedArray<T>(SlabSize);
            return _memory.Slice(0, length);
#elif NETCOREAPP3_1_OR_GREATER
            _memory = new T[SlabSize];
            return _memory.Slice(0, length);
#else
            _array = new T[SlabSize];
            return new Memory<T>(_array, 0, length);
#endif
            static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(length));
        }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RentSlow(out Memory<T> value, int length)
    {
        if (length == 0)
        {
            value = default;
            return;
        }
        if (length < 0) ThrowOutOfRange();

        if (length > SlabSize)
        {   // give up for over-sized
#if NET5_0_OR_GREATER
            value = GC.AllocateUninitializedArray<T>(length);
#else
            value = new T[length];
#endif
        }
        else
        {
            _remaining = SlabSize - length;
#if NET5_0_OR_GREATER
            _memory = GC.AllocateUninitializedArray<T>(SlabSize);
            value = _memory.Slice(0, length);
#elif NETCOREAPP3_1_OR_GREATER
            _memory = new T[SlabSize];
            value = _memory.Slice(0, length);
#else
            _array = new T[SlabSize];
            value = new Memory<T>(_array, 0, length);
#endif
        }
        static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(length));
    }
    //}

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //internal static Memory<T> Rent(int length)
    //{
    //    var obj = s_perThreadSlab;
    //    return obj is not null ? obj.Rent(length) : NewThreadSlabRent(length);
    //}

    //[MethodImpl(MethodImplOptions.NoInlining)]
    //private static Memory<T> NewThreadSlabRent(int length)
    //    => (s_perThreadSlab = new PerThreadSlab()).Rent(length);
}
