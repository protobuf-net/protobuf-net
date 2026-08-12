using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProtoBuf.Nano;

// The NEW surface - hand-written, beside the generated compatibility floor. The existing API is
// reimplemented over these primitives; the generator emits against them directly.
public ref partial struct ReaderState
{
    // ---------------------------------------------------------------- state
    //
    // Two real scenarios: ReadOnlySequence<byte> and Stream; a plain array/Memory is the
    // single-segment special case of the former. Everything is normalized to "a byte[] segment,
    // maybe leased": a Stream reads into one pooled buffer reused for its lifetime; a sequence
    // segment is used in place when TryGetArray says yes, else leased-and-copied during fetch.
    // That normalization is what lets the per-TFM Current accessor be uniform - ref byte via a
    // C# 11 ref field here; arr[index] behind the same inlined accessor down-level.

#if NET7_0_OR_GREATER
    /// <summary>Start of the current segment; may point mid-array (a sequence segment).</summary>
    private ref byte _segment;
#else
    /// <summary>Start of the current segment as an index into <see cref="_buffer"/> - no ref
    /// fields down-level, so the root is arr[index] behind the same inlined accessor.</summary>
    private int _segmentStart;
#endif

    /// <summary>The backing for the segment - the user's array or a leased one.</summary>
    private byte[] _buffer;

    /// <summary>
    /// The per-TFM accessor: the ONE place the layouts differ. net7+ applies the offset to a ref
    /// field (no bounds checks); down-level indexes the array (bounds-checked, slower - the
    /// down-level path pays, and modern TFMs are the optimization target).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref byte At(int offset)
    {
#if NET7_0_OR_GREATER
        return ref Unsafe.Add(ref _segment, offset);
#else
        return ref _buffer[_segmentStart + offset];
#endif
    }

    /// <summary>Position within the current segment.</summary>
    private int _offset;

    /// <summary>Valid bytes in the current segment.</summary>
    private int _count;

    /// <summary>
    /// The innermost length-prefix boundary, clamped into the current segment (recomputed on
    /// refill): the per-field termination check in ReadRawTag is a single int compare against
    /// this, and the *stack* of enclosing limits lives in generated-code locals via
    /// PushLimit/PopLimit - state holds only the innermost.
    /// </summary>
    private int _effectiveEnd;

    /// <summary><see cref="_buffer"/> came from the pool and is ours to return on Dispose.</summary>
    private bool _leased;

    /// <summary>Absolute position of the segment start - errors and limits are absolute.</summary>
    private long _positionBase;

    /// <summary>Bytes known to remain beyond the current segment; -1 when unknown (Stream).</summary>
    private long _remaining;

    /// <summary>
    /// Where the next segment comes from: a <see cref="Stream"/>, the next
    /// <see cref="ReadOnlySequenceSegment{T}"/>, or null when the current segment is the last.
    /// </summary>
    private object? _source;

    /// <summary>
    /// The innermost termination scope, sign-discriminated as legacy SubItemToken always was:
    /// positive/zero = absolute end position (length mode; long.MaxValue = unbounded), negative =
    /// group mode holding -(long)fieldNumber - the sentinel's wire type is always 4, so the 29-bit
    /// field number is the whole identity and the bottom three bits need no storage. A state slot
    /// rather than a parameter, because the ISerializer&lt;T&gt;.Read signature is immovable - and
    /// since the slot must exist for that path, it is the ONLY mechanism (direct calls use it too;
    /// one approach, one long per dive in the caller). Routine fields never read it: the hot
    /// length check is the derived <see cref="_effectiveEnd"/> compare, and the group check is the
    /// switch default case - (tag &amp; 7) == 4 &amp;&amp; (long)(tag &gt;&gt; 3) == -_scope, the
    /// wiretype test doubling as the mismatched-end-group throw gate.
    ///
    /// Known trade, recorded deliberately: entering a group REPLACES the visible positional bound,
    /// so a malformed stream missing its end-group inside a length-prefixed parent overruns the
    /// parent limit until mismatch/EOF. That is exactly legacy semantics (the legacy reader
    /// unbounds position inside groups too): no regression, unobservable on valid input, and the
    /// match makes the SubItemToken veneer mechanical.
    /// </summary>
    private long _scope;

    // ---------------------------------------------------------------- legacy header state
    //
    // The old API's decomposed header: written by the ReadFieldHeader/Hint/Assert veneers, NEVER
    // by the raw path - raw callers carry the tag in a local and these fields go stale, which is
    // fine because the two APIs do not interleave within one consumer. They cannot be overlapped
    // into a single raw-tag field: Hint stretches the wire type beyond 3 bits (SignedVarint =
    // Varint | (1 << 3) = 8 - the zigzag hint is literally a fourth bit, verified in
    // ProtoReader.Hint, which upgrades the stored value in place when the low 3 bits match), and
    // WireType.None = -1 needs sign on top. Two ints, cold for raw consumers.

    private int _fieldNumber;
    private WireType _wireType; // init to WireType.None (-1) in every constructor

    /// <summary>
    /// Sub-item nesting depth, capped exactly as legacy TypeModel.MaxDepth is (default 512) -
    /// nano's direct child calls recurse per wire nesting level, so without the cap a malicious
    /// deeply-nested payload is a stack overflow. Reference-tracking recursion detection is
    /// deliberately NOT reproduced: the depth cap is the fair trade, decided.
    /// </summary>
    private int _depth;
    private const int MaxDepth = 512; // = TypeModel.DefaultMaxDepth; configurable when this lands in Core

    /// <summary>The field number of the last header read via the legacy API.</summary>
    public int FieldNumber => _fieldNumber;

    /// <summary>The wire type of the last header read via the legacy API, including hints.</summary>
    public WireType WireType => _wireType;

    /// <summary>Absolute position of the reader.</summary>
    public long Position => _positionBase + _offset;

    // ---------------------------------------------------------------- construction

    /// <summary>Single array: the trivial single-segment case; nothing leased, no source.</summary>
    public ReaderState(byte[] buffer, int offset, int count)
    {
        _buffer = buffer;
#if NET7_0_OR_GREATER
        _segment = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), offset);
