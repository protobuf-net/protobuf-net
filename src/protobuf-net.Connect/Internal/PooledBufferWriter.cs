using System;
using System.Buffers;

namespace ProtoBuf.Connect.Internal;

/// <summary>
/// A minimal <see cref="IBufferWriter{T}"/> over a pooled array.
/// </summary>
/// <remarks>
/// Used where the bytes have to exist before they can be handed over - a request body that must be
/// re-sendable, for instance. Where they do not, both ends write straight to the transport's own
/// writer instead and this is not involved.
/// </remarks>
internal sealed class PooledBufferWriter : IBufferWriter<byte>, IDisposable
{
    private byte[]? _buffer;
    private int _written;

    public PooledBufferWriter(int initialCapacity = 256)
        => _buffer = ArrayPool<byte>.Shared.Rent(Math.Max(initialCapacity, 16));

    public ReadOnlyMemory<byte> WrittenMemory
        => new(_buffer ?? throw new ObjectDisposedException(nameof(PooledBufferWriter)), 0, _written);

    public int WrittenCount => _written;

    public void Advance(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        var buffer = _buffer ?? throw new ObjectDisposedException(nameof(PooledBufferWriter));
        if (_written + count > buffer.Length) throw new InvalidOperationException("Advanced past the end of the buffer.");
        _written += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        Ensure(sizeHint);
        return _buffer.AsMemory(_written);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        Ensure(sizeHint);
        return _buffer.AsSpan(_written);
    }

    private void Ensure(int sizeHint)
    {
        var buffer = _buffer ?? throw new ObjectDisposedException(nameof(PooledBufferWriter));
        if (sizeHint <= 0) sizeHint = 1;
        if (buffer.Length - _written >= sizeHint) return;

        var grown = ArrayPool<byte>.Shared.Rent(Math.Max(buffer.Length * 2, _written + sizeHint));
        Buffer.BlockCopy(buffer, 0, grown, 0, _written);
        _buffer = grown;
        ArrayPool<byte>.Shared.Return(buffer);
    }

    public void Dispose()
    {
        var buffer = _buffer;
        _buffer = null;
        if (buffer is not null) ArrayPool<byte>.Shared.Return(buffer);
    }
}
