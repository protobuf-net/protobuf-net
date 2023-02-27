﻿using ProtoBuf.Nano.Internal;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if NETCOREAPP3_1_OR_GREATER
using System.Runtime.Intrinsics.X86;
#endif

namespace ProtoBuf.Nano;

/// <summary>
/// Raw API for formatting protobuf data; THIS IS FOR POC ONLY; real impl is Writer
/// </summary>
public ref struct PrepWriter
{
#if USE_SPAN_BUFFER
    private Memory<byte> _bufferMemory;
    private Span<byte> _buffer;
#else
    private byte[] _buffer;
#endif

    int _index, _end;
#if !USE_SPAN_BUFFER
    int _start;
#endif
    private long _positionBase;
    private object _state;

    /// <summary>
    /// Gets the position of the writer
    /// </summary>
    public long Position => _positionBase + _index;

    /// <summary>
    /// Create a new writer instance backed by a buffer-writer
    /// </summary>
    public PrepWriter(IBufferWriter<byte> target)
    {
        _state = target;
        _positionBase = 0;

        const int BUFFER_SIZE = 300000;
#if USE_SPAN_BUFFER
        _bufferMemory = target.GetMemory(BUFFER_SIZE);
        _buffer = _bufferMemory.Span;
        _index = 0;
        _end = _buffer.Length;
#else
        var memory = target.GetMemory(BUFFER_SIZE);
        if (MemoryMarshal.TryGetArray<byte>(memory, out var segment))
        {
            _buffer = segment.Array!;
            _start = _index = segment.Offset;
            _end = segment.Offset + segment.Count;
        }
        else
        {
            throw new InvalidOperationException();
        }
#endif
    }

    private void Flush()
    {
        if (_state is IBufferWriter<byte> bw)
        {
#if USE_SPAN_BUFFER
            bw.Advance(_index);
            _end = _index = 0;
#else
            bw.Advance(_index - _start);
            _end = _index = _start = 0;
#endif
        }
    }

    /// <summary>
    /// Returns the length, in bytes, of the supplied value as a 32-bit unsigned varint
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint MeasureVarint32(uint value)
    {
#if NETCOREAPP3_1_OR_GREATER
        if (Lzcnt.IsSupported)
        {
            var bits = 32 - Lzcnt.LeadingZeroCount(value);
            return bits == 0 ? 1 : ((bits + 6) / 7);
        }
        else
#endif
        {
            if ((value & (~0U << 7)) == 0) return 1;
            if ((value & (~0U << 14)) == 0) return 2;
            if ((value & (~0U << 21)) == 0) return 3;
            if ((value & (~0U << 28)) == 0) return 4;
            return 5;
        }
    }

    /// <summary>
    /// Flush and release all resources associated with this instance
    /// </summary>
    public void Dispose() => Flush();

    /// <summary>
    /// Returns the length, in bytes, of the supplied value as a 64-bit unsigned varint
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint MeasureVarint64(ulong value)
    {
#if NETCOREAPP3_1_OR_GREATER
        if (Lzcnt.X64.IsSupported)
        {
            var bits = 64 - (uint)Lzcnt.X64.LeadingZeroCount(value);
            return bits == 0 ? 1 : ((bits + 6) / 7);
        }
        else
#endif
        {
            if ((value & (~0UL << 7)) == 0) return 1;
            if ((value & (~0UL << 14)) == 0) return 2;
            if ((value & (~0UL << 21)) == 0) return 3;
            if ((value & (~0UL << 28)) == 0) return 4;
            if ((value & (~0UL << 35)) == 0) return 5;
            if ((value & (~0UL << 42)) == 0) return 6;
            if ((value & (~0UL << 49)) == 0) return 7;
            if ((value & (~0UL << 56)) == 0) return 8;
            if ((value & (~0UL << 63)) == 0) return 9;
            return 10;
        }
    }

    /// <summary>
    /// Returns the length, in bytes, of a payload including the length prefix
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong MeasureWithLengthPrefix(ulong bytes) => MeasureVarint64(bytes) + bytes;

    /// <summary>
    /// Returns the length, in bytes, of a payload including the length prefix
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong MeasureWithLengthPrefix(uint bytes) => MeasureVarint64(bytes) + (ulong)bytes;


    /// <summary>
    /// Returns the length, in bytes, of a payload including the length prefix
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong MeasureWithLengthPrefix(string value)
        => MeasureWithLengthPrefix((uint)PrepReader.UTF8.GetByteCount(value));

    /// <summary>
    /// Returns the length, in bytes, of a payload including the length prefix
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong MeasureWithLengthPrefix(ReadOnlyMemory<char> value)
        => MeasureWithLengthPrefix((uint)PrepReader.UTF8.GetByteCount(value.Span));

    /// <summary>
    /// Returns the length, in bytes, of a payload including the length prefix
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong MeasureWithLengthPrefix(ReadOnlyMemory<byte> value)
        => MeasureWithLengthPrefix((uint)value.Length);


    /// <summary>Convert a signed integer to a zig-zag unsigned integer</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Zig(int value) => (uint)((value << 1) ^ (value >> 31));

    /// <summary>Convert a signed integer to a zig-zag unsigned integer</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Zig(long value) => (ulong)((value << 1) ^ (value >> 63));

    /// <summary>
    /// Write an unsigned integer with varint encoding
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarint(byte value)
    {
        if (_index < _end & (value & 0x80) == 0)
        {
            _buffer[_index++] = value;
        }
        else
        {
            WriteVarint((uint)value);
        }
    }

    /// <summary>
    /// Writes a new tag (field-header)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarint(bool value)
    {
        if (_index < _end)
        {
            _buffer[_index++] = value ? (byte)1 : (byte)0;
        }
        else
        {
            WriteVarint((uint)(value ? (byte)1 : (byte)0));
        }
    }

    /// <summary>
    /// Write an unsigned integer with varint encoding
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarint(uint value)
    {
        if (_index + 5 <= _end)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (Lzcnt.IsSupported)
            {
                var bits = 32 - Lzcnt.LeadingZeroCount(value);
                const uint HI_BIT = 0b10000000;

                switch ((bits + 6) / 7)
                {
                    case 0:
                    case 1:
                        Debug.Assert(MeasureVarint32(value) == 1);
                        _buffer[_index++] = (byte)value;
                        return;
                    case 2:
                        Debug.Assert(MeasureVarint32(value) == 2);
                        _buffer[_index++] = (byte)(value | HI_BIT);
                        _buffer[_index++] = (byte)(value >> 7);
                        return;
                    case 3:
                        Debug.Assert(MeasureVarint32(value) == 3);
                        _buffer[_index++] = (byte)(value | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                        _buffer[_index++] = (byte)(value >> 14);
                        return;
                    case 4:
                        Debug.Assert(MeasureVarint32(value) == 4);
                        _buffer[_index++] = (byte)(value | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 14) | HI_BIT);
                        _buffer[_index++] = (byte)(value >> 21);
                        return;
                    default:
                        Debug.Assert(MeasureVarint32(value) == 5);
                        _buffer[_index++] = (byte)(value | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 14) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 21) | HI_BIT);
                        _buffer[_index++] = (byte)(value >> 28);
                        return;
                }
            }
            else
