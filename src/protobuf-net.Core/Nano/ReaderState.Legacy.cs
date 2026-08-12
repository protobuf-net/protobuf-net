using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Nano;

// The compatibility floor, implemented as veneers over the raw surface - the stateful API paying
// for its own state, exactly as docs/nano-core.md prescribes. These are the members that graduate
// from the generated shape file as they arrive here.
internal ref partial struct ReaderState
{
    /// <summary>
    /// Legacy header read: the raw tag, decomposed into the field number (returned) and the wire
    /// type (state) - the shift/mask the raw path never does. Also pays the group-sentinel check
    /// on every field, which the raw path keeps in the switch default: the stateful API cannot
    /// know its caller's constants, so end-of-group must look like end-of-message here. Drains
    /// the pending slot first - a TryReadFieldHeader miss that could not restore hands its
    /// already-decoded tag here.
    /// </summary>
    public int ReadFieldHeader()
    {
        var tag = _pendingTag;
        if (tag != 0) _pendingTag = 0;
        else tag = ReadRawTag();
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
    /// Legacy sub-item entry: reassembles the tag from state and defers the framing decision to
    /// <see cref="PushScope"/> - the token is the prior scope, because SubItemToken and ReadScope
    /// are literally the same encoding: a sign-discriminated long, negative for groups,
    /// prior-limit otherwise. Reaching the internal SubItemToken constructor is what the IVT
    /// grant exists for.
    /// </summary>
    public SubItemToken StartSubItem()
    {
        var scope = PushScope((uint)((_fieldNumber << 3) | ((int)_wireType & 7)));
        return new SubItemToken(scope.Value);
    }

    /// <summary>Legacy sub-item exit: the token is the prior scope; restore it.</summary>
    public void EndSubItem(SubItemToken token)
        => PopScope(new ReadScope(token.value64));

    /// <summary>
    /// Legacy look-ahead: consume the next header only if it is <paramref name="field"/> (any wire
    /// type except end-group) - the repeated-field run loop of legacy generated code. The raw path
    /// has no equivalent member by design (the tag-local loop condition does this job with no
    /// re-decode). Forward-only, unconditionally: the tag is decoded once and a miss parks it in
    /// the pending slot for the next header read. A save/restore-on-miss arm was built and then
    /// removed (Marc's observation): once the pending slot exists - and it must, because nothing
    /// can rewind a Stream or a walked-past sequence segment - restoring merely guarantees the
    /// same bytes get parsed twice, for the identical store count on the miss path.
    /// </summary>
    public bool TryReadFieldHeader(int field)
    {
        uint tag = _pendingTag;
        if (tag != 0) // a prior miss already decoded the next tag
        {
            if ((int)(tag >> 3) == field && (tag & 7) != 4)
            {
                _pendingTag = 0;
                _fieldNumber = field;
                _wireType = (WireType)(tag & 7);
                return true;
            }
            return false; // stays pending
        }
        tag = ReadRawTag();
        if (tag != 0 && (int)(tag >> 3) == field && (tag & 7) != 4)
        {
            _fieldNumber = field;
            _wireType = (WireType)(tag & 7);
            return true;
        }
        _pendingTag = tag; // 0 at a length-scope end = slot stays empty, correctly
        return false;
    }

    /// <summary>Legacy skip: reconstructs the tag from state - the join the raw path never splits.</summary>
    public void SkipField()
        => SkipTag((uint)((_fieldNumber << 3) | (int)_wireType));

    /// <summary>Legacy string read: wire type from state, then the raw read.</summary>
    public string ReadString()
    {
        if (_wireType != WireType.String) ThrowWireType<string>();
        return ReadRawString();
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

    /// <summary>Reads 4 bytes little-endian. The big-endian branch folds away at JIT time on
    /// every little-endian platform (IsLittleEndian is a JIT constant), so correctness on BE
    /// (.NET on s390x exists) costs nothing - and legacy is BE-correct via BinaryPrimitives, so
    /// anything less would be a platform regression.</summary>
    public uint ReadRawFixed32()
    {
        if (_count - _offset < 4) return ReadRawFixed32Straddle();
        var value = Unsafe.ReadUnaligned<uint>(ref At(_offset));
        if (!BitConverter.IsLittleEndian) value = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(value);
        _offset += 4;
        return value;
    }

    // byte-wise LE assembly: endian-free by construction, and crosses refills like every straddle
    [MethodImpl(MethodImplOptions.NoInlining)]
    private uint ReadRawFixed32Straddle()
        => ReadRawByte()
        | ((uint)ReadRawByte() << 8)
        | ((uint)ReadRawByte() << 16)
        | ((uint)ReadRawByte() << 24);

    /// <summary>Reads 8 bytes little-endian; same folded BE handling as <see cref="ReadRawFixed32"/>.</summary>
    public ulong ReadRawFixed64()
    {
        if (_count - _offset < 8) return ReadRawFixed64Straddle();
        var value = Unsafe.ReadUnaligned<ulong>(ref At(_offset));
        if (!BitConverter.IsLittleEndian) value = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(value);
        _offset += 8;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ulong ReadRawFixed64Straddle()
        => ReadRawFixed32Straddle() | ((ulong)ReadRawFixed32Straddle() << 32);
}
