using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ProtoBuf
{
    /// <summary>
    /// Carries measured sub-message lengths from the generated <c>Measure_</c> pass to the
    /// generated <c>RawWrite_</c> pass, by POSITION rather than by object identity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both passes walk the graph pre-order over the same guards, so a slot index is all the
    /// correlation needed and no hashing is required. <b>A slot is reserved exactly where the write
    /// calls <see cref="Next"/>, one for one, in the same order</b> - so the slot belongs to the
    /// CALL SITE, not to the contract being measured. The measure reserves at a length-prefixed
    /// sub-message site, recurses, and fills the slot on the way out; the write reads it with a
    /// bare <see cref="Next"/> and recurses, with no index arithmetic anywhere.
    /// </para>
    /// <para>
    /// Two consequences follow, and both are load-bearing rather than tidy-ups. A member that is
    /// measured but whose write reads no length - a <c>group</c>, which carries none, or anything
    /// handed to the classic engine - must reserve <b>nothing</b>, or every later length shifts.
    /// And a sub-tree the raw write will not walk is measured with a <b>null</b> buffer, which
    /// suppresses reservation all the way down. An earlier draft had <c>Measure_</c> claim a slot
    /// for itself, which cannot express "measured but not read"; see <c>notes/gaps.md</c> B38.
    /// </para>
    /// <para>
    /// This replaces a <c>Dictionary&lt;object, long&gt;</c> that cost three hash operations per
    /// sub-message node on deep graphs and two on wide ones - around half of a length-prefixed
    /// serialize. See <c>notes/gaps.md</c> B38 for the measurements and for why the dictionary was
    /// not merely a slow container: it is what made the previous LAZY scheme linear, so removing it
    /// requires measuring eagerly. Measuring lazily without it is O(n^2) in depth.
    /// </para>
    /// <para>
    /// <b>Positional transport cannot cross a call boundary</b>, which is why <see cref="Enter"/>
    /// and <see cref="Leave"/> exist. The classic engine's measure hook
    /// (<c>IMeasuringSerializer&lt;T&gt;.Measure</c>) runs at one moment and the matching
    /// <c>ISerializer&lt;T&gt;.Write</c> at another, with arbitrary work in between - it may measure
    /// several objects before writing any of them. A cursor cannot span that; identity can. So a
    /// crossing records <c>object -&gt; slot index</c> and the write recovers it: <b>one hash per
    /// crossing, none per node</b>. That map is allocated only if a crossing actually happens, so a
    /// fully-raw model never pays for it at all.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
    public sealed class RawLengthBuffer
    {
        /// <summary>
        /// A buffer that accepts reservations and throws them away, for measuring a sub-tree the
        /// raw write will NOT walk.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Such a sub-tree must reserve <b>nothing</b> - the write hands it to the stateful engine,
        /// which computes its own lengths, so a reservation here would shift every later slot. The
        /// rule was "measure it with a null buffer, which suppresses reservation all the way down",
        /// and the second half of that sentence was <b>not true</b>: the callee's own
        /// <c>Reserve()</c> sites are unconditional, so a null buffer reached one and threw. That is
        /// what this exists to fix, and it fixes it without a branch at every call site.
        /// </para>
        /// <para>
        /// Only <see cref="Reserve"/> and <see cref="Set"/> are meaningful on it. Nothing reads
        /// from a discarded sub-tree by construction, so <see cref="Next"/> and the boundary
        /// members are never reached; they are left as they are rather than made to throw, since a
        /// throw here would be a worse failure than the wrong answer it replaces.
        /// </para>
        /// </remarks>
        public static readonly RawLengthBuffer Discard = new RawLengthBuffer(discard: true);

        private readonly bool _discard;

        /// <summary>Creates a buffer.</summary>
        public RawLengthBuffer() { }

        private RawLengthBuffer(bool discard) => _discard = discard;

        private long[] _slots = new long[64];
        private int _count;     // append high-water: the next slot Reserve() will hand out
        private int _read;      // consume cursor: the next slot Next() will return
        // object -> its slot index, populated ONLY where a raw walk is entered from the classic
        // engine; null until that first happens, which for a fully-raw model is never
        private Dictionary<object, int> _boundary;

        /// <summary>Claims the next slot and returns its index; fill it with <see cref="Set"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Reserve()
        {
            if (_discard) return 0;
            var index = _count++;
            if (index >= _slots.Length) Grow();
            return index;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Grow() => Array.Resize(ref _slots, _slots.Length * 2);

        /// <summary>Stores a measured length into a slot claimed by <see cref="Reserve"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, long length)
        {
            if (!_discard) _slots[index] = length;
        }

        /// <summary>Reads the next measured length, in the order the measure pass produced them.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long Next() => _slots[_read++];

        /// <summary>The index the next <see cref="Reserve"/> will hand out.</summary>
        public int Mark() => _count;

        /// <summary>Positions the read cursor, which a write pass then consumes forwards from.</summary>
        public void SeekTo(int index) => _read = index;

        /// <summary>
        /// Records where a sub-tree's slots begin, against the object they describe, so a later
        /// call can find them. Only used when a raw walk is entered from outside.
        /// </summary>
        public void Enter(object value, int index)
            => (_boundary ??= new Dictionary<object, int>(ReferenceComparer.Instance))[value] = index;

        /// <summary>
        /// Recovers a sub-tree's starting slot recorded by <see cref="Enter"/>. False where the
        /// measure pass has not run for this object, in which case the caller must measure first.
        /// </summary>
        public bool Leave(object value, out int index)
        {
            if (_boundary is not null) return _boundary.TryGetValue(value, out index);
            index = 0;
            return false;
        }

        /// <summary>
        /// The context a generated <c>Measure_</c> pass hands to a consumer's callbacks, for which
        /// <see cref="ProtoWriter.IsMeasuring(ISerializationContext)"/> answers <c>true</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A measure-first contract fires <c>[ProtoBeforeSerialization]</c> in <b>both</b> passes,
        /// because both must observe the same object or the measured length will not match the
        /// bytes written. That is only tolerable if a callback can tell the passes apart - firing
        /// twice indistinguishably would be worse than the old behaviour of refusing to measure
        /// such a contract at all - and this is what makes it possible.
        /// </para>
        /// <para>
        /// The classic buffer-writer backend answers the same question by <b>being</b> a counting
        /// writer (<c>NullProtoWriter</c>). The raw measure has no writer at all - it is
        /// arithmetic - so it borrows the real context's model and user-state and differs only in
        /// this one answer. Cached against the context it wraps, so a whole serialize costs one
        /// allocation and a reused writer costs none.
        /// </para>
        /// </remarks>
        public ISerializationContext AsMeasuring(ISerializationContext context)
        {
            var measuring = _measuring;
            if (measuring is null || !ReferenceEquals(measuring.Inner, context))
            {
                _measuring = measuring = new MeasuringContext(context);
            }
            return measuring;
        }

        private MeasuringContext _measuring;

        private sealed class MeasuringContext : ISerializationContext, IMeasuringPassContext
        {
            internal readonly ISerializationContext Inner;
            internal MeasuringContext(ISerializationContext inner) => Inner = inner;
            public TypeModel Model => Inner.Model;
            public object UserState => Inner.UserState;
        }

        /// <summary>Whether the write pass consumed exactly what the measure pass produced.</summary>
        /// <remarks>
        /// The positional scheme's one real failure mode is the two passes disagreeing about which
        /// sub-messages exist, which would shift every later length. <c>DebugAssertPosition</c>
        /// catches that in DEBUG per member; this is the cheap whole-payload version, an int
        /// comparison, so the generated root can check it in any configuration.
        /// </remarks>
        public bool Balanced => _read == _count;

        /// <summary>
        /// Drops everything recorded, ready for a fresh root. The writer does this per serialize;
        /// it is public because anything driving the generated <c>Measure_</c> statics directly
        /// holds its own buffer and must - <see cref="Reserve"/> only ever moves forwards, so a
        /// buffer reused without this grows without bound.
        /// </summary>
        public void Reset()
        {
            _count = _read = 0;
            _boundary?.Clear();
        }

        /// <summary>
        /// Hands back capacity, on the same signals the length dictionaries use: real memory
        /// pressure, or a buffer too large to be worth retaining regardless.
        /// </summary>
        internal void Trim(bool pressure, int retainedCap)
        {
            if (pressure || _count > retainedCap) _slots = new long[64];
            _count = _read = 0;
            if (_boundary is not null && (pressure || _boundary.Count > retainedCap)) _boundary = null;
            else _boundary?.Clear();
            if (pressure) _measuring = null; // it pins the writer it wraps
        }

        /// <summary>
        /// Swaps contents with another buffer, for the measure-then-serialize hand-off. Deliberately
        /// a swap rather than a copy: copying the length caches there once cost an extra 22 KB and
        /// 11%. The read cursor is reset rather than swapped - the receiving writer consumes from
        /// wherever it seeks, not from where the measuring writer stopped.
        /// </summary>
        internal void SwapWith(RawLengthBuffer other)
        {
            // plain temporaries rather than tuple deconstruction: net462 has no ValueTuple,
            // which is why NetObjectCache.InitializeFrom spells its swaps out the same way
            var slots = _slots; _slots = other._slots; other._slots = slots;
            var count = _count; _count = other._count; other._count = count;
            var boundary = _boundary; _boundary = other._boundary; other._boundary = boundary;
            _read = other._read = 0;
        }

        // reference identity regardless of user Equals/GetHashCode overrides; the BCL's
        // ReferenceEqualityComparer is net5+ only, and this must serve every TFM
        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceComparer Instance = new ReferenceComparer();
            private ReferenceComparer() { }
            bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);
            int IEqualityComparer<object>.GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
