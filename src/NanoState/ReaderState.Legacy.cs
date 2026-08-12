using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Nano;

// The compatibility floor, implemented as veneers over the raw surface - the stateful API paying
// for its own state, exactly as docs/nano-core.md prescribes. These are the members that graduate
// from the generated shape file as they arrive here.
public ref partial struct ReaderState
{
    /// <summary>
    /// Legacy header read: the raw tag, decomposed into the field number (returned) and the wire
    /// type (state) - the shift/mask the raw path never does.
    /// </summary>
    public int ReadFieldHeader()
    {
        var tag = ReadRawTag();
        if (tag == 0)
        {
            _fieldNumber = 0;
            _wireType = WireType.None;
            return 0;
        }
        _fieldNumber = (int)(tag >> 3);
        _wireType = (WireType)(tag & 7);
        return _fieldNumber;
    }

    /// <summary>
    /// Legacy typed read: consults the wire type from state - including the zigzag hint, which is
    /// exactly the runtime dispatch the raw API turns into a compile-time decision.
    /// </summary>
    public int ReadInt32()
        => _wireType switch
        {
            WireType.Varint => unchecked((int)ReadRawVarint64()), // legacy: 64-tolerant, truncated
            WireType.SignedVarint => Zag(ReadRawVarint64()),
            WireType.Fixed32 => unchecked((int)ReadRawFixed32()),
            WireType.Fixed64 => checked((int)unchecked((long)ReadRawFixed64())),
            _ => ThrowWireType<int>(),
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Zag(ulong value)
    {
        var v = unchecked((long)value);
        return unchecked((int)((-(v & 1)) ^ ((v >> 1) & ~(1L << 63))));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private T ThrowWireType<T>()
        => throw new InvalidOperationException($"invalid wire-type {_wireType} for this read");

    /// <summary>Reads 4 bytes little-endian.</summary>
    public uint ReadRawFixed32()
    {
        if (_count - _offset < 4) ThrowEndOfData();
        var value = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref _segment, _offset));
        _offset += 4;
        return value; // little-endian assumed; see docs/nano-core.md
    }

    /// <summary>Reads 8 bytes little-endian.</summary>
    public ulong ReadRawFixed64()
    {
        if (_count - _offset < 8) ThrowEndOfData();
        var value = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref _segment, _offset));
        _offset += 8;
        return value;
    }
}
