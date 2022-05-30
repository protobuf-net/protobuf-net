using ProtoBuf.Nano.Internal;
using System;
using System.Runtime.InteropServices;

namespace ProtoBuf.Nano;

/// <summary>
/// Utility methods for interacting with ref-counted memory
/// </summary>
public static class RefCountedMemory
{
    /// <summary>
    /// If the supplied memory is ref-counted: decrement the counter
    /// </summary>
    /// <returns><c>true</c> if the memory is ref-counted</returns>
    public static bool TryRelease<T>(Memory<T> value)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedMemoryManager<T>>(value, out var manager))
        {
            manager.Release();
            return true;
        }
        return false;
    }

    /// <summary>
    /// If the supplied memory is ref-counted: decrement the counter
    /// </summary>
    /// <returns><c>true</c> if the memory is ref-counted</returns>
    public static bool TryRelease<T>(ReadOnlyMemory<T> value)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedMemoryManager<T>>(value, out var manager))
        {
            manager.Release();
            return true;
        }
        return false;
    }

    /// <summary>
    /// If the supplied memory is ref-counted: increment the counter
    /// </summary>
    /// <returns><c>true</c> if the memory is ref-counted</returns>
    public static bool TryPreserve<T>(Memory<T> value)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedMemoryManager<T>>(value, out var manager))
        {
            manager.Preserve();
            return true;
        }
        return false;
    }

    /// <summary>
    /// If the supplied memory is ref-counted: increment the counter
    /// </summary>
    /// <returns><c>true</c> if the memory is ref-counted</returns>
    public static bool TryPreserve<T>(ReadOnlyMemory<T> value)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedMemoryManager<T>>(value, out var manager))
        {
            manager.Preserve();
            return true;
        }
        return false;
    }

    /// <summary>
    /// If the supplied memory is ref-counted: query the current counter
    /// </summary>
    /// <returns>The current count if the memory is ref-counted; <c>-1</c> otherwise</returns>
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
    /// <returns>The current count if the memory is ref-counted; <c>-1</c> otherwise</returns>
    internal static int GetRefCount<T>(ReadOnlyMemory<T> value)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedMemoryManager<T>>(value, out var manager))
        {
            return manager.RefCount;
        }
        return -1;
    }


    internal static void TryRecover<T>(Memory<T> value, int count)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, SlabAllocator<T>.PerThreadSlab>(value, out var manager, out var start, out var length))
        {
            manager.TryRecoverForCurrentThread(start, length, count);
        }
    }

    internal static void TryRecover<T>(ReadOnlyMemory<T> value, int count)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, SlabAllocator<T>.PerThreadSlab>(value, out var manager, out var start, out var length))
        {
            manager.TryRecoverForCurrentThread(start, length, count);
        }
    }
}
