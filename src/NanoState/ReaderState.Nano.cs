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

    /// <summary>Start of the current segment; may point mid-array (a sequence segment).</summary>
    private ref byte _segment;

    /// <summary>The backing for <see cref="_segment"/> - the user's array or a leased one.</summary>
    private byte[] _buffer;

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
    /// The end-group sentinel for the innermost group scope, 0 when in length/EOF mode. A state
    /// slot rather than a parameter, because the ISerializer&lt;T&gt;.Read signature is immovable -
    /// and since the slot must exist for that path, it is the ONLY mechanism (direct calls use it
    /// too; one approach, and a slightly smaller frame). Routine fields never read it: the check
    /// lives in the switch default case.
    /// </summary>
    private uint _stopTag;

    /// <summary>Absolute position of the reader.</summary>
    public long Position => _positionBase + _offset;

    // ---------------------------------------------------------------- construction

    /// <summary>Single array: the trivial single-segment case; nothing leased, no source.</summary>
    public ReaderState(byte[] buffer, int offset, int count)
    {
        _buffer = buffer;
        _segment = ref MemoryMarshal.GetArrayDataReference(buffer);
        _segment = ref Unsafe.Add(ref _segment, offset);
        _offset = 0;
        _count = count;
        _effectiveEnd = count;
        _leased = false;
        _positionBase = 0;
        _remaining = 0;
        _source = null;
    }

    /// <summary>Memory: used in place when array-backed, else leased-and-copied once.</summary>
    public ReaderState(ReadOnlyMemory<byte> value)
    {
        if (MemoryMarshal.TryGetArray(value, out var array))
        {
            _buffer = array.Array!;
            _segment = ref MemoryMarshal.GetArrayDataReference(_buffer);
            _segment = ref Unsafe.Add(ref _segment, array.Offset);
            _leased = false;
        }
        else
        {
            _buffer = ArrayPool<byte>.Shared.Rent(value.Length);
            value.Span.CopyTo(_buffer);
            _segment = ref MemoryMarshal.GetArrayDataReference(_buffer);
            _leased = true;
        }
        _offset = 0;
        _count = value.Length;
        _effectiveEnd = value.Length;
        _positionBase = 0;
        _remaining = 0;
        _source = null;
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
        _segment = ref Unsafe.NullRef<byte>();
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
        => throw new NotImplementedException();

    /// <summary>
    /// Enters a group scope: sets the end-group sentinel (checked in the switch default case -
    /// matched fields never test it) and leaves the enclosing length limit in force. A wiretype-4
    /// tag that is not the current sentinel reaches <see cref="SkipTag"/>, which throws: the
    /// mismatched-end-group check falls out free.
    /// </summary>
    public ReadScope PushGroup(uint endGroupTag)
        => throw new NotImplementedException();

    /// <summary>Restores the enclosing scope captured by a push.</summary>
    public void PopScope(in ReadScope prior)
        => throw new NotImplementedException();

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
    /// shift-and-mask veneer over this.
    /// </summary>
    public uint ReadRawTag()
        => throw new NotImplementedException();

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
    /// </summary>
    public void SkipTag(uint tag)
        => throw new NotImplementedException();
}

/// <summary>
/// The prior termination scope - the innermost length limit and group sentinel together - held in
/// a generated-code local across a dive and restored on the way out.
/// </summary>
public readonly struct ReadScope
{
    // prior absolute limit + prior stopTag; shape only for now
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
