using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProtoBuf
{
    partial class ProtoReader
    {

// The NEW surface - hand-written, beside the generated compatibility floor. The existing API is
// reimplemented over these primitives; the generator emits against them directly.
public ref partial struct State
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
    private readonly ref byte At(int offset)
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
    /// Where the next segment comes from: a <see cref="Stream"/>, a boxed
    /// <see cref="ReadOnlySequence{T}"/> (one allocation per multi-segment reader, walked via
    /// <see cref="_nextPosition"/>), or null when the current segment is the last.
    /// </summary>
    private object _source; // Stream, boxed ReadOnlySequence<byte>, or null (resident buffer)

    /// <summary>The walk cursor within a multi-segment sequence.</summary>
    private SequencePosition _nextPosition;

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
    /// A tag decoded by a TryReadFieldHeader miss that could not be un-consumed (0 = none; a real
    /// tag is never 0). The reader is FORWARD-ONLY - nothing can rewind a Stream, and a sequence
    /// walk may have discarded (or un-leased) the segment a saved offset pointed into - so a
    /// speculative decode that was not provably local hands its result to the next header read
    /// instead of pushing bytes back. Veneer state, exactly like the two fields above: written
    /// and drained only by the header veneers, never touched by the raw path (whose callers hold
    /// the tag in a local and hand a miss forward via dispatch - the same rule, one level down).
    /// </summary>
    private uint _pendingTag;

    /// <summary>
    /// Sub-item nesting depth, capped exactly as legacy TypeModel.MaxDepth is (default 512) -
    /// the generated direct child calls recurse per wire nesting level, so without the cap a malicious
    /// deeply-nested payload is a stack overflow. Reference-tracking recursion detection is
    /// deliberately NOT reproduced: the depth cap is the fair trade, decided.
    /// </summary>
    private int _depth;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IncrDepthExceeded()
        => ++_depth >= (_model is null ? global::ProtoBuf.Meta.TypeModel.DefaultMaxDepth : _model.MaxDepth);

    // ---------------------------------------------------------------- state extras (the swap)
    //
    // What the class reader used to hold: the model, user state, the lazy serialization-context
    // shim, and the lazy string interner. Class-typed or value fields only; State travels by
    // ref, so mutations flow, and struct copies share the class-typed instances.

    internal global::ProtoBuf.Meta.TypeModel _model;
    internal ISerializationContext _contextShim;
    internal object _userState;
    internal bool _internStrings;
    private System.Collections.Generic.Dictionary<string, string> _stringInterner;

    internal string Intern(string value)
    {
        if (value is null) return null;
        if (value.Length == 0) return "";
        if (_stringInterner is null)
        {
            _stringInterner = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal) { { value, value } };
        }
        else if (_stringInterner.TryGetValue(value, out var found))
        {
            value = found;
        }
        else
        {
            _stringInterner.Add(value, value);
        }
        return value;
    }

    /// <summary>Fills the span exactly, crossing refills - the Span counterpart of FillFrom.</summary>
    internal void ReadRawBytesInto(Span<byte> destination)
    {
        while (!destination.IsEmpty)
        {
            if (_offset >= _count && !GetNextBuffer()) ThrowEndOfData();
            int take = Math.Min(destination.Length, _count - _offset);
#if NET7_0_OR_GREATER
            MemoryMarshal.CreateReadOnlySpan(ref At(_offset), take).CopyTo(destination);
#else
            new ReadOnlySpan<byte>(_buffer, _segmentStart + _offset, take).CopyTo(destination);
#endif
            _offset += take;
            destination = destination.Slice(take);
        }
    }

    /// <summary>Absolute position of the reader.</summary>
    public readonly long Position => _positionBase + _offset;

    // ---------------------------------------------------------------- construction

    /// <summary>Single array: the trivial single-segment case; nothing leased, no source.</summary>
    public State(byte[] buffer, int offset, int count)
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
    public State(ReadOnlyMemory<byte> value)
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
    /// Sequence: a single-segment sequence collapses to the Memory case; otherwise the boxed
    /// sequence (one allocation) is walked via a SequencePosition cursor, per-window
    /// TryGetArray-else-lease. The root scope is the sequence's known length.
    /// </summary>
    public State(scoped in ReadOnlySequence<byte> value)
    {
        if (value.IsSingleSegment)
        {
            this = new State(value.First);
            return;
        }
        _buffer = [];
        _offset = 0;
        _count = 0;
        _effectiveEnd = 0;
        _leased = false;
        _positionBase = 0;
        _remaining = value.Length;
        _scope = value.Length; // root scope: length mode over the whole sequence
        _source = value; // boxed: the one allocation this reader makes
        _nextPosition = value.Start;
        _wireType = WireType.None;
        AdvanceSequence(); // load the first non-empty window
    }

    /// <summary>
    /// Stream: one leased buffer for the reader's lifetime, shift-and-top-up on refill. A
    /// seekable exact-type MemoryStream with an exposable buffer collapses to the single-segment
    /// case - PRODUCT PARITY, not an invention: legacy does the same unwrap (including reaching
    /// the private buffer by reflection, which the spike skips; see ProtoReader.Stream.cs), so
    /// benchmarks must deliberately defeat it to measure streaming at all. lengthHint (when the
    /// caller knows - a length-prefixed network frame) seeds _remaining and bounds the root
    /// scope; without it the root is unbounded and EOF is the clean end of the document.
    /// </summary>
    public State(Stream source, long lengthHint = -1)
    {
        if (source is MemoryStream ms && ms.GetType() == typeof(MemoryStream) && ms.CanSeek
            && ms.TryGetBuffer(out var segment))
        {
            int position = checked((int)ms.Position);
            this = new State(segment.Array!, segment.Offset + position, segment.Count - position);
            return;
        }
        _buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
#if NET7_0_OR_GREATER
        _segment = ref MemoryMarshal.GetArrayDataReference(_buffer);
#else
        _segmentStart = 0;
#endif
        _offset = 0;
        _count = 0;
        _effectiveEnd = 0;
        _leased = true;
        _positionBase = 0;
        _remaining = lengthHint;
        _scope = lengthHint >= 0 ? lengthHint : long.MaxValue; // unbounded root without a hint
        _source = source;
        _wireType = WireType.None;
        FillFromStream(source); // initial fill
    }

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
    /// Advances to the next window: for a Stream, shift the unconsumed tail to the front of the
    /// leased buffer and top up; for a sequence, walk to the next node (TryGetArray in place,
    /// else lease+copy, returning any prior lease first); false at end of data. The contract
    /// (docs/nano-core.md): owes callers NOTHING before the current offset, and never needs to -
    /// every straddle is byte-wise consumption, so there is no partial-primitive state to carry.
    /// Recomputing the clamped limit is part of this, which is what keeps the per-field check an
    /// int compare.
    /// </summary>
    private bool GetNextBuffer()
        => _source switch
        {
            Stream stream => FillFromStream(stream),
            ReadOnlySequence<byte> => AdvanceSequence(),
            _ => false,
        };

    private bool FillFromStream(Stream stream)
    {
        // shift the unconsumed tail to the front (stream windows always start at array index 0)
        int tail = _count - _offset;
        if (tail > 0 && _offset != 0)
        {
            Buffer.BlockCopy(_buffer, _offset, _buffer, 0, tail);
        }
        _positionBase += _offset;
        _offset = 0;
        _count = tail;
        // top up as far as the buffer and any length hint allow: maximizing residency is the
        // refill's legal courtesy, since resident is the common case the bulk arms exploit
        int added = 0;
        while (true)
        {
            int space = _buffer.Length - _count;
            if (_remaining >= 0) space = (int)Math.Min(space, _remaining);
            if (space <= 0) break;
            int got = stream.Read(_buffer, _count, space);
            if (got <= 0) break;
            _count += got;
            added += got;
            if (_remaining >= 0) _remaining -= got;
        }
        RecomputeEffectiveEnd();
        return added > 0;
    }

    private bool AdvanceSequence()
    {
        // only ever called with the current window exhausted (byte-wise consumption): the prior
        // window contributes exactly _count to the absolute position
        var seq = (ReadOnlySequence<byte>)_source!;
        while (seq.TryGet(ref _nextPosition, out var mem, advance: true))
        {
            if (mem.IsEmpty) continue;
            _positionBase += _count;
            var old = _buffer;
            var wasLeased = _leased;
            if (MemoryMarshal.TryGetArray(mem, out var segment))
            {
                _buffer = segment.Array!;
#if NET7_0_OR_GREATER
                _segment = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_buffer), segment.Offset);
#else
                _segmentStart = segment.Offset;
#endif
                _leased = false;
            }
            else
            {
                var rented = ArrayPool<byte>.Shared.Rent(mem.Length);
                mem.Span.CopyTo(rented);
                _buffer = rented;
#if NET7_0_OR_GREATER
                _segment = ref MemoryMarshal.GetArrayDataReference(rented);
#else
                _segmentStart = 0;
#endif
                _leased = true;
            }
            if (wasLeased) ArrayPool<byte>.Shared.Return(old);
            _offset = 0;
            _count = mem.Length;
            if (_remaining >= 0) _remaining -= mem.Length;
            RecomputeEffectiveEnd();
            return true;
        }
        return false;
    }

    private void RecomputeEffectiveEnd()
        => _effectiveEnd = _scope >= 0
            ? (int)Math.Max(0, Math.Min(_count, _scope - _positionBase))
            : _count;

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
        // plausibility where totals are known (Memory/sequence/hinted stream); a bare stream
        // cannot verify up front and is validated by consumption/EOF instead
        if (length < 0 || (_remaining >= 0 && end - _positionBase > (long)_count + _remaining)) ThrowEndOfData();
        if (IncrDepthExceeded()) ThrowTooDeep();
        _scope = end;
        _effectiveEnd = (int)Math.Max(0, Math.Min(_count, end - _positionBase));
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
    /// Enters the scope a message-field tag implies: length mode for wire-type 2, group mode for
    /// wire-type 3 - the framing decision is already in the tag, so a dive site accepting both
    /// framings (as legacy always has, without prejudice) needs no branch of its own beyond its
    /// two case labels. The end-group sentinel is the start tag plus one: only the low 3 bits
    /// differ (3 becomes 4).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadScope PushScope(uint tag)
        => (tag & 7) switch
        {
            2 => PushLengthPrefix(),
            3 => PushGroup(tag + 1),
            _ => ThrowNotAScope(tag),
        };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ReadScope ThrowNotAScope(uint tag)
        => throw new InvalidOperationException($"tag {tag} (wire-type {tag & 7}) does not open a sub-message scope");

    /// <summary>
    /// Enters a group scope: sets the end-group sentinel (checked in the switch default case via
    /// <see cref="IsScopeEnd"/> - matched fields never test it). Position becomes unbounded, which
    /// is legacy semantics exactly - see the recorded trade on <see cref="_scope"/>.
    /// </summary>
    public ReadScope PushGroup(uint endGroupTag)
    {
        if ((endGroupTag & 7) != 4) ThrowMalformed();
        if (IncrDepthExceeded()) ThrowTooDeep();
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
        // exact-consumption check: Position-based, so it holds across refills (a length scope
        // may span many windows)
        if (_scope >= 0 && _positionBase + _offset != _scope) ThrowMalformed(); // not fully consumed
        _depth--;
        var value = prior.Value;
        _scope = value;
        RecomputeEffectiveEnd();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void ThrowTooDeep() // graduated: matches the legacy shape's member
        => throw new InvalidOperationException("Maximum model depth exceeded (see " + nameof(global::ProtoBuf.Meta.TypeModel) + "." + nameof(global::ProtoBuf.Meta.TypeModel.MaxDepth) + "): " + _depth.ToString());

    /// <summary>
    /// Whether <paramref name="tag"/> is the current group's end sentinel - the switch-default
    /// test, so matched fields never pay for it. The tag was already consumed by
    /// <see cref="ReadRawTag"/>; a wiretype-4 tag that fails this test belongs to nobody and
    /// should go to <see cref="SkipTag"/>, which throws.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsScopeEnd(uint tag)
    {
        if ((tag & 7) != 4) return false;
        if ((long)(tag >> 3) == -_scope) return true;
        return LegacyGroupEnd(tag);
    }

    /// <summary>
    /// A generated raw read can be invoked THROUGH the legacy framing: a classic serializer (or
    /// <c>ReadMessage</c> on a group-formatted member) calls <c>StartSubItem</c>, whose group arm
    /// mints a token and increments depth WITHOUT pushing a raw scope - so the end-group tag
    /// belongs to a frame this scope slot knows nothing about. Stash it exactly as
    /// <c>ReadFieldHeader</c>'s end-group spoof would (<c>_wireType</c>/<c>_fieldNumber</c>), so
    /// the caller's <c>EndSubItem</c> verifies the right group ended. Guarded to legacy frames
    /// only (<c>_scope</c> not group-encoded): inside a RAW group scope a mismatched end tag
    /// stays false, falls to <c>SkipTag</c>, and throws. The two remaining stray-tag routes are
    /// both caught downstream - under a raw length scope by <c>PopScope</c>'s exact-consumption
    /// check, under a legacy length frame by <c>EndSubItem</c>'s "terminated via end-group"
    /// check - and at the root (<c>_depth == 0</c>) there is no frame, so it stays false.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool LegacyGroupEnd(uint tag)
    {
        if (_depth > 0 && _scope >= 0)
        {
            _wireType = WireType.EndGroup;
            _fieldNumber = (int)(tag >> 3);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Whether the current LENGTH scope is exhausted - the loop test for packed elements, which
    /// carry no tags to hand to <see cref="ReadRawTag"/>: push the length prefix, read elements
    /// while this is false, pop. Meaningless in group mode (packed data is always
    /// length-prefixed), where it reports end-of-segment instead.
    /// </summary>
    public bool AtScopeEnd
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _offset >= _effectiveEnd;
    }

    // ---------------------------------------------------------------- snapshot

    /// <summary>
    /// The storable (non-ref-struct) form: the class-API bridge and the iterator paths (which
    /// cannot hold a ref struct) live on this. Verbatim fields; the ref needs no slot - the
    /// segment-start index is recovered via ByteOffset at snapshot time and the ref re-derived
    /// on restore. The leased buffer is held DIRECTLY (in-process ownership; see PORTING.md).
    /// </summary>
    internal readonly ReaderSnapshot Snapshot()
        => new ReaderSnapshot(
            _buffer,
#if NET7_0_OR_GREATER
            _buffer is null ? 0 : (int)Unsafe.ByteOffset(ref MemoryMarshal.GetArrayDataReference(_buffer), ref Unsafe.AsRef(in _segment)),
#else
            _segmentStart,
#endif
            _offset, _count, _effectiveEnd, _leased, _positionBase, _remaining, _scope,
            _source, _nextPosition, _depth, _fieldNumber, _wireType, _pendingTag,
            _model, _userState, _internStrings, _stringInterner);

    /// <summary>Reconstitutes a reader from a snapshot.</summary>
    internal State(scoped in ReaderSnapshot snapshot)
    {
        _buffer = snapshot.Buffer;
#if NET7_0_OR_GREATER
        _segment = ref snapshot.Buffer is null
            ? ref Unsafe.NullRef<byte>()
            : ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(snapshot.Buffer), snapshot.SegmentStart);
#else
        _segmentStart = snapshot.SegmentStart;
#endif
        _offset = snapshot.Offset;
        _count = snapshot.Count;
        _effectiveEnd = snapshot.EffectiveEnd;
        _leased = snapshot.Leased;
        _positionBase = snapshot.PositionBase;
        _remaining = snapshot.Remaining;
        _scope = snapshot.Scope;
        _source = snapshot.Source;
        _nextPosition = snapshot.NextPosition;
        _depth = snapshot.Depth;
        _fieldNumber = snapshot.FieldNumber;
        _wireType = snapshot.WireTypeValue;
        _pendingTag = snapshot.PendingTag;
        _model = snapshot.Model;
        _userState = snapshot.UserState;
        _internStrings = snapshot.InternStringsValue;
        _stringInterner = snapshot.Interner;
    }

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
    /// The end path: EITHER the scope is genuinely finished (return 0), OR the current segment
    /// is merely exhausted mid-scope and a refill continues - the distinction the clamped
    /// <see cref="_effectiveEnd"/> deliberately erases on the hot path lives here, cold. In
    /// group mode (or a length scope extending past EOF), running out of data is corrupt input,
    /// matching legacy's EndSubItem validation; an UNBOUNDED root (a bare Stream with no length
    /// hint, _scope == long.MaxValue) treats EOF as the clean end of the document.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private uint EndOfScope()
    {
        while (true)
        {
            if (_scope >= 0 && _positionBase + _offset >= _scope) return 0; // genuine scope end
            if (!GetNextBuffer())
            {
                if (_scope == long.MaxValue) return 0; // unbounded root: clean EOF
                ThrowEndOfData(); // truncated group, or a length scope the data never delivered
            }
            if (_offset < _effectiveEnd) return ReadRawTag(); // refreshed window: go again
        }
    }

    /// <summary>One byte, crossing refills - the universal straddle primitive: forward-only
    /// consumption means every slow path can cross a segment boundary byte-wise with no state
    /// to carry.</summary>
    private byte ReadRawByte()
    {
        if (_offset >= _count && !GetNextBuffer()) ThrowEndOfData();
        return At(_offset++);
    }

    /// <summary>
    /// A field-0 tag is invalid protobuf, and LEGACY's SetTag reported it as a ProtoException
    /// ("Invalid field in source data: 0") - retained deliberately: ProtoException is the
    /// contract callers catch on corrupt input, so the raw path must not downgrade it to a
    /// plain InvalidOperationException, and a bare zero byte must not read as a false
    /// end-of-message.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowInvalidFieldZero()
        => throw AddErrorData(new ProtoException("Invalid field in source data: 0"), ref this);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private uint ReadRawTagTail(uint value, int offset)
    {
        // values 0-7 land here too (field 0, any wire type): invalid, exactly as legacy's SetTag
        // throws "Invalid field in source data" - same exception TYPE deliberately, see the helper
        if ((value & 0x80) == 0) ThrowInvalidFieldZero();
        // continuation beyond byte 0: rarer, consumed byte-wise so a tag straddling a refill
        // needs no special-casing (ByteUnrolled shape - see VarintU32DecodeResults.md). A
        // MINIMALLY-encoded multi-byte tag has field >= 16, but an OVERLONG encoding can still
        // deliver a field-0 tag (0x80 0x00 decodes to 0 - a false end-of-message; 0x82 0x00 to
        // field 0, wire 2), so the decoded value is checked once at the exit of this cold path;
        // the single-byte fast path needs nothing, its range test already excludes 0-7.
        _offset = offset + 1; // commit the first byte; the rest cross refills as they come
        value &= 0x7F;
        int shift = 7;
        for (int i = 1; i < 5; i++)
        {
            uint b = ReadRawByte();
            value |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                if (value < 8) ThrowInvalidFieldZero();
                return value;
            }
            shift += 7;
        }
        ThrowOverflowError(); // varint exhaustion = OverflowException, as legacy
        return 0;
    }

    // There is deliberately NO TryReadRawTag: with the tag in a caller local, run consumption for
    // repeated fields is the tag read as the do-while condition (miss falls back to dispatch via
    // continue), and fields-in-order speculation is a compare + goto case - both decode each tag
    // exactly once with zero stored state. A Try form would have to decode-and-discard or stash;
    // see docs/nano-core.md, "Run consumption needs no API at all".

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
            case 3: // start-group
                SkipGroup(tag);
                break;
            default: // 4 = end-group that nothing expected; 6/7 are not wire types
                ThrowMalformed();
                break;
        }
    }

    /// <summary>
    /// Skips an unknown group-framed field: read-and-skip until the matching end sentinel (the
    /// start tag plus one). Depth-guarded UNCONDITIONALLY, elision lever or not: unknown fields
    /// nest arbitrarily regardless of the model - the wire decides, not the schema. A nested
    /// group recurses through <see cref="SkipTag"/>; any other wiretype-4 tag (a mismatched
    /// end-group) throws there.
    /// </summary>
    private void SkipGroup(uint startTag)
    {
        if (IncrDepthExceeded()) ThrowTooDeep();
        uint endTag = startTag + 1; // only the low 3 bits differ: 3 becomes 4
        while (true)
        {
            uint tag = ReadRawTag();
            if (tag == 0) ThrowMalformed(); // scope/data ended before the end-group arrived
            if (tag == endTag) break;
            SkipTag(tag);
        }
        _depth--;
    }

    internal void Advance(int bytes)
    {
        if (bytes < 0) ThrowEndOfData();
        while (true)
        {
            int local = _count - _offset;
            if (bytes <= local)
            {
                _offset += bytes;
                return;
            }
            _offset = _count;
            bytes -= local;
            if (!GetNextBuffer()) ThrowEndOfData();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowEndOfData() => throw AddErrorData(new EndOfStreamException(), ref this);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowMalformed() => throw AddErrorData(new InvalidOperationException("malformed data"), ref this);

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
        ThrowOverflowError(); // varint exhaustion = OverflowException, as legacy
        return 0;
    }

    /// <summary>Reads a zigzag-encoded varint as i32 - the DataFormat.ZigZag selection, made at
    /// compile time by the generator where legacy needed the Hint dance. Tolerant of the 64-bit
    /// form, as ReadRawVarint32 is.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadRawZigZag32()
    {
        uint value = ReadRawVarint32();
        return unchecked((int)((value >> 1) ^ (uint)-(int)(value & 1)));
    }

    /// <summary>Reads a zigzag-encoded varint as i64.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadRawZigZag64()
    {
        ulong value = ReadRawVarint64();
        return unchecked((long)((value >> 1) ^ (ulong)-(long)(value & 1)));
    }

    /// <summary>Reads a fixed32-framed float. The netfx arm reinterprets via Unsafe:
    /// Int32BitsToSingle does not exist down-level, and the bit pattern is the value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadRawSingle()
    {
        uint bits = ReadRawFixed32();
#if NET7_0_OR_GREATER
        return BitConverter.Int32BitsToSingle(unchecked((int)bits));
#else
        return Unsafe.As<uint, float>(ref bits);
#endif
    }

    /// <summary>Reads a fixed64-framed double.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadRawDouble()
        => BitConverter.Int64BitsToDouble(unchecked((long)ReadRawFixed64()));

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
                if (++i == 10) { ThrowOverflowError(); return 0; }
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
            ulong b = ReadRawByte(); // byte-wise: crosses refills with no state to carry
            value |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0) return value;
            shift += 7;
        }
        ThrowOverflowError(); // varint exhaustion = OverflowException, as legacy
        return 0;
    }

    /// <summary>
    /// Reads a length-prefixed UTF-8 string; zero length is the empty-string singleton. The
    /// plausible-length guard lives here at its natural home: the claimed length must fit the
    /// data we actually have (single-segment v1 makes this the strong form of legacy's
    /// EagerAllocationLimit check), so a hostile prefix cannot drive allocation.
    /// </summary>
    public string ReadRawString()
    {
        int len = checked((int)ReadRawVarint32());
        if (len == 0) return "";
        if ((uint)len > (uint)(_count - _offset)) return ReadRawStringSlow(len); // straddles: assemble
        var offset = _offset;
        _offset = offset + len;
#if NET7_0_OR_GREATER
        return System.Text.Encoding.UTF8.GetString(
            MemoryMarshal.CreateReadOnlySpan(ref At(offset), len));
#else
        // the buffer+index layout is the natural netfx fast path here
        return System.Text.Encoding.UTF8.GetString(_buffer, _segmentStart + offset, len);
#endif
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private string ReadRawStringSlow(int len)
    {
        var scratch = FillScratch(len);
        var result = System.Text.Encoding.UTF8.GetString(scratch, 0, len);
        ArrayPool<byte>.Shared.Return(scratch);
        return result;
    }

    /// <summary>
    /// Assembles <paramref name="len"/> straddling bytes into a rented scratch. The allocation
    /// policy (docs/nano-core.md): where the total is known (Memory/sequence/hinted stream) the
    /// claim is verified up front and rented once; where it is not (a bare stream), the scratch
    /// GROWS AS REAL BYTES ARRIVE - a hostile length prefix costs at most the actual payload,
    /// the eager-allocation problem dissolved rather than capped.
    /// </summary>
    private byte[] FillScratch(int len)
    {
        if (_remaining >= 0)
        {
            if ((long)len > (long)(_count - _offset) + _remaining) ThrowEndOfData();
            var scratch = ArrayPool<byte>.Shared.Rent(len);
            FillFrom(scratch, 0, len);
            return scratch;
        }
        var grow = ArrayPool<byte>.Shared.Rent(Math.Min(len, EagerAllocationLimit));
        int filled = 0;
        while (filled < len)
        {
            if (_offset >= _count && !GetNextBuffer()) ThrowEndOfData();
            int take = Math.Min(len - filled, _count - _offset);
            if (filled + take > grow.Length)
            {
                var bigger = ArrayPool<byte>.Shared.Rent(Math.Min(len, Math.Max(grow.Length * 2, filled + take)));
                Buffer.BlockCopy(grow, 0, bigger, 0, filled);
                ArrayPool<byte>.Shared.Return(grow);
                grow = bigger;
                take = Math.Min(take, grow.Length - filled);
            }
            CopyWindowTo(grow, filled, take);
            filled += take;
        }
        return grow;
    }

    /// <summary>Copies exactly <paramref name="bytes"/> into <paramref name="dest"/>, crossing
    /// refills; the caller has already verified plausibility.</summary>
    private void FillFrom(byte[] dest, int destOffset, int bytes)
    {
        while (bytes > 0)
        {
            if (_offset >= _count && !GetNextBuffer()) ThrowEndOfData();
            int take = Math.Min(bytes, _count - _offset);
            CopyWindowTo(dest, destOffset, take);
            destOffset += take;
            bytes -= take;
        }
    }

    private void CopyWindowTo(byte[] dest, int destOffset, int bytes)
    {
#if NET7_0_OR_GREATER
        MemoryMarshal.CreateReadOnlySpan(ref At(_offset), bytes)
            .CopyTo(dest.AsSpan(destOffset, bytes));
#else
        Buffer.BlockCopy(_buffer, _segmentStart + _offset, dest, destOffset, bytes);
#endif
        _offset += bytes;
    }

    // ------------------------------------------------------------ extension data
    //
    // Unknown-field retention for extensible contracts: the field is CAPTURED into the
    // instance's extension bag instead of skipped, in wire format, so the write side can blit it
    // back out. Two byte-fidelity rules, both deliberate: the TAG is re-encoded canonically -
    // its original bytes are behind the offset, unreachable under forward-only, and the caller's
    // parameter supplies the value (the raw convention solving its own constraint); legacy
    // re-encodes headers through ProtoWriter too, so this matches. The PAYLOAD is teed
    // byte-preserving (original varint encodings kept), resident block-writes where possible,
    // byte-wise across refills otherwise. Group-framed unknowns capture recursively with
    // re-encoded markers, depth-guarded unconditionally - the wire decides nesting, not the
    // schema.

    /// <summary>Captures the unknown field whose tag was just read into the instance's untyped
    /// extension bag (<see cref="IExtensible"/>).</summary>
    public void AppendExtensionData(uint tag, IExtensible instance)
        => AppendExtensionDataCore(tag, instance.GetExtensionObject(createIfMissing: true));

    /// <summary>Captures into the per-type bag of an <see cref="ITypedExtensible"/> - each layer
    /// of a hierarchy keys its own, exactly as the legacy typed overload does.</summary>
    public void AppendExtensionData(uint tag, ITypedExtensible instance, Type type)
        => AppendExtensionDataCore(tag, instance.GetExtensionObject(type, createIfMissing: true));

    private void AppendExtensionDataCore(uint tag, IExtension extension)
    {
        var dest = extension.BeginAppend();
        try
        {
            CaptureField(tag, dest);
            extension.EndAppend(dest, commit: true);
        }
        catch
        {
            extension.EndAppend(dest, commit: false);
            throw;
        }
    }

    private void CaptureField(uint tag, Stream dest)
    {
        WriteVarintTo(dest, tag);
        switch (tag & 7)
        {
            case 0: // varint: byte-preserving tee
                CaptureVarint(dest);
                break;
            case 1: // fixed64
                CaptureBytes(dest, 8);
                break;
            case 2: // length-prefixed
            {
                int len = checked((int)ReadRawVarint32());
                WriteVarintTo(dest, (uint)len);
                if (_remaining >= 0 && (long)len > (long)(_count - _offset) + _remaining) ThrowEndOfData();
                CaptureBytes(dest, len);
                break;
            }
            case 3: // group: recursive capture to the matching sentinel (start tag + 1)
            {
                if (IncrDepthExceeded()) ThrowTooDeep();
                uint endTag = tag + 1;
                while (true)
                {
                    uint inner = ReadRawTag();
                    if (inner == 0) ThrowMalformed(); // scope/data ended before the end-group
                    if (inner == endTag)
                    {
                        WriteVarintTo(dest, inner);
                        break;
                    }
                    CaptureField(inner, dest);
                }
                _depth--;
                break;
            }
            case 5: // fixed32
                CaptureBytes(dest, 4);
                break;
            default: // 4 = end-group nothing expected; 6/7 are not wire types
                ThrowMalformed();
                break;
        }
    }

    private static void WriteVarintTo(Stream dest, uint value)
    {
        while (value >= 0x80)
        {
            dest.WriteByte((byte)(value | 0x80));
            value >>= 7;
        }
        dest.WriteByte((byte)value);
    }

    /// <summary>Tees one varint byte-for-byte - the original encoding is preserved, overlong or
    /// not, because the bag's promise is fidelity.</summary>
    private void CaptureVarint(Stream dest)
    {
        for (int i = 0; i < 10; i++)
        {
            byte b = ReadRawByte();
            dest.WriteByte(b);
            if ((b & 0x80) == 0) return;
        }
        ThrowOverflowError(); // varint exhaustion = OverflowException, as legacy
    }

    private void CaptureBytes(Stream dest, int bytes)
    {
        if (bytes < 0) ThrowEndOfData();
        while (bytes > 0)
        {
            if (_offset >= _count && !GetNextBuffer()) ThrowEndOfData();
            int take = Math.Min(bytes, _count - _offset);
#if NET7_0_OR_GREATER
            dest.Write(MemoryMarshal.CreateReadOnlySpan(ref At(_offset), take));
#else
            dest.Write(_buffer, _segmentStart + _offset, take);
#endif
            _offset += take;
            bytes -= take;
        }
    }

    /// <summary>Reads 4 bytes little-endian. The big-endian branch folds away at JIT time on
    /// every little-endian platform (IsLittleEndian is a JIT constant), so correctness on BE
    /// costs nothing - and legacy is BE-correct via BinaryPrimitives, so anything less would be
    /// a platform regression.</summary>
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

    /// <summary>
    /// Reads a varint as u32, STRICT: overflow beyond 32 bits throws rather than truncating -
    /// the legacy Unsigned mode, used for lengths, where a lying 10-byte form must not quietly
    /// become a garbage length.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadRawVarint32Strict()
    {
        uint value = ReadRawByte();
        if ((value & 0x80) == 0) return value;
        value &= 0x7F;
        int shift = 7;
        for (int i = 1; i < 5; i++)
        {
            uint b = ReadRawByte();
            value |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                if (i == 4 && (b & 0xF0) != 0) ThrowOverflowError(); // only 4 bits fit from byte 5
                return value;
            }
            shift += 7;
        }
        ThrowOverflowError();
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowOverflowError() => throw AddErrorData(new OverflowException(), ref this);

    // ------------------------------------------------------------ packed fast paths
    //
    // Packed reads are one-line helper calls from generated code, and the platform fork lives
    // HERE as ordinary #if - not in the emitted code, not as a generator decision - so every
    // scenario is reviewable in one place, the goldens stay TFM-independent, and the multi-TFM
    // test legs exercise each arm for free. The rule (docs/nano-core.md): #if in the library for
    // body-shape variants that are perf-only and keyed by BCL availability; the generator decides
    // only where the choice alters the plan or diagnostics. A packed run is not nesting, so these
    // bound by length directly: no scope push/pop, no depth count.
    //
    // RESIDENCY (the forward-only rule's other edge): the bulk arms - the terminator pre-scan and
    // the block copy - peek ahead without consuming, which is legal ONLY over bytes already in
    // the current segment; nothing can replay across a refill, and a run larger than the buffer
    // can never be made resident at all. So the plausible-length check below, which in
    // single-segment v1 means "truncated data, throw", becomes the FAST/SLOW SWITCH when
    // GetNextBuffer arrives: resident -> bulk arm; straddling -> the plain per-element forward
    // loop, crossing refills exactly as ordinary reads do. (The Stream refill may choose to make
    // residency common - shift-and-top-up already preserves from the current offset, so topping
    // up until the run is local, capped by buffer size, is a legal courtesy - but that is the
    // refill layer's decision; these helpers only ask whether the bytes are here.)

    /// <summary>
    /// Reads a packed varint-encoded run into <paramref name="values"/> (appending - merge
    /// semantics account for pre-existing elements). On net8+ the element count is computed
    /// exactly up front - every varint ends with exactly one non-continuation byte, so the count
    /// of high-bit-clear bytes IS the element count - allowing SetCount + a bounds-check-free
    /// span fill; whether that beats the plain Add loop is measured, not assumed
    /// (PackedParseResults.md). A truncated trailing varint fails the exact-consumption check.
    /// </summary>
    public void ReadPackedVarint32(List<int> values)
    {
        int len = checked((int)ReadRawVarint32());
        if (len == 0) return;
        if ((uint)len > (uint)(_count - _offset)) // the residency switch, exactly as designed
        {
            ReadPackedVarint32Straddle(len, values);
            return;
        }
        int end = _offset + len;
#if NET8_0_OR_GREATER
        int count = 0;
        for (int i = _offset; i < end; i++)
        {
            if ((At(i) & 0x80) == 0) count++;
        }
        int oldCount = values.Count;
        CollectionsMarshal.SetCount(values, oldCount + count);
        foreach (ref var slot in CollectionsMarshal.AsSpan(values).Slice(oldCount))
        {
            slot = unchecked((int)ReadRawVarint32());
        }
        if (_offset != end) ThrowMalformed(); // trailing bytes the scan did not count as elements
#else
        while (_offset < end) values.Add(unchecked((int)ReadRawVarint32()));
        if (_offset != end) ThrowMalformed(); // the final varint overran the declared length
#endif
    }

    /// <summary>The non-resident arm: per-element forward reads crossing refills - correct and
    /// unspectacular, for the rare run that straddles or exceeds the buffer.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ReadPackedVarint32Straddle(int len, List<int> values)
    {
        long end = Position + len;
        if (_remaining >= 0 && end - _positionBase > (long)_count + _remaining) ThrowEndOfData();
        while (Position < end) values.Add(unchecked((int)ReadRawVarint32()));
        if (Position != end) ThrowMalformed(); // the final varint overran the declared length
    }

    /// <summary>
    /// Reads a packed fixed32 run into <paramref name="values"/> (appending). The count is exact
    /// by construction (length / 4), and on net8+ little-endian the fill is a single block copy;
    /// the big-endian branch (per-element, endian-corrected by ReadRawFixed32) folds away at JIT
    /// time on every little-endian platform.
    /// </summary>
    public void ReadPackedFixed32(List<int> values)
    {
        int len = checked((int)ReadRawVarint32());
        if (len == 0) return;
        if ((len & 3) != 0) ThrowMalformed();
        if ((uint)len > (uint)(_count - _offset)) // the residency switch
        {
            ReadPackedFixed32Straddle(len, values);
            return;
        }
        int count = len >> 2;
#if NET8_0_OR_GREATER
        int oldCount = values.Count;
        CollectionsMarshal.SetCount(values, oldCount + count);
        var dest = CollectionsMarshal.AsSpan(values).Slice(oldCount);
        if (BitConverter.IsLittleEndian)
        {
            MemoryMarshal.Cast<byte, int>(MemoryMarshal.CreateReadOnlySpan(ref At(_offset), len))
                .CopyTo(dest);
            _offset += len;
        }
        else
        {
            foreach (ref var slot in dest) slot = unchecked((int)ReadRawFixed32());
        }
#else
        for (int i = 0; i < count; i++) values.Add(unchecked((int)ReadRawFixed32()));
#endif
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ReadPackedFixed32Straddle(int len, List<int> values)
    {
        long end = Position + len;
        if (_remaining >= 0 && end - _positionBase > (long)_count + _remaining) ThrowEndOfData();
        int count = len >> 2;
        for (int i = 0; i < count; i++) values.Add(unchecked((int)ReadRawFixed32()));
    }

    /// <summary>
    /// Reads a length-prefixed bytes field as a fresh array - REPLACE semantics, the decided
    /// default (docs/nano-core.md, merge semantics): the caller assigns, nothing is appended.
    /// Same plausible-length guard as <see cref="ReadRawString"/>.
    /// </summary>
    public byte[] ReadRawBytes()
    {
        int len = checked((int)ReadRawVarint32());
        if (len == 0) return [];
        if ((uint)len > (uint)(_count - _offset)) return ReadRawBytesSlow(len); // straddles
        var offset = _offset;
        _offset = offset + len;
        var result = new byte[len];
#if NET7_0_OR_GREATER
        MemoryMarshal.CreateReadOnlySpan(ref At(offset), len).CopyTo(result);
#else
        Buffer.BlockCopy(_buffer, _segmentStart + offset, result, 0, len);
#endif
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private byte[] ReadRawBytesSlow(int len)
    {
        if (_remaining >= 0)
        {
            // verified claim: fill the exact result directly, no scratch at all
            if ((long)len > (long)(_count - _offset) + _remaining) ThrowEndOfData();
            var result = new byte[len];
            FillFrom(result, 0, len);
            return result;
        }
        var scratch = FillScratch(len); // unknown total: grown by real bytes
        var exact = new byte[len];
        Buffer.BlockCopy(scratch, 0, exact, 0, len);
        ArrayPool<byte>.Shared.Return(scratch);
        return exact;
    }
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
/// The storable (non-ref-struct) snapshot of a <see cref="ProtoReader.State"/>: plain fields
/// only, the ref field represented as a segment-start index. The class-API bridge and the
/// iterator paths live on this.
/// </summary>
internal readonly struct ReaderSnapshot
{
    internal readonly byte[] Buffer;
    internal readonly int SegmentStart, Offset, Count, EffectiveEnd;
    internal readonly bool Leased;
    internal readonly long PositionBase, Remaining, Scope;
    internal readonly object Source;
    internal readonly System.SequencePosition NextPosition;
    internal readonly int Depth, FieldNumber;
    internal readonly WireType WireTypeValue;
    internal readonly uint PendingTag;
    internal readonly global::ProtoBuf.Meta.TypeModel Model;
    internal readonly object UserState;
    internal readonly bool InternStringsValue;
    internal readonly System.Collections.Generic.Dictionary<string, string> Interner;

    internal ReaderSnapshot(byte[] buffer, int segmentStart, int offset, int count,
        int effectiveEnd, bool leased, long positionBase, long remaining, long scope,
        object source, System.SequencePosition nextPosition, int depth, int fieldNumber,
        WireType wireType, uint pendingTag, global::ProtoBuf.Meta.TypeModel model,
        object userState, bool internStrings,
        System.Collections.Generic.Dictionary<string, string> interner)
    {
        Buffer = buffer; SegmentStart = segmentStart; Offset = offset; Count = count;
        EffectiveEnd = effectiveEnd; Leased = leased; PositionBase = positionBase;
        Remaining = remaining; Scope = scope; Source = source; NextPosition = nextPosition;
        Depth = depth; FieldNumber = fieldNumber; WireTypeValue = wireType;
        PendingTag = pendingTag; Model = model; UserState = userState;
        InternStringsValue = internStrings; Interner = interner;
    }
}

    }
}