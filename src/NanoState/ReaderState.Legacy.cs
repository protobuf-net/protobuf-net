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
    /// type (state) - the shift/mask the raw path never does. Also pays the group-sentinel check
    /// on every field, which the raw path keeps in the switch default: the stateful API cannot
    /// know its caller's constants, so end-of-group must look like end-of-message here.
    /// </summary>
    public int ReadFieldHeader()
    {
        var tag = ReadRawTag();
        if (tag == 0 || IsScopeEnd(tag))
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
    /// Legacy sub-item entry: framing selected by the wire type in state - length prefix or group
    /// - and the token is the prior scope, because SubItemToken and ReadScope are literally the
    /// same encoding: a sign-discriminated long, negative for groups, prior-limit otherwise.
    /// Reaching the internal SubItemToken constructor is what the IVT grant exists for.
    /// </summary>
    public SubItemToken StartSubItem()
    {
        var scope = _wireType switch
        {
            WireType.String => PushLengthPrefix(),
            WireType.StartGroup => PushGroup((uint)((_fieldNumber << 3) | 4)),
            _ => ThrowWireType<ReadScope>(),
        };
        return new SubItemToken(scope.Value);
    }

    /// <summary>Legacy sub-item exit: the token is the prior scope; restore it.</summary>
    public void EndSubItem(SubItemToken token)
        => PopScope(new ReadScope(token.value64));

    /// <summary>Legacy skip: reconstructs the tag from state - the join the raw path never splits.</summary>
    public void SkipField()
        => SkipTag((uint)((_fieldNumber << 3) | (int)_wireType));

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
