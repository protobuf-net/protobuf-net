using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProtoBuf.Nano.Internal;

/// <summary>
/// A custom memory-manager that implements ref-counting
/// </summary>
internal abstract class RefCountedMemoryManager<T> : MemoryManager<T>
{
    private int _refCount = 1;

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing) Release();
    }

    /// <summary>
    /// Gets the counter value associated with this instance
    /// </summary>
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
    internal void Release()
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

    /// <summary>
    /// Operation to perform when the counter becomes zero
    /// </summary>
    protected abstract void OnRelease();
}