using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProtoBuf.Api;

public abstract class RefCountedMemoryManager<T> : MemoryManager<T>
{
    private int _refCount = 1;
    protected override void Dispose(bool disposing)
    {
        if (disposing) Release();
    }

    public int RefCount => Volatile.Read(ref _refCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Preserve()
    {
        if (Interlocked.Increment(ref _refCount) <= 1) PreserveFail();
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void PreserveFail()
    {
        Interlocked.Decrement(ref _refCount);
        throw new InvalidOperationException("already dead!");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Release()
    {
        switch (Interlocked.Decrement(ref _refCount))
        {
            case 0:
                OnRelease();
                break;
            case -1:
                Throw();
                break;
                static void Throw() => throw new InvalidOperationException("released too many times!");
        }
    }

    protected abstract void OnRelease();
}