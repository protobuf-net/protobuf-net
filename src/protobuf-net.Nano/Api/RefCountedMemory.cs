using System;
using System.Runtime.InteropServices;

namespace ProtoBuf.Api;

public static class RefCountedMemory
{
    public static bool TryRelease<T>(Memory<T> value)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedMemoryManager<T>>(value, out var manager))
        {
            manager.Release();
            return true;
        }
        return false;
    }

    public static bool TryRelease<T>(ReadOnlyMemory<T> value)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedMemoryManager<T>>(value, out var manager))
        {
            manager.Release();
            return true;
        }
        return false;
    }

    public static int GetRefCount<T>(Memory<T> value)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedMemoryManager<T>>(value, out var manager))
        {
            return manager.RefCount;
        }
        return -1;
    }
    public static int GetRefCount<T>(ReadOnlyMemory<T> value)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, RefCountedMemoryManager<T>>(value, out var manager))
        {
            return manager.RefCount;
        }
        return -1;
    }

    public static void TryRecover<T>(Memory<T> value, int count)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, SlabAllocator<T>.PerThreadSlab>(value, out var manager, out var start, out var length))
        {
            manager.TryRecoverForCurrentThread(start, length, count);
        }
    }

    public static void TryRecover<T>(ReadOnlyMemory<T> value, int count)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, SlabAllocator<T>.PerThreadSlab>(value, out var manager, out var start, out var length))
        {
            manager.TryRecoverForCurrentThread(start, length, count);
        }
    }
}
