using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ProtoBuf.Internal;

namespace ProtoBuf.Api;

public ref struct Reader
{
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
    long Position => _positionBase + _index;
    long _objectEnd;
    internal static readonly UTF8Encoding UTF8 = new(false);

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
    public ReadOnlyMemory<byte> ReadBytes(ReadOnlyMemory<byte> value)
    {
        var bytes = ReadLengthPrefix();
        if (bytes == 0) return Empty(value);

        Memory<byte> mutable;
        if (bytes <= value.Length && RefCountedMemory.GetRefCount(value) == 1)
        {
            mutable = MemoryMarshal.AsMemory(value.Slice(0, bytes));
        }
        else
        {
            RefCountedMemory.TryRelease(value);
            mutable = SlabAllocator<byte>.Rent(bytes);
        }

        if (_index + bytes <= _end)
        {
#if USE_SPAN_BUFFER
            _buffer.Slice(_index, bytes).CopyTo(mutable.Span);
#else
            new Span<byte>(_buffer, _index, bytes).CopyTo(mutable.Span);
#endif
            _index += bytes;
        }
        else
        {
            ReadBytesSlow(mutable);
        }
        return mutable;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ReadBytesSlow(Memory<byte> value) => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadLengthPrefix()
    {
        var len = ReadVarintInt32();
        if (len < 0) ThrowNegative();
        return len;

        static void ThrowNegative() => throw new InvalidOperationException("Negative length");
    }
    private static ReadOnlyMemory<T> Empty<T>(ReadOnlyMemory<T> value)
    {
        RefCountedMemory.TryRelease(value);
        return default;
    }
    public ReadOnlyMemory<char> ReadString(ReadOnlyMemory<char> value)
    {
        var bytes = ReadLengthPrefix();
        if (bytes == 0) return Empty(value);

        if (_index + bytes <= _end)
        {
#if USE_SPAN_BUFFER
            var source = _buffer.Slice(_index, bytes);
            var expectedChars = UTF8.GetCharCount(source);
#else
            var expectedChars = UTF8.GetCharCount(_buffer, _index, bytes);
#endif

            Memory<char> mutable;
            if (expectedChars <= value.Length && RefCountedMemory.GetRefCount(value) == 1)
            {
                mutable = MemoryMarshal.AsMemory(value.Slice(0, expectedChars));
            }
            else
            {
                RefCountedMemory.TryRelease(value);
                mutable = SlabAllocator<char>.Rent(expectedChars);
            }
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
            return ReadStringSlow(bytes, value);
        }
    }
    private ReadOnlyMemory<char> ReadStringSlow(int bytes, ReadOnlyMemory<char> value)
    {
        throw new NotImplementedException();
    }

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


    //public ReadOnlyMemory<T> AppendLengthPrefixed<T>(ReadOnlyMemory<T> itemRequests, MessageReader<T> reader, uint tag, int sizeHint)
    //{
    //    Memory<T> target = SlabAllocator<T>.Expand(itemRequests, sizeHint);
    //    int count = itemRequests.Length;

    //    var oldEnd = _objectEnd;
    //    var targetSpan = target.Span;
    //    do
    //    {
    //        var subItemLength = ReadLengthPrefix();
    //        _objectEnd = Position + subItemLength;
    //        if (count == targetSpan.Length)
    //        {
    //            target = SlabAllocator<T>.Expand(target, sizeHint); ;
    //            targetSpan = target.Span;
    //        }
    //        targetSpan[count++] = reader(ref this);
    //        _objectEnd = oldEnd;
    //    } while (TryReadTag(tag));

    //    Debug.Assert(oldEnd >= Position);

    //    target.TryRecover(count);
    //    return target.Slice(0, count);
    //}

    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public unsafe ReadOnlyMemory<T> UnsafeAppendLengthPrefixed<T>(ReadOnlyMemory<T> itemRequests, delegate*<ref Reader, T> reader, uint tag, int sizeHint)
    {
        Memory<T> target = SlabAllocator<T>.Expand(itemRequests, sizeHint);
        int count = itemRequests.Length;

        var oldEnd = _objectEnd;
        var targetSpan = target.Span;
        do
        {
            var subItemLength = ReadLengthPrefix();
            _objectEnd = Position + subItemLength;
            if (count == targetSpan.Length)
            {
                target = SlabAllocator<T>.Expand(target, sizeHint); ;
                targetSpan = target.Span;
            }
            targetSpan[count++] = reader(ref this);
            _objectEnd = oldEnd;
        } while (TryReadTag(tag));

        Debug.Assert(oldEnd >= Position);

        RefCountedMemory.TryRecover(target, count);
        return target.Slice(0, count);
    }

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

    internal ulong ReadVarintUInt64()
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
    internal uint ReadVarintUInt32()
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
    internal int ReadVarintInt32() => (int)ReadVarintUInt32();

    internal long ReadVarintInt64() => (long)ReadVarintUInt64();
}

