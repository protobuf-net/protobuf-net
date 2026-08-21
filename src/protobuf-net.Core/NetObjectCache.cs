using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProtoBuf
{
    internal sealed class NetObjectCache
    {
        private Dictionary<ObjectKey, long> _knownLengths = new();

        // the raw measure pass's ??= length cache (notes/nano-writer.md): sub-message lengths
        // keyed by reference identity, populated post-order by the generated Measure_ statics
        // and consumed at the write sites. It lives HERE rather than on the writer because this
        // cache is the codebase's established home for cross-writer measurement state: the
        // buffer-writer's null-writer sidecar shares the parent's instance by construction
        // ("share the *same* known objects key"), and MeasureState's Serialize hands the
        // measuring writer's cache to the writing writer via InitializeFrom - so a length
        // measured ANYWHERE serves the write EVERYWHERE, captured the first time. Clearing
        // rides the same lifecycle as _knownLengths, which is what makes a stale entry (a
        // corrupt stream, not an error) impossible wherever known-lengths were already safe.
        // long, deliberately, matching _knownLengths: a single message body CAN exceed
        // int.MaxValue (many large byte[] members, say), and classic handles it - int
        // arithmetic would overflow silently, which is a corrupt stream, not an error
        private Dictionary<object, long> _rawLengths = new(RawLengthComparer.Instance);

        internal Dictionary<object, long> RawLengths => _rawLengths;

        // the ordered replacement for _rawLengths on the generated raw path: same job - carry a
        // measured sub-message length from Measure_ to RawWrite_ - but keyed by POSITION, which
        // costs no hashing at all. notes/gaps.md B38. Kept beside the dictionary rather than
        // replacing it outright, because the classic engine's own measure-by-writing path still
        // uses _knownLengths and the two must not be confused.
        private RawLengthBuffer _rawSlots = new();

        internal RawLengthBuffer RawSlots => _rawSlots;

        // reference identity regardless of user Equals/GetHashCode overrides; the BCL's
        // ReferenceEqualityComparer is net5+ only, and this must serve every TFM
        private sealed class RawLengthComparer : IEqualityComparer<object>
        {
            internal static readonly RawLengthComparer Instance = new RawLengthComparer();
            private RawLengthComparer() { }
            bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);
            int IEqualityComparer<object>.GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct ObjectKey : IEquatable<ObjectKey>
        {
            private readonly object _obj;
            private readonly Type _subTypeLevel; // null means "root type" (from the perspective of the serializer)
            [MethodImpl(ProtoReader.HotPath)]
            public ObjectKey(object obj, Type subTypeLevel)
            {
                _obj = obj;
                _subTypeLevel = subTypeLevel;
            }
            public override string ToString() => $"{_subTypeLevel}/{_obj}";

            [MethodImpl(ProtoReader.HotPath)]
            public override int GetHashCode() => RuntimeHelpers.GetHashCode(_obj) ^ (_subTypeLevel?.GetHashCode() ?? 0);
            [MethodImpl(ProtoReader.HotPath)]
            public override bool Equals(object obj) => obj is ObjectKey key && Equals(key);
            [MethodImpl(ProtoReader.HotPath)]
            public bool Equals(ObjectKey other) => this._obj == other._obj & this._subTypeLevel == other._subTypeLevel;
        }

        int _hit, _miss;

        [MethodImpl(ProtoReader.HotPath)]
        public bool TryGetKnownLength(object obj, Type subTypeLevel, out long length)
        {
            if (_knownLengths.TryGetValue(new ObjectKey(obj, subTypeLevel), out length))
            {
                _hit++;
                return true;
            }
            else
            {
                _miss++;
                length = default;
                return false;
            }
        }

        public void SetKnownLength(object obj, Type subTypeLevel, long length)
        {
            var key = new ObjectKey(obj, subTypeLevel);
            _knownLengths[key] = length;
        }

#if FEAT_DYNAMIC_REF

        private List<object> underlyingList;

        private List<object> List => underlyingList ?? (underlyingList = new List<object>());

        internal const int Root = 0;
        internal object GetKeyedObject(int key)
        {
            if (key-- == Root)
            {
                if (rootObject is null) ThrowHelper.ThrowProtoException("No root object assigned");
                return rootObject;
            }
            var list = List;

            if (key < 0 || key >= list.Count)
            {
                Debug.WriteLine("Missing key: " + key);
                ThrowHelper.ThrowProtoException("Internal error; a missing key occurred");
            }

            object tmp = list[key];
            if (tmp is null)
            {
                ThrowHelper.ThrowProtoException("A deferred key does not have a value yet");
            }
            return tmp;
        }

        internal void SetKeyedObject(int key, object value)
        {
            if (key-- == Root)
            {
                if (value is null) ThrowHelper.ThrowArgumentNullException(nameof(value));
                if (rootObject is object && ((object)rootObject != (object)value)) ThrowHelper.ThrowProtoException("The root object cannot be reassigned");
                rootObject = value;
            }
            else
            {
                var list = List;
                if (key == list.Count)
                {
                    list.Add(value);
                }
                else if (key < list.Count)
                {
                    object oldVal = list[key];
                    if (oldVal is null)
                    {
                        list[key] = value;
                    }
                    else if (!ReferenceEquals(oldVal, value))
                    {
                        ThrowHelper.ThrowProtoException("Reference-tracked objects cannot change reference");
                    } // otherwise was the same; nothing to do
                }
                else
                {
                    ThrowHelper.ThrowProtoException("Internal error; a key mismatch occurred");
                }
            }
        }

        private object rootObject;
        internal int AddObjectKey(object value, out bool existing)
        {
            if (value is null) ThrowHelper.ThrowArgumentNullException(nameof(value));

            if ((object)value == (object)rootObject) // (object) here is no-op, but should be
            {                                        // preserved even if this was typed - needs ref-check
                existing = true;
                return Root;
            }

            string s = value as string;
            var list = List;
            int index;

            if (s is null)
            {
                if (objectKeys is null)
                {
                    objectKeys = new Dictionary<object, int>(ReferenceComparer.Default);
                    index = -1;
                }
                else
                {
                    if (!objectKeys.TryGetValue(value, out index)) index = -1;
                }
            }
            else
            {
                if (stringKeys is null)
                {
                    stringKeys = new Dictionary<string, int>();
                    index = -1;
                }
                else
                {
                    if (!stringKeys.TryGetValue(s, out index)) index = -1;
                }
            }

            if (!(existing = index >= 0))
            {
                index = list.Count;
                list.Add(value);
                if (s is null)
                {
                    objectKeys.Add(value, index);
                }
                else
                {
                    stringKeys.Add(s, index);
                }
            }
            return index + 1;
        }

        private int trapStartIndex; // defaults to 0 - optimization for RegisterTrappedObject
                                    // to make it faster at seeking to find deferred-objects

        internal void RegisterTrappedObject(object value)
        {
            if (rootObject is null)
            {
                rootObject = value;
            }
            else
            {
                if (underlyingList is object)
                {
                    for (int i = trapStartIndex; i < underlyingList.Count; i++)
                    {
                        trapStartIndex = i + 1; // things never *become* null; whether or
                                                // not the next item is null, it will never
                                                // need to be checked again

                        if (underlyingList[i] is null)
                        {
                            underlyingList[i] = value;
                            break;
                        }
                    }
                }
            }
        }

        private Dictionary<string, int> stringKeys;

        private System.Collections.Generic.Dictionary<object, int> objectKeys;
        internal sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public readonly static ReferenceComparer Default = new ReferenceComparer();
            private ReferenceComparer() { }

            bool IEqualityComparer<object>.Equals(object x, object y)
            {
                return x == y; // ref equality
            }

            int IEqualityComparer<object>.GetHashCode(object obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
#endif

        internal void Clear()
        {
#if FEAT_DYNAMIC_REF
            trapStartIndex = 0;
            rootObject = null;
            if (underlyingList is object) underlyingList.Clear();
            if (stringKeys is object) stringKeys.Clear();
            if (objectKeys is object) objectKeys.Clear();
#endif
            // RETAIN, BUT NOT FOREVER (notes/nano-writer.md). These writers are pooled, so
            // discarding capacity on every use meant re-growing a several-hundred-entry
            // dictionary from empty each time - measured at 22,392 B per serialize and 7-12%.
            // Retaining it unconditionally is the opposite mistake: one large graph left a
            // pooled writer holding 11.7 MB forever, ~10x the payload.
            //
            // So capacity is kept by default and handed back on either of two signals:
            //   - a gen2 collection since we last cleared, i.e. actual memory pressure. If no
            //     GC has run, memory is not scarce and retaining costs nothing; this is the
            //     cadence ArrayPool trims on, and GC.CollectionCount answers it with a static
            //     read - no finalizer, no registry, no allocation, every TFM;
            //   - a size that is not worth retaining regardless, so the single enormous graph
            //     is dropped on the spot rather than waiting for a gen2 that an idle process
            //     may not run for a long time.
            var gen2 = GC.CollectionCount(2);
            bool pressure = gen2 != _lastGen2;
            _lastGen2 = gen2;

            ClearAndMaybeTrim(ref _knownLengths, pressure, static () => new());
            ClearAndMaybeTrim(ref _rawLengths, pressure, static () => new(RawLengthComparer.Instance));
            _rawSlots.Trim(pressure, RetainedEntryCap);
            _hit = _miss = 0;
        }

        private int _lastGen2;

        /// <summary>Above this many entries the capacity is handed back immediately rather than
        /// retained: a cache this large is the "one enormous graph" case, and the whole point is
        /// not to leave a pooled writer holding it.</summary>
        private const int RetainedEntryCap = 1024;

        private static void ClearAndMaybeTrim<TKey, TValue>(ref Dictionary<TKey, TValue> map,
            bool pressure, Func<Dictionary<TKey, TValue>> create)
        {
            var count = map.Count;
            map.Clear();
            if (pressure || count > RetainedEntryCap)
            {
#if NET
                map.TrimExcess();
#else
                map = create(); // no TrimExcess down-level; a fresh instance releases it
#endif
            }
        }

        internal int LengthHits => _hit;
        internal int LengthMisses => _miss;

        /// <summary>
        /// Takes over another cache's measurements, for the measure-then-write hand-off.
        /// </summary>
        /// <remarks>
        /// EXCHANGES the dictionaries rather than copying them. The measured path measures a
        /// whole tree and then writes it, so a copy meant building every entry twice and
        /// allocating a second dictionary to hold them - measurably, exactly twice the
        /// allocation of a direct write on the descriptor corpus.
        /// <para>
        /// A swap, not a share: each cache still owns exactly one dictionary afterwards, so
        /// disposal and clearing behave as they always did. Sharing one instance between two
        /// writers would alias caches whose lifetimes merely happen to nest today.
        /// </para>
        /// <para>
        /// The source is left holding this cache's (empty) dictionaries, which is why serializing
        /// the same measurement twice stays correct: the second pass simply finds nothing cached
        /// and re-derives, exactly as an unmeasured write does. These are pure caches keyed by
        /// object identity, and a length is a length whoever computed it.
        /// </para>
        /// </remarks>
        internal void InitializeFrom(NetObjectCache obj)
        {
            if (obj is not null)
            {
                // plain temporaries rather than tuple deconstruction: net462 has no ValueTuple
                _knownLengths.Clear();
                var known = _knownLengths;
                _knownLengths = obj._knownLengths;
                obj._knownLengths = known;

                _rawLengths.Clear();
                var raw = _rawLengths;
                _rawLengths = obj._rawLengths;
                obj._rawLengths = raw;

                // same O(1) hand-off for the ordered buffer; a copy here once cost 22 KB and 11%
                _rawSlots.SwapWith(obj._rawSlots);
            }
        }

        internal void CopyBack(NetObjectCache obj)
        {
            if (obj is not null)
            {
                obj._hit += _hit;
                obj._miss += _miss;
            }
        }
    }
}