#else
        _segmentStart = offset;
#endif
        _offset = 0;
        _count = count;
        _effectiveEnd = count;
        _scope = count; // root scope: length mode, ending at the data end
        _leased = false;
        _positionBase = 0;
        _remaining = 0;
        _source = null;
        _wireType = WireType.None;
    }

    /// <summary>Memory: used in place when array-backed, else leased-and-copied once.</summary>
    public ReaderState(ReadOnlyMemory<byte> value)
    {
        if (MemoryMarshal.TryGetArray(value, out var array))
        {
            _buffer = array.Array!;
#if NET7_0_OR_GREATER
            _segment = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_buffer), array.Offset);
#else
            _segmentStart = array.Offset;
#endif
            _leased = false;
        }
        else
        {
            _buffer = ArrayPool<byte>.Shared.Rent(value.Length);
            value.Span.CopyTo(_buffer);
#if NET7_0_OR_GREATER
            _segment = ref MemoryMarshal.GetArrayDataReference(_buffer);
#else
            _segmentStart = 0;
#endif
            _leased = true;
        }
        _offset = 0;
        _count = value.Length;
        _effectiveEnd = value.Length;
        _scope = value.Length; // root scope: length mode, ending at the data end
        _positionBase = 0;
        _remaining = 0;
        _source = null;
        _wireType = WireType.None;
    }

    /// <summary>
    /// Sequence: the first segment loads as for Memory; _source holds the next segment node and
    /// _remaining the known tail, so fetch-next is a linked-list walk with per-segment
    /// TryGetArray-else-lease.
    /// </summary>
    public ReaderState(in ReadOnlySequence<byte> value)
        => throw new NotImplementedException();

    /// <summary>
    /// Stream: lease one buffer, reused for every refill; lengthHint (when the caller knows, e.g.
    /// a length-prefixed network frame) seeds _remaining, else -1.
    /// </summary>
    public ReaderState(Stream source, long lengthHint = -1)
        => throw new NotImplementedException();

    /// <summary>Returns the leased buffer, if any; the reader is dead afterwards.</summary>
    public void Dispose()
    {
        var buffer = _buffer;
        _buffer = null!;
#if NET7_0_OR_GREATER
        _segment = ref Unsafe.NullRef<byte>();
#else
        _segmentStart = 0;
#endif
        if (_leased)
        {
            _leased = false;
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    // ---------------------------------------------------------------- segments

    /// <summary>
    /// Advances to the next segment: for a Stream, shift the unconsumed tail to the front of the
    /// leased buffer and top up; for a sequence, walk to the next node (TryGetArray in place, else
    /// lease+copy - note a lease here must first return any prior lease); false at end of data.
    /// Updates _positionBase/_remaining/_effectiveEnd; recomputing the clamped limit is part of
    /// this, which is what keeps the per-field check an int compare.
    /// </summary>
    private bool GetNextBuffer()
        => throw new NotImplementedException();

    // The fast-path window: unguarded 8-byte loads require _offset + 8 <= _count. The final <=8
    // bytes of ANY segment take the bounds-checked slow path - a sequence may hand us the user's
    // own array (TryGetArray), which we must not overread. A leased buffer could be padded to
    // skip even that, but one rule beats two until a benchmark says otherwise.

    // ---------------------------------------------------------------- termination

    /// <summary>
    /// Enters a length-prefixed scope: sets the limit AND clears the group sentinel - a stale
    /// outer stopTag inside a length-bounded sub-message could false-match a wiretype-4 tag, so
    /// every dive pushes scope, either kind. The prior scope goes into a generated-code local and
    /// comes back via <see cref="PopScope"/>: the nesting stack lives in the callers, state holds
    /// only the innermost.
    /// </summary>
    public ReadScope PushLimit(long length)
    {
        var prior = _scope;
        long end = Position + length;
        // single-segment v1: the whole limit must land inside the data we have
        if (length < 0 || end - _positionBase > _count) ThrowEndOfData();
        if (++_depth >= MaxDepth) ThrowTooDeep();
        _scope = end;
        _effectiveEnd = (int)(end - _positionBase);
        return new ReadScope(prior);
    }

    /// <summary>
    /// Reads the length prefix AND enters its scope, one call: the length is meaningless except
    /// as a limit, so the dive site never needs to see it. This is the form generated code uses
    /// for a length-prefixed sub-message (and the StartSubItem veneer will use it too).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadScope PushLengthPrefix()
        => PushLimit(ReadRawVarint32());

    /// <summary>
    /// Enters a group scope: sets the end-group sentinel (checked in the switch default case via
    /// <see cref="IsScopeEnd"/> - matched fields never test it). Position becomes unbounded, which
    /// is legacy semantics exactly - see the recorded trade on <see cref="_scope"/>.
    /// </summary>
    public ReadScope PushGroup(uint endGroupTag)
    {
        if ((endGroupTag & 7) != 4) ThrowMalformed();
        if (++_depth >= MaxDepth) ThrowTooDeep();
        var prior = _scope;
        _scope = -(long)(endGroupTag >> 3);
        _effectiveEnd = _count;
        return new ReadScope(prior);
    }

    /// <summary>
    /// Restores the enclosing scope captured by a push. A length scope must have been consumed
    /// exactly - a short sub-message is corrupt input, matching legacy's EndSubItem validation;
    /// group scopes were validated by their sentinel (and truncation throws in ReadRawTag).
    /// </summary>
    public void PopScope(in ReadScope prior)
    {
        // consumption check as an int compare: for a length scope, _effectiveEnd IS the scope end
        // clamped into this segment (single-segment v1), so offset-vs-effectiveEnd says exactly
        // what long Position-vs-scope math would, cheaper
        if (_scope >= 0 && _offset != _effectiveEnd) ThrowMalformed(); // not fully consumed
        _depth--;
        var value = prior.Value;
        _scope = value;
        _effectiveEnd = value >= 0
            ? (int)Math.Min(_count, value - _positionBase)
            : _count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void ThrowTooDeep() // graduated: matches the legacy shape's member
        => throw new InvalidOperationException($"maximum depth {MaxDepth} exceeded");

    /// <summary>
    /// Whether <paramref name="tag"/> is the current group's end sentinel - the switch-default
    /// test, so matched fields never pay for it. The tag was already consumed by
    /// <see cref="ReadRawTag"/>; a wiretype-4 tag that fails this test belongs to nobody and
    /// should go to <see cref="SkipTag"/>, which throws.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsScopeEnd(uint tag)
        => (tag & 7) == 4 && (long)(tag >> 3) == -_scope;

    // ---------------------------------------------------------------- snapshot

    /// <summary>
    /// The storable (non-ref-struct) form, for async resume at refill boundaries - the reader
    /// itself stays sync. The ref field needs no slot: the segment-start index is recovered via
    /// Unsafe.ByteOffset against the array root at snapshot time, and the ref is re-derived on
    /// restore.
    /// </summary>
    public ReaderSnapshot Snapshot()
        => throw new NotImplementedException();

    /// <summary>Reconstitutes a reader from a snapshot.</summary>
    public ReaderState(in ReaderSnapshot snapshot)
        => throw new NotImplementedException();

    // ---------------------------------------------------------------- raw reads

    /// <summary>
    /// Reads the next field tag as its raw wire value - the tag varint as-is, field number and
    /// wire type still joined - or 0 at the end of the current message. The length-prefix
    /// termination check lives here (one int compare against the clamped limit); generated code
    /// dispatches on compile-time constants (for example
    /// <c>case (2 &lt;&lt; 3) | (int)WireType.String:</c>), so no decomposition happens and no
    /// state is written; the legacy <c>ReadFieldHeader()</c>/<c>WireType</c> pair becomes a
    /// shift-and-mask veneer over this. Strict-5: tags never take the sign-extended form.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadRawTag()
    {
        var offset = _offset;
        if (offset >= _effectiveEnd) return EndOfScope();
        // the dominant case - fields 1-15 - is a single byte; the range check is one compare,
        // same cost as the bare MSB test, and additionally rejects field 0 (values 0-7): a zero
        // byte where a tag belongs must throw, not read as a false end-of-message
        uint b0 = At(offset);
        if (b0 - 8 <= 119) // 8..127: single-byte tag, field >= 1
        {
            _offset = offset + 1;
            return b0;
        }
        return ReadRawTagTail(b0, offset);
    }

    /// <summary>
    /// The end path, once per message: in group mode, running out of data means the end-group
    /// sentinel never arrived - corrupt input, matching legacy's EndSubItem validation.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private uint EndOfScope()
    {
        if (_scope < 0) ThrowEndOfData(); // truncated group
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private uint ReadRawTagTail(uint value, int offset)
    {
        // values 0-7 land here too (field 0, any wire type): invalid, exactly as legacy's SetTag
        // throws "Invalid field in source data"
        if ((value & 0x80) == 0) ThrowMalformed();
        // continuation beyond byte 0: rarer, and bounds-guarded per byte so the buffer tail
        // needs no special-casing (ByteUnrolled shape - see VarintU32DecodeResults.md). Any
        // multi-byte tag has field >= 16, so no further field-0 check is needed.
        value &= 0x7F;
        int shift = 7;
        for (int i = 1; i < 5; i++)
        {
            if (offset + i >= _count) ThrowEndOfData();
            uint b = At(offset + i);
            value |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                _offset = offset + i + 1;
                return value;
            }
            shift += 7;
        }
        ThrowMalformed();
        return 0;
    }

    /// <summary>
    /// Consumes the next tag only if it is exactly <paramref name="tag"/> - the fields-in-order
    /// fast path: a serializer that just read field n speculates that field n+1 comes next and
    /// skips the dispatch entirely when it is right.
    /// </summary>
    public bool TryReadRawTag(uint tag)
        => throw new NotImplementedException();

    /// <summary>
    /// Skips the field whose raw tag was just read - the untyped counterpart of the legacy
    /// <c>SkipField()</c>, taking the wire type from the tag's low bits rather than from state.
    /// A wiretype-4 tag (end-group) reaching here is by definition not the current sentinel, so
    /// it throws - the mismatched-end-group check, for free.
    /// </summary>
    public void SkipTag(uint tag)
    {
        switch (tag & 7)
        {
            case 0: // varint
                _ = ReadRawVarint64();
                break;
            case 1: // fixed64
                Advance(8);
                break;
            case 2: // length-prefixed
                Advance(checked((int)ReadRawVarint32()));
                break;
            case 5: // fixed32
                Advance(4);
                break;
            case 3: // start-group: v1 does not skip groups yet
                throw new NotImplementedException("group skip");
            default: // 4 = end-group that nothing expected; 6/7 are not wire types
                ThrowMalformed();
                break;
        }
    }

    private void Advance(int bytes)
    {
        if (bytes < 0 || _count - _offset < bytes) ThrowEndOfData();
        _offset += bytes;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowEndOfData() => throw new InvalidOperationException("unexpected end of data");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowMalformed() => throw new InvalidOperationException("malformed data");

    /// <summary>
    /// Reads a varint as u32, tolerant of the 10-byte sign-extended form a negative int32 arrives
    /// in (high garbage discarded) - values are tolerant, tags are strict. ByteUnrolled per the
    /// measured table; the fast path assumes the 10-byte window and the buffer tail falls to the
    /// per-byte guarded slow path.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadRawVarint32()
    {
        var offset = _offset;
        if (_count - offset >= 10)
        {
            ref byte src = ref At(offset);
            uint b0 = src;
            if ((b0 & 0x80) == 0) { _offset = offset + 1; return b0; }
            uint value = b0 & 0x7F;
            uint b = Unsafe.Add(ref src, 1);
            value |= (b & 0x7F) << 7;
            if ((b & 0x80) == 0) { _offset = offset + 2; return value; }
            b = Unsafe.Add(ref src, 2);
            value |= (b & 0x7F) << 14;
            if ((b & 0x80) == 0) { _offset = offset + 3; return value; }
            b = Unsafe.Add(ref src, 3);
            value |= (b & 0x7F) << 21;
            if ((b & 0x80) == 0) { _offset = offset + 4; return value; }
            b = Unsafe.Add(ref src, 4);
            value |= b << 28;
            if ((b & 0x80) == 0) { _offset = offset + 5; return value; }
            return TolerantSpill(value, offset + 5);
        }
        return unchecked((uint)ReadRawVarint64Slow()); // tolerant: high garbage discarded
    }

    /// <summary>
    /// The sign-extension garbage of a 6-10 byte "u32": bytes 5-9 are skipped, the value is the
    /// low 32 bits already accumulated. Rare by construction, hence out of line.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private uint TolerantSpill(uint value, int offset)
    {
        for (int i = 0; i < 5; i++)
        {
            if ((At(offset + i) & 0x80) == 0)
            {
                _offset = offset + i + 1;
                return value;
            }
        }
        ThrowMalformed();
        return 0;
    }

    /// <summary>Reads a varint as u64; ByteUnrolled, same tail discipline as u32.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadRawVarint64()
    {
        var offset = _offset;
        if (_count - offset >= 10)
        {
            ref byte src = ref At(offset);
            uint b0 = src;
            if ((b0 & 0x80) == 0) { _offset = offset + 1; return b0; }
            ulong value = b0 & 0x7Fu;
            int shift = 7, i = 1;
            while (true)
            {
                ulong b = Unsafe.Add(ref src, i);
                value |= (b & 0x7F) << shift;
                if ((b & 0x80) == 0) { _offset = offset + i + 1; return value; }
                if (++i == 10) { ThrowMalformed(); return 0; }
                shift += 7;
            }
        }
        return ReadRawVarint64Slow();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ulong ReadRawVarint64Slow()
    {
        ulong value = 0;
        int shift = 0;
        for (int i = 0; i < 10; i++)
        {
            if (_offset >= _count) ThrowEndOfData();
            ulong b = At(_offset++);
            value |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0) return value;
            shift += 7;
        }
        ThrowMalformed();
        return 0;
    }

    /// <summary>Reads a length-prefixed UTF-8 string.</summary>
    public string ReadRawString()
        => throw new NotImplementedException();
}

/// <summary>
/// The prior termination scope, held in a generated-code local across a dive and restored on the
/// way out. One sign-discriminated long (see ReaderState._scope) - which is also exactly what
/// legacy SubItemToken is, making the StartSubItem/EndSubItem veneer mechanical.
/// </summary>
public readonly struct ReadScope
{
    private readonly long _value;
    internal ReadScope(long value) => _value = value;
    internal long Value => _value;
}

/// <summary>
/// The storable (non-ref-struct) snapshot of a <see cref="ReaderState"/>: the async/resume story
/// (what <c>ProtoReader.SolidState</c> is to the legacy reader). Plain fields only - the ref
/// field is represented as the segment-start index within the buffer.
/// </summary>
public readonly struct ReaderSnapshot
{
    // buffer + segment-start index + offset/count + positionBase + remaining + source + scope;
    // shape only for now - members arrive with the implementation
}