#endif
            {
                while ((value & ~0x7FU) != 0)
                {
                    _buffer[_index++] = (byte)((value & 0x7F) | 0x80);
                    value >>= 7;
                }
                _buffer[_index++] = (byte)value;
            }
        }
        else
        {
            WriteVarintUInt32Slow(value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WriteVarintUInt32Slow(uint value)
        => throw new NotImplementedException();

    /// <summary>
    /// Writes a payload including length prefix
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteWithLengthPrefix(string value)
        => WriteWithLengthPrefix(value.AsSpan());

    /// <summary>
    /// Writes a payload including length prefix
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteWithLengthPrefix(ReadOnlyMemory<char> value)
        => WriteWithLengthPrefix(value.Span);

    /// <summary>
    /// Writes a payload including length prefix
    /// </summary>
    private void WriteWithLengthPrefix(ReadOnlySpan<char> value)
    {
        var bytes = PrepReader.UTF8.GetByteCount(value);
        WriteVarint((uint)bytes);
        if (_index + bytes <= _end)
        {
#if USE_SPAN_BUFFER
            var actualBytes = PrepReader.UTF8.GetBytes(value, _buffer.Slice(_index));
#else
            var actualBytes = PrepReader.UTF8.GetBytes(value, new Span<byte>(_buffer, _index, bytes));
#endif
            Debug.Assert(actualBytes == bytes);
            _index += bytes;
        }
        else
        {
            WriteStringBytesSlow(value);
        }
    }
    private void WriteStringBytesSlow(ReadOnlySpan<char> value)
        => throw new NotImplementedException();

    /// <summary>
    /// Write an unsigned integer with varint encoding
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarint(ulong value)
    {
        if ((value >> 32) == 0) WriteVarint((uint)value);
        else WriteVarintUInt64Full(value);
    }
    private void WriteVarintUInt64Full(ulong value)
    {
        if (_index + 5 <= _end)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (Lzcnt.X64.IsSupported)
            {
                var bits = 64 - Lzcnt.X64.LeadingZeroCount(value);
                const uint HI_BIT = 0b10000000;

                switch ((bits + 6) / 7)
                {
                    case 0:
                    case 1:
                        _buffer[_index++] = (byte)value;
                        return;
                    case 2:
                        _buffer[_index++] = (byte)(value | HI_BIT);
                        _buffer[_index++] = (byte)(value >> 7);
                        return;
                    case 3:
                        _buffer[_index++] = (byte)(value | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                        _buffer[_index++] = (byte)(value >> 14);
                        return;
                    case 4:
                        _buffer[_index++] = (byte)(value | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 14) | HI_BIT);
                        _buffer[_index++] = (byte)(value >> 21);
                        return;
                    case 5:
                        _buffer[_index++] = (byte)(value | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 14) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 21) | HI_BIT);
                        _buffer[_index++] = (byte)(value >> 28);
                        return;
                    case 6:
                        _buffer[_index++] = (byte)(value | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 14) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 21) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 28) | HI_BIT);
                        _buffer[_index++] = (byte)(value >> 35);
                        return;
                    case 7:
                        _buffer[_index++] = (byte)(value | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 14) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 21) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 28) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 35) | HI_BIT);
                        _buffer[_index++] = (byte)(value >> 42);
                        return;
                    case 8:
                        _buffer[_index++] = (byte)(value | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 14) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 21) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 28) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 35) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 42) | HI_BIT);
                        _buffer[_index++] = (byte)(value >> 49);
                        return;
                    case 9:
                        _buffer[_index++] = (byte)(value | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 14) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 21) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 28) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 35) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 42) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 49) | HI_BIT);
                        _buffer[_index++] = (byte)(value >> 56);
                        return;
                    default:
                        _buffer[_index++] = (byte)(value | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 14) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 21) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 28) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 35) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 42) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 49) | HI_BIT);
                        _buffer[_index++] = (byte)((value >> 56) | HI_BIT);
                        _buffer[_index++] = (byte)(value >> 63);
                        return;
                }
            }
            else
