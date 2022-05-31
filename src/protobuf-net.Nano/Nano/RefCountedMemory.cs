using ProtoBuf.Nano.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProtoBuf.Nano;

/// <summary>
/// Utility methods for interacting with ref-counted memory
/// </summary>
public static class RefCountedMemory
{
    /// <summary>
    /// Rent a right-sized chunk of ref-counted memory
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Memory<T> Rent<T>(int length)
        => RefCountedSlabAllocator<T>.Rent(length);

    /// <summary>
    /// If the supplied memory is ref-counted: decrement the counter
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Release<T>(Memory<T> value)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedMemoryManager<T>>(value, out var manager))
        {
            manager.Release();
        }
    }

    /// <summary>
    /// If the supplied memory is ref-counted: decrement the counter
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Release<T>(ReadOnlyMemory<T> value)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedMemoryManager<T>>(value, out var manager))
        {
            manager.Release();
        }
    }

    /// <summary>
    /// Dispose all items in the buffer, and try to release the buffer itself
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReleaseAll<T>(this ReadOnlyMemory<T> value) where T : struct, IDisposable
    {
        foreach (ref readonly var item in value.Span)
        {
            item.Dispose();
        }
        Release(value);
    }

    /// <summary>
    /// If the supplied memory is ref-counted: increment the counter
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TryPreserve<T>(Memory<T> value)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedMemoryManager<T>>(value, out var manager))
        {
            manager.Preserve();
        }
    }

    /// <summary>
    /// If the supplied memory is ref-counted: increment the counter
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TryPreserve<T>(ReadOnlyMemory<T> value)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedMemoryManager<T>>(value, out var manager))
        {
            manager.Preserve();
        }
    }

    /// <summary>
    /// If the supplied memory is ref-counted: query the current counter
    /// </summary>
    /// <returns>The current count if the memory is ref-counted; <c>-1</c> otherwise</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetRefCount<T>(Memory<T> value)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedMemoryManager<T>>(value, out var manager))
        {
            return manager.RefCount;
        }
        return -1;
    }

    /// <summary>
    /// If the supplied memory is ref-counted: query the current counter
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetRefCount<T>(ReadOnlyMemory<T> value)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedMemoryManager<T>>(value, out var manager))
        {
            return manager.RefCount;
        }
        return -1;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void TryRecover<T>(Memory<T> value, int count)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedSlabAllocator<T>.PerThreadSlab>(value, out var manager, out var start, out var length))
        {
            manager.TryRecoverForCurrentThread(start, length, count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void TryRecover<T>(ReadOnlyMemory<T> value, int count)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedSlabAllocator<T>.PerThreadSlab>(value, out var manager, out var start, out var length))
        {
            manager.TryRecoverForCurrentThread(start, length, count);
        }
    }
}
