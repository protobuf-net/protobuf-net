using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ProtoBuf.Nano.Internal;

namespace ProtoBuf.Nano;

/// <summary>
/// Raw API for parsing protobuf data
/// </summary>
public ref struct Reader
{
    /// <summary>
    /// Releases all resources associated with this instance
    /// </summary>
    public void Dispose()
    {
#if USE_SPAN_BUFFER
        var tmp = _bufferMemory;
        _buffer = default;
        _bufferMemory = default;
#else
        var tmp = _buffer;
        _buffer = null!;
        if (_returnToArrayPool)
        {
            _returnToArrayPool = false;
            ArrayPool<byte>.Shared.Return(tmp);
        }
#endif

    }

#if USE_SPAN_BUFFER
    ReadOnlyMemory<byte> _bufferMemory;
    ReadOnlySpan<byte> _buffer;
#else
    byte[] _buffer;
    bool _returnToArrayPool;
#endif
    int _index, _end;
    long _positionBase;
    /// <summary>
    /// Gets the position of the reader
    /// </summary>
    public long Position => _positionBase + _index;
    long _objectEnd;
    internal static readonly UTF8Encoding UTF8 = new(false);

    /// <summary>
    /// Create a new reader instance backed by an array
    /// </summary>
    public Reader(byte[] buffer, int offset, int count)
    {
#if USE_SPAN_BUFFER
        _bufferMemory = buffer;
#else
        _returnToArrayPool = false;
#endif
        _buffer = buffer;
        _index = offset;
        _end = offset + count;
        _positionBase = 0;
        _objectEnd = _end;
    }

    /// <summary>
    /// Create a new reader instance backed by a single memory value
    /// </summary>
    /// <param name="value"></param>
    public Reader(ReadOnlyMemory<byte> value)
    {
#if USE_SPAN_BUFFER
        _bufferMemory = value;
        _buffer = value.Span;
        _index = 0;
        _end = value.Length;
#else
        if (MemoryMarshal.TryGetArray<byte>(value, out var segment))
        {
            _buffer = segment.Array!;
            _index = segment.Offset;
            _end = segment.Offset + segment.Count;
            _returnToArrayPool = false;
        }
        else
        {
            _buffer = ArrayPool<byte>.Shared.Rent(value.Length);
            _index = 0;
            _end = value.Length;
            value.Span.CopyTo(_buffer);
            _returnToArrayPool = true;
        }
#endif
        _positionBase = 0;
        _objectEnd = _end;
    }

    /// <summary>
    /// Reads a length-prefixed chunk of bytes
    /// </summary>
    public byte[] ReadBytes()
    {
        var bytes = ReadLengthPrefix();
        if (bytes == 0) return Array.Empty<byte>();

        if (_index + bytes <= _end)
        {
#if NET5_0_OR_GREATER
            var arr = GC.AllocateUninitializedArray<byte>(bytes);
#else
            var arr = new byte[bytes];
#endif

#if USE_SPAN_BUFFER
            _buffer.Slice(_index, bytes).CopyTo(arr);
#else
            Buffer.BlockCopy(_buffer, _index, arr, 0, bytes);
#endif
            _index += bytes;
            return arr;
        }
        return ReadBytesVectorSlow(bytes);
    }

    private byte[] ReadBytesVectorSlow(int length) => throw new NotImplementedException();

    /// <summary>
    /// Reads a length-prefixed chunk of bytes
    /// </summary>
    public ReadOnlyMemory<byte> ReadSlabBytes()
    {
        var bytes = ReadLengthPrefix();
        if (bytes == 0) return default;

        if (_index + bytes <= _end)
        {
            Memory<byte> mutable = SimpleSlabAllocator<byte>.Rent(bytes);
#if USE_SPAN_BUFFER
            _buffer.Slice(_index, bytes).CopyTo(mutable.Span);
#else
            new Span<byte>(_buffer, _index, bytes).CopyTo(mutable.Span);
#endif
            _index += bytes;
            return mutable;
        }
        else
        {
            return ReadSlabBytesSlow(bytes);
        }
    }

    /// <summary>
    /// Reads a length-prefixed chunk of bytes
    /// </summary>
    /// <remarks>The supplied existing value is discarded (i.e. replace not append), after releasing any backing memory</remarks>
    public ReadOnlyMemory<byte> ReadRefCountedBytes(ReadOnlyMemory<byte> value)
    {
        var bytes = ReadLengthPrefix();
        RefCountedMemory.Release(value);
        if (bytes == 0) return default;

        if (_index + bytes <= _end)
        {
            Memory<byte> mutable = RefCountedSlabAllocator<byte>.Rent(bytes);
#if USE_SPAN_BUFFER
            _buffer.Slice(_index, bytes).CopyTo(mutable.Span);
#else
            new Span<byte>(_buffer, _index, bytes).CopyTo(mutable.Span);
#endif
            _index += bytes;
            return mutable;
        }
        else
        {
            return ReadRefCountedBytesSlow(bytes);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ReadOnlyMemory<byte> ReadSlabBytesSlow(int length) => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ReadOnlyMemory<byte> ReadRefCountedBytesSlow(int length) => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadLengthPrefix()
    {
        int len = ReadVarintInt32();
        if (len < 0) ThrowNegative();
        return len;

        static void ThrowNegative() => throw new InvalidOperationException("Negative length");
    }

    /// <summary>
    /// Reads a length-prefixed chunk of utf-8 encoded characters
    /// </summary>
    public string ReadString()
    {
        var bytes = ReadLengthPrefix();
        if (bytes == 0) return "";

        if (_index + bytes <= _end)
        {
#if USE_SPAN_BUFFER
            var s = UTF8.GetString(_buffer.Slice(_index, bytes));
#else
            var s = UTF8.GetString(_buffer, _index, bytes);
#endif
            _index += bytes;
            return s;
        }
        else
        {
            return ReadStringSlow(bytes);
        }
    }

    /// <summary>
    /// Reads a length-prefixed chunk of utf-8 encoded characters
    /// </summary>
    public ReadOnlyMemory<char> ReadSlabString()
    {
        var bytes = ReadLengthPrefix();
        if (bytes == 0) return default;

        if (_index + bytes <= _end)
        {
#if USE_SPAN_BUFFER
            var source = _buffer.Slice(_index, bytes);
            var expectedChars = UTF8.GetCharCount(source);
#else
            var expectedChars = UTF8.GetCharCount(_buffer, _index, bytes);
#endif

            Memory<char> mutable = SimpleSlabAllocator<char>.Rent(expectedChars);
#if USE_SPAN_BUFFER
            var actualChars = UTF8.GetChars(source, mutable.Span);
#else
            var actualChars = UTF8.GetChars(new ReadOnlySpan<byte>(_buffer, _index, bytes), mutable.Span);
#endif

            Debug.Assert(expectedChars == actualChars);
            _index += bytes;
            return mutable;
        }
        else
        {
            return ReadSlabStringSlow(bytes);
        }
    }

    private ReadOnlyMemory<char> ReadSlabStringSlow(int length) => throw new NotImplementedException();
    private string ReadStringSlow(int length) => throw new NotImplementedException();

    /// <summary>
    /// Reads a length-prefixed chunk of utf-8 encoded characters
    /// </summary>
    /// <remarks>The supplied existing value is discarded (i.e. replace not append), after releasing any backing memory</remarks>
    public ReadOnlyMemory<char> ReadRefCountedString(ReadOnlyMemory<char> value)
    {
        var bytes = ReadLengthPrefix();
        RefCountedMemory.Release(value);
        if (bytes == 0) return default;

        if (_index + bytes <= _end)
        {
#if USE_SPAN_BUFFER
            var source = _buffer.Slice(_index, bytes);
            var expectedChars = UTF8.GetCharCount(source);
#else
            var expectedChars = UTF8.GetCharCount(_buffer, _index, bytes);
#endif
            Memory<char> mutable = RefCountedSlabAllocator<char>.Rent(expectedChars);
#if USE_SPAN_BUFFER
            var actualChars = UTF8.GetChars(source, mutable.Span);
#else
            var actualChars = UTF8.GetChars(new ReadOnlySpan<byte>(_buffer, _index, bytes), mutable.Span);
#endif

            Debug.Assert(expectedChars == actualChars);
            _index += bytes;
            return mutable;
        }
        else
        {
            return ReadRefCountedStringSlow(bytes);
        }
    }
    private ReadOnlyMemory<char> ReadRefCountedStringSlow(int bytes)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Read a raw tag (field header), taking object boundaries into account
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadTag()
    {
        if (Position >= _objectEnd) return 0;
        return ReadVarintUInt32();
    }

    private bool TryReadTag(uint tag)
    {
        if (Position >= _objectEnd) return false;

        if (tag < 128 && _index < _end)
        {
            if (_buffer[_index] == tag)
            {
                _index++;
                return true;
            }
            return false;
        }
        return TryReadTagSlow(tag);
    }
    private bool TryReadTagSlow(uint tag)
    {
        var snapshot = this;
        if (snapshot.ReadTag() == tag)
        {   // confirmed; update state
            this = snapshot;
        }
        return false;
    }


    /// <summary>
    /// Extend an existing collection of sub-items, treating each as length-prefixed
    /// </summary>
    /// <remarks>It is assumed that the first tag has already been consumed; additional items are consumed as long as the next tag encountered is a match</remarks>
    public ReadOnlyMemory<T> AppendLengthPrefixed<T>(ReadOnlyMemory<T> value, INanoSerializer<T> reader, uint tag, int sizeHint)
    {
        Memory<T> target = RefCountedSlabAllocator<T>.Expand(value, sizeHint);
        int count = value.Length;

        var oldEnd = _objectEnd;
        var targetSpan = target.Span;
        do
        {
            var subItemLength = ReadLengthPrefix();
            _objectEnd = Position + subItemLength;
            if (count == targetSpan.Length)
            {
                target = RefCountedSlabAllocator<T>.Expand(target, sizeHint); ;
                targetSpan = target.Span;
            }
            targetSpan[count++] = reader.Read(ref this);
            _objectEnd = oldEnd;
        } while (TryReadTag(tag));

        Debug.Assert(oldEnd >= Position);

        RefCountedMemory.TryRecover(target, count);
        return target.Slice(0, count);
    }

    /// <summary>
    /// Extend an existing collection of sub-items, treating each as length-prefixed
    /// </summary>
    /// <remarks>It is assumed that the first tag has already been consumed; additional items are consumed as long as the next tag encountered is a match</remarks>
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public unsafe ReadOnlyMemory<T> UnsafeAppendLengthPrefixed<T>(ReadOnlyMemory<T> value, delegate*<ref Reader, T> reader, uint tag, int sizeHint)
    {
        Memory<T> target = RefCountedSlabAllocator<T>.Expand(value, sizeHint);
        int count = value.Length;

        var oldEnd = _objectEnd;
        var targetSpan = target.Span;
        do
        {
            var subItemLength = ReadLengthPrefix();
            _objectEnd = Position + subItemLength;
            if (count == targetSpan.Length)
            {
                target = RefCountedSlabAllocator<T>.Expand(target, sizeHint); ;
                targetSpan = target.Span;
            }
            targetSpan[count++] = reader(ref this);
            _objectEnd = oldEnd;
        } while (TryReadTag(tag));

        Debug.Assert(oldEnd >= Position);

        RefCountedMemory.TryRecover(target, count);
        return target.Slice(0, count);
    }

    /// <summary>
    /// Extend an existing collection of sub-items, treating each as length-prefixed
    /// </summary>
    /// <remarks>It is assumed that the first tag has already been consumed; additional items are consumed as long as the next tag encountered is a match</remarks>
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public unsafe void UnsafeAppendLengthPrefixed<T>(List<T> value, delegate*<ref Reader, T> reader, uint tag)
    {
        var oldEnd = _objectEnd;
        do
        {
            var subItemLength = ReadLengthPrefix();
            _objectEnd = Position + subItemLength;
            
            value.Add(reader(ref this));
            _objectEnd = oldEnd;
        } while (TryReadTag(tag));

        Debug.Assert(oldEnd >= Position);
    }

    /// <summary>
    /// Read a 32-bit floating point value value
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadSingle()
    {
        if (BitConverter.IsLittleEndian && _index + 4 <= _end)
        {
#if USE_SPAN_BUFFER
            var value = Unsafe.ReadUnaligned<float>(ref Unsafe.AsRef(in _buffer[_index]));
#else
            var value = Unsafe.ReadUnaligned<float>(ref _buffer[_index]);
#endif
            _index += 4;
            return value;
        }
        return ReadSingleSlow();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private float ReadSingleSlow() => throw new NotImplementedException();

    /// <summary>
    /// Read an unsigned 64-bit varint value
    /// </summary>
    public ulong ReadVarintUInt64()
    {
        if (_index + 10 <= _end)
        {
            var buffer = _buffer;
            ulong result = buffer[_index++];
            if (result < 128)
            {
                return result;
            }
            result &= 0x7f;
            int shift = 7;
            do
            {
                byte b = buffer[_index++];
                result |= (ulong)(b & 0x7F) << shift;
                if (b < 0x80)
                {
                    return result;
                }
                shift += 7;
            }
            while (shift < 64);

            ThrowMalformed();
        }
        return ReadVarintUInt64Slow();
    }

    private ulong ReadVarintUInt64Slow()
    {
        ulong value = 0;
        for (int i = 0; i < 10; i++)
        {
            ulong b = ReadRawByte();
            value |= (b & 0x7F) << (7 * i);
            if ((b & 0x80) == 0) return value;
        }
        return ThrowTooManyBytes();
        static ulong ThrowTooManyBytes() => throw new InvalidOperationException("Too many bytes!");
    }

    /// <summary>
    /// Read an unsigned 32-bit varint value
    /// </summary>
    public uint ReadVarintUInt32()
    {
        if (_index + 5 <= _end)
        {
            var buffer = _buffer;
            int tmp = buffer[_index++];
            if (tmp < 128)
            {
                return (uint)tmp;
            }
            int result = tmp & 0x7f;
            if ((tmp = buffer[_index++]) < 128)
            {
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = buffer[_index++]) < 128)
                {
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = buffer[_index++]) < 128)
                    {
                        result |= tmp << 21;
                    }
                    else
                    {
                        result |= (tmp & 0x7f) << 21;
                        result |= (tmp = buffer[_index++]) << 28;
                        if (tmp >= 128)
                        {
                            // Discard upper 32 bits.
                            // Note that this has to use ReadRawByte() as we only ensure we've
                            // got at least 5 bytes at the start of the method. This lets us
                            // use the fast path in more cases, and we rarely hit this section of code.
                            for (int i = 0; i < 5; i++)
                            {
                                if (ReadRawByte() < 128)
                                {
                                    return (uint)result;
                                }
                            }
                            ThrowMalformed();
                        }
                    }
                }
            }
            return (uint)result;
        }
        return ReadVarintUInt32Slow();
    }

    static void ThrowMalformed() => throw new InvalidOperationException("malformed varint");

    private byte ReadRawByte()
    {
        if (_index < _end)
        {
            return _buffer[_index++];
        }
        return ReadRawByteSlow();
    }
    private byte ReadRawByteSlow() => throw new NotImplementedException();

    private uint ReadVarintUInt32Slow() => throw new NotImplementedException();

    /// <summary>
    /// Read a signed 32-bit varint value
    /// </summary>
    public int ReadVarintInt32() => (int)ReadVarintUInt32();

    /// <summary>
    /// Read an signed 64-bit varint value
    /// </summary>
    public long ReadVarintInt64() => (long)ReadVarintUInt64();
}