#endif
            {
                while ((value & ~0x7FUL) != 0)
                {
                    _buffer[_index++] = (byte)((value & 0x7F) | 0x80);
                    value >>= 7;
                }
                _buffer[_index++] = (byte)value;
            }
        }
        else
        {
            WriteVarintUInt64Slow(value);
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WriteVarintUInt64Slow(ulong value) => throw new NotImplementedException();

    /// <summary>
    /// Writes a payload including length prefix
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteWithLengthPrefix(ReadOnlyMemory<byte> value)
        => WriteWithLengthPrefix(value.Span);

    /// <summary>
    /// Writes a payload including length prefix
    /// </summary>
    private void WriteWithLengthPrefix(ReadOnlySpan<byte> value)
    {
        var bytes = value.Length;
        WriteVarint((uint)bytes);
        if (_index + bytes <= _end)
        {
#if USE_SPAN_BUFFER
            value.CopyTo(_buffer.Slice(_index));
#else
            value.CopyTo(new Span<byte>(_buffer, _index, bytes));
#endif
            _index += bytes;
        }
        else
        {
            WriteBytesBytesSlow(value);
        }
    }
    private void WriteBytesBytesSlow(ReadOnlySpan<byte> value)
        => throw new NotImplementedException();

    /// <summary>
    /// Writes a 32-bit floating-point value
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSingle(float value)
    {
        if (BitConverter.IsLittleEndian && _index + 4 <= _end)
        {
            Unsafe.WriteUnaligned<float>(ref _buffer[_index], value);
            _index += 4;
        }
        else
        {
            WriteSingleSlow(value);
        }
    }
    private void WriteSingleSlow(float value) => throw new NotImplementedException();
}

