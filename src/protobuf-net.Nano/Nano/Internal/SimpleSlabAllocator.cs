using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Nano.Internal;

internal static class SimpleSlabAllocator<T>
{
    [ThreadStatic]
    private static int s_remaining;

    // measures show: on .NET Core etc, Slice is 2x faster than new (.45 vs .26 ns)
    // on netfx, new is faster (.45 vs .68 ns)
#if NETCOREAPP3_1_OR_GREATER
    [ThreadStatic]
    private static Memory<T> s_memory;
#else
    [ThreadStatic]
    private static T[]? s_array;
#endif

    private readonly static int SlabSize = (512 * 1024) / Unsafe.SizeOf<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Memory<T> Rent(int length)
    {
        if (length > 0 && length <= s_remaining)
        {
#if NETCOREAPP3_1_OR_GREATER
            var mem = s_memory.Slice(SlabSize - s_remaining, length);
#else
            var mem = new Memory<T>(s_array, SlabSize - s_remaining, length);
#endif
            s_remaining -= length;
            return mem;
        }
        return RentSlow(length);
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

        s_remaining = SlabSize - length;
#if NET5_0_OR_GREATER
        s_memory = GC.AllocateUninitializedArray<T>(SlabSize);
        return s_memory.Slice(0, length);
#elif NETCOREAPP3_1_OR_GREATER
        s_memory = new T[SlabSize];
        return s_memory.Slice(0, length);
#else
        s_array = new T[SlabSize];
        return new Memory<T>(s_array, 0, length);
#endif

        static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(length));
    }
}
