// The raw writer surface and CollectionsMarshal.AsSpan are both net8-only here, and the point of
// this file is a v4-versus-v4 measurement on a modern runtime; the net472 leg of this project has
// nothing to say about it. Excluded from BenchmarkBaseline for the same reason DelimitedEncoding-
// Benchmarks is - it drives the compile-time model, which the 2.4.x baseline package has no notion of.
#if NET8_0_OR_GREATER
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Configs;
using ProtoBuf;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Node = Benchmark.DelimitedEncodingBenchmarks.LengthPrefixedNode;
using DNode = Benchmark.DelimitedEncodingBenchmarks.DelimitedNode;

namespace Benchmark
{
    /// <summary>
    /// Isolates the cost of HOW a measured sub-message length reaches its write site, holding
    /// everything else constant: same writer, same graph, same bytes.
    /// <list type="bullet">
    /// <item><c>Generated</c> - what the generator emits today: lengths memoised in
    /// <c>state.RawLengths</c>, a <c>Dictionary&lt;object, long&gt;</c> keyed by reference
    /// identity. Three dictionary operations per sub-message node (miss + insert while measuring,
    /// hit while writing).</item>
    /// <item><c>Ordered</c> - the same two passes, but the lengths ride an append-only
    /// <c>long[]</c> in visit order and are consumed by index. Measure and write walk the graph in
    /// the same order, so the index is all the correlation needed. Zero hashing.</item>
    /// <item><c>Delimited</c> - the floor: no length is needed at all.</item>
    /// </list>
    /// The gap between <c>Generated</c> and <c>Ordered</c> is the price of the dictionary.
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net80), MemoryDiagnoser]
    public class LengthCarrierBenchmarks
    {
        [Params(8, 64, 512)] public int Size { get; set; }

        private Node _wide, _deep, _alias;
        private DNode _wideDelimited, _deepDelimited, _aliasDelimited;
        private Bw _bw;
        private long[] _lengths = new long[1024];

        [GlobalSetup]
        public void Setup()
        {
            _bw = new Bw();
            DelimitedModel.Instance.MaxDepth = 1024;   // the deep chain is about framing, not the limit
            _wide = new Node { Value = 1, Children = new List<Node>() };
            _wideDelimited = new DNode { Value = 1, Children = new List<DNode>() };
            for (int i = 0; i < Size; i++)
            {
                _wide.Children.Add(new Node { Value = i + 1 });
                _wideDelimited.Children.Add(new DNode { Value = i + 1 });
            }
            _deep = new Node { Value = 1 };
            _deepDelimited = new DNode { Value = 1 };
            for (int i = 0; i < Size; i++)
            {
                _deep = new Node { Value = i + 2, Child = _deep };
                _deepDelimited = new DNode { Value = i + 2, Child = _deepDelimited };
            }
            // THE ALIASED CASE: one shared subtree instance, referenced Size times in parallel
            // (not recursively). This is where the dictionary's memoisation genuinely pays - it
            // measures the shared subtree once and hits the cache for every later occurrence -
            // and where an ordered array cannot, since it keys on position rather than identity.
            var shared = new Node { Value = 7, Children = new List<Node>() };
            var sharedD = new DNode { Value = 7, Children = new List<DNode>() };
            for (int i = 0; i < 8; i++)
            {
                shared.Children.Add(new Node { Value = i + 1 });
                sharedD.Children.Add(new DNode { Value = i + 1 });
            }
            _alias = new Node { Value = 1, Children = new List<Node>() };
            _aliasDelimited = new DNode { Value = 1, Children = new List<DNode>() };
            for (int i = 0; i < Size; i++)
            {
                _alias.Children.Add(shared);              // the SAME instance, every time
                _aliasDelimited.Children.Add(sharedD);
            }

            _lengths = new long[(Size * 12) + 32];

            // every route must produce the SAME BYTES, not merely the same count
            AssertSame(nameof(Generated), Bytes(ViaModel), Bytes(Generated));
            AssertSame(nameof(Ordered), Bytes(ViaModel), Bytes(Ordered));
            AssertSame(nameof(OrderedDeep), Bytes(ViaModelDeep), Bytes(OrderedDeep));
            AssertSame(nameof(GeneratedDeep), Bytes(ViaModelDeep), Bytes(GeneratedDeep));
            AssertSame(nameof(GeneratedAlias), Bytes(ViaModelAlias), Bytes(GeneratedAlias));
            AssertSame(nameof(OrderedAlias), Bytes(ViaModelAlias), Bytes(OrderedAlias));
            AssertSame(nameof(DictBaseline), Bytes(ViaModel), Bytes(DictBaseline));
            AssertSame(nameof(DictNoProbe), Bytes(ViaModel), Bytes(DictNoProbe));
            AssertSame(nameof(LazyOrdered), Bytes(ViaModel), Bytes(LazyOrdered));
            AssertSame(nameof(LazyOrderedDeep), Bytes(ViaModelDeep), Bytes(LazyOrderedDeep));
            AssertSame(nameof(LazyOrderedAlias), Bytes(ViaModelAlias), Bytes(LazyOrderedAlias));
        }

        private byte[] Bytes(System.Func<long> route)
        {
            route();
            return _bw.ToArray();
        }

        private static void AssertSame(string what, byte[] expected, byte[] actual)
        {
            if (expected.Length != actual.Length)
                throw new System.Exception($"{what}: {actual.Length} bytes, expected {expected.Length}");
            for (int i = 0; i < expected.Length; i++)
                if (expected[i] != actual[i])
                    throw new System.Exception($"{what}: byte {i} is {actual[i]:x2}, expected {expected[i]:x2}");
        }

        [Benchmark(Baseline = true)]
        public long ViaModel()
        {
            _bw.Reset();
            DelimitedModel.Instance.Serialize<Node>(_bw, _wide);
            return _bw.WrittenCount;
        }

        [Benchmark]
        public long Generated()
        {
            _bw.Reset();
            var state = ProtoWriter.State.Create(_bw, DelimitedModel.Instance);
            try
            {
                DelimitedModel.RawWritePrefixed(ref state, _wide);
                state.Close();
            }
            finally { state.Dispose(); }
            return _bw.WrittenCount;
        }

        [Benchmark]
        public long Ordered()
        {
            _bw.Reset();
            var state = ProtoWriter.State.Create(_bw, DelimitedModel.Instance);
            try
            {
                int n = 0;
                MeasureOrdered(_wide, state.RawDepthBudget, ref n);
                n = 0;
                WriteOrdered(ref state, _wide, state.RawDepthBudget, ref n);
                state.Close();
            }
            finally { state.Dispose(); }
            return _bw.WrittenCount;
        }

        [Benchmark]
        public long Delimited()
        {
            _bw.Reset();
            var state = ProtoWriter.State.Create(_bw, DelimitedModel.Instance);
            try
            {
                DelimitedModel.RawWriteDelimited(ref state, _wideDelimited);
                state.Close();
            }
            finally { state.Dispose(); }
            return _bw.WrittenCount;
        }

        [Benchmark, BenchmarkCategory("deep")]
        public long ViaModelDeep()
        {
            _bw.Reset();
            DelimitedModel.Instance.Serialize<Node>(_bw, _deep);
            return _bw.WrittenCount;
        }

        [Benchmark, BenchmarkCategory("deep")]
        public long GeneratedDeep()
        {
            _bw.Reset();
            var state = ProtoWriter.State.Create(_bw, DelimitedModel.Instance);
            try { DelimitedModel.RawWritePrefixed(ref state, _deep); state.Close(); }
            finally { state.Dispose(); }
            return _bw.WrittenCount;
        }

        [Benchmark, BenchmarkCategory("deep")]
        public long OrderedDeep()
        {
            _bw.Reset();
            var state = ProtoWriter.State.Create(_bw, DelimitedModel.Instance);
            try
            {
                int n = 0;
                MeasureOrdered(_deep, state.RawDepthBudget, ref n);
                n = 0;
                WriteOrdered(ref state, _deep, state.RawDepthBudget, ref n);
                state.Close();
            }
            finally { state.Dispose(); }
            return _bw.WrittenCount;
        }

        [Benchmark, BenchmarkCategory("deep")]
        public long DelimitedDeep()
        {
            _bw.Reset();
            var state = ProtoWriter.State.Create(_bw, DelimitedModel.Instance);
            try { DelimitedModel.RawWriteDelimited(ref state, _deepDelimited); state.Close(); }
            finally { state.Dispose(); }
            return _bw.WrittenCount;
        }

        [Benchmark, BenchmarkCategory("alias")]
        public long ViaModelAlias()
        {
            _bw.Reset();
            DelimitedModel.Instance.Serialize<Node>(_bw, _alias);
            return _bw.WrittenCount;
        }

        [Benchmark, BenchmarkCategory("alias")]
        public long GeneratedAlias()
        {
            _bw.Reset();
            var state = ProtoWriter.State.Create(_bw, DelimitedModel.Instance);
            try { DelimitedModel.RawWritePrefixed(ref state, _alias); state.Close(); }
            finally { state.Dispose(); }
            return _bw.WrittenCount;
        }

        [Benchmark, BenchmarkCategory("alias")]
        public long OrderedAlias()
        {
            _bw.Reset();
            var state = ProtoWriter.State.Create(_bw, DelimitedModel.Instance);
            try
            {
                int n = 0;
                MeasureOrdered(_alias, state.RawDepthBudget, ref n);
                n = 0;
                WriteOrdered(ref state, _alias, state.RawDepthBudget, ref n);
                state.Close();
            }
            finally { state.Dispose(); }
            return _bw.WrittenCount;
        }

        [Benchmark, BenchmarkCategory("alias")]
        public long DelimitedAlias()
        {
            _bw.Reset();
            var state = ProtoWriter.State.Create(_bw, DelimitedModel.Instance);
            try { DelimitedModel.RawWriteDelimited(ref state, _aliasDelimited); state.Close(); }
            finally { state.Dispose(); }
            return _bw.WrittenCount;
        }

        /// <summary>
        /// A hand-written copy of exactly what the generator emits today - three dictionary
        /// operations per node. Exists to show the hand-written harness reproduces
        /// <see cref="Generated"/>, so the delta to the variants below is trustworthy.
        /// </summary>
        [Benchmark, BenchmarkCategory("wide")]
        public long DictBaseline()
        {
            _bw.Reset();
            var state = ProtoWriter.State.Create(_bw, DelimitedModel.Instance);
            try
            {
                MeasureDict(_wide, state.RawDepthBudget, state.RawLengths, probe: true);
                WriteDict(ref state, _wide, state.RawDepthBudget);
                state.Close();
            }
            finally { state.Dispose(); }
            return _bw.WrittenCount;
        }

        /// <summary>
        /// The same, minus the measure-side PROBE: measure unconditionally and store, so a node
        /// costs one insert plus one write-site lookup instead of miss + insert + lookup. Needs no
        /// framework API, so it carries no TFM condition. The only thing given up is dedup of an
        /// aliased subtree, which <see cref="OrderedAlias"/> shows is worth little.
        /// </summary>
        [Benchmark, BenchmarkCategory("wide")]
        public long DictNoProbe()
        {
            _bw.Reset();
            var state = ProtoWriter.State.Create(_bw, DelimitedModel.Instance);
            try
            {
                MeasureDict(_wide, state.RawDepthBudget, state.RawLengths, probe: false);
                WriteDict(ref state, _wide, state.RawDepthBudget);
                state.Close();
            }
            finally { state.Dispose(); }
            return _bw.WrittenCount;
        }

        private static long MeasureDict(Node value, int depth,
            Dictionary<object, long> lengths, bool probe)
        {
            if (--depth < 0) ProtoWriter.State.ThrowRawTooDeep();
            long len = 0, sub;
            var tmp1 = value.Value;
            if (tmp1 != 0) len += 1 + ProtoWriter.State.MeasureRawVarint64(unchecked((ulong)(long)tmp1));
            var tmp2 = value.Child;
            if (tmp2 != null)
            {
                if (probe)
                {
                    if (!lengths.TryGetValue(tmp2, out sub))
                    { sub = MeasureDict(tmp2, depth, lengths, probe); lengths[tmp2] = sub; }
                }
                else { sub = MeasureDict(tmp2, depth, lengths, probe); lengths[tmp2] = sub; }
                len += 1 + ProtoWriter.State.MeasureRawVarint64((ulong)sub) + sub;
            }
            var tmp3 = value.Children;
            if (tmp3 != null)
            {
                foreach (var item3 in CollectionsMarshal.AsSpan(tmp3))
                {
                    if (item3 is null) ProtoWriter.State.ThrowNullRepeatedContents<Node>();
                    if (probe)
                    {
                        if (!lengths.TryGetValue(item3, out sub))
                        { sub = MeasureDict(item3, depth, lengths, probe); lengths[item3] = sub; }
                    }
                    else { sub = MeasureDict(item3, depth, lengths, probe); lengths[item3] = sub; }
                    len += 1 + ProtoWriter.State.MeasureRawVarint64((ulong)sub) + sub;
                }
            }
            return len;
        }

        private static void WriteDict(ref ProtoWriter.State state, Node value, int depth)
        {
            if (--depth < 0) ProtoWriter.State.ThrowRawTooDeep();
            ProtoBuf.Meta.TypeModel.ThrowUnexpectedSubtype(value);
            var tmp1 = value.Value;
            if (tmp1 != 0)
            {
                state.WriteRawTag((1 << 3) | 0);
                state.WriteRawVarint64(unchecked((ulong)(long)tmp1));
            }
            var tmp2 = value.Child;
            if (tmp2 != null)
            {
                state.WriteRawTag((2 << 3) | 2);
                state.RawLengths.TryGetValue(tmp2, out var len);
                state.WriteRawVarint64((ulong)len);
                WriteDict(ref state, tmp2, depth);
            }
            var tmp3 = value.Children;
            if (tmp3 != null)
            {
                foreach (var item3 in CollectionsMarshal.AsSpan(tmp3))
                {
                    if (item3 is null) ProtoWriter.State.ThrowNullRepeatedContents<Node>();
                    state.WriteRawTag((3 << 3) | 2);
                    state.RawLengths.TryGetValue(item3, out var len);
                    state.WriteRawVarint64((ulong)len);
                    WriteDict(ref state, item3, depth);
                }
            }
        }

        /// <summary>
        /// The ordered array kept in the generator's existing <b>lazy</b> shape: no separate
        /// eager pass, the measure is still triggered from the write site. The convention that
        /// makes it work is that <c>MeasureLazy</c> claims a slot for <b>itself</b> first, so a
        /// subtree occupies <c>[self, descendants...]</c> and the write consumes in the same order.
        /// The write site marks the cursor, measures (which fills the run), rewinds, and reads.
        /// </summary>
        [Benchmark, BenchmarkCategory("wide")]
        public long LazyOrdered()
        {
            _bw.Reset();
            var state = ProtoWriter.State.Create(_bw, DelimitedModel.Instance);
            try
            {
                int cursor = 0;
                WriteLazy(ref state, _wide, state.RawDepthBudget, ref cursor);
                state.Close();
            }
            finally { state.Dispose(); }
            return _bw.WrittenCount;
        }

        [Benchmark, BenchmarkCategory("deep")]
        public long LazyOrderedDeep()
        {
            _bw.Reset();
            var state = ProtoWriter.State.Create(_bw, DelimitedModel.Instance);
            try
            {
                int cursor = 0;
                WriteLazy(ref state, _deep, state.RawDepthBudget, ref cursor);
                state.Close();
            }
            finally { state.Dispose(); }
            return _bw.WrittenCount;
        }

        [Benchmark, BenchmarkCategory("alias")]
        public long LazyOrderedAlias()
        {
            _bw.Reset();
            var state = ProtoWriter.State.Create(_bw, DelimitedModel.Instance);
            try
            {
                int cursor = 0;
                WriteLazy(ref state, _alias, state.RawDepthBudget, ref cursor);
                state.Close();
            }
            finally { state.Dispose(); }
            return _bw.WrittenCount;
        }

        /// <summary>Claims slot <c>cursor</c> for <paramref name="value"/>, then its descendants.</summary>
        private long MeasureLazy(Node value, int depth, ref int cursor)
        {
            if (--depth < 0) ProtoWriter.State.ThrowRawTooDeep();
            int self = cursor++;
            if (self >= _lengths.Length) System.Array.Resize(ref _lengths, _lengths.Length * 2);
            long len = 0;
            var tmp1 = value.Value;
            if (tmp1 != 0) len += 1 + ProtoWriter.State.MeasureRawVarint64(unchecked((ulong)(long)tmp1));
            var tmp2 = value.Child;
            if (tmp2 != null)
            {
                var sub = MeasureLazy(tmp2, depth, ref cursor);
                len += 1 + ProtoWriter.State.MeasureRawVarint64((ulong)sub) + sub;
            }
            var tmp3 = value.Children;
            if (tmp3 != null)
            {
                foreach (var item3 in CollectionsMarshal.AsSpan(tmp3))
                {
                    if (item3 is null) ProtoWriter.State.ThrowNullRepeatedContents<Node>();
                    var sub = MeasureLazy(item3, depth, ref cursor);
                    len += 1 + ProtoWriter.State.MeasureRawVarint64((ulong)sub) + sub;
                }
            }
            _lengths[self] = len;
            return len;
        }

        private void WriteLazy(ref ProtoWriter.State state, Node value, int depth, ref int cursor)
        {
            if (--depth < 0) ProtoWriter.State.ThrowRawTooDeep();
            ProtoBuf.Meta.TypeModel.ThrowUnexpectedSubtype(value);
            var tmp1 = value.Value;
            if (tmp1 != 0)
            {
                state.WriteRawTag((1 << 3) | 0);
                state.WriteRawVarint64(unchecked((ulong)(long)tmp1));
            }
            var tmp2 = value.Child;
            if (tmp2 != null)
            {
                state.WriteRawTag((2 << 3) | 2);
                int mark = cursor;
                MeasureLazy(tmp2, depth, ref cursor);   // fills [mark, ...]
                cursor = mark;                          // rewind and consume the same run
                state.WriteRawVarint64((ulong)_lengths[cursor++]);
                WriteLazy(ref state, tmp2, depth, ref cursor);
            }
            var tmp3 = value.Children;
            if (tmp3 != null)
            {
                foreach (var item3 in CollectionsMarshal.AsSpan(tmp3))
                {
                    if (item3 is null) ProtoWriter.State.ThrowNullRepeatedContents<Node>();
                    state.WriteRawTag((3 << 3) | 2);
                    int mark = cursor;
                    MeasureLazy(item3, depth, ref cursor);
                    cursor = mark;
                    state.WriteRawVarint64((ulong)_lengths[cursor++]);
                    WriteLazy(ref state, item3, depth, ref cursor);
                }
            }
        }

        // --- the ordered-array variant: a line-for-line copy of the generated pair, with the
        // --- dictionary probe replaced by a post-order slot in visit order

        private long MeasureOrdered(Node value, int depth, ref int next)
        {
            if (--depth < 0) ProtoWriter.State.ThrowRawTooDeep();
            long len = 0;
            var tmp1 = value.Value;
            if (tmp1 != 0) len += 1 + ProtoWriter.State.MeasureRawVarint64(unchecked((ulong)(long)tmp1));
            var tmp2 = value.Child;
            if (tmp2 != null)
            {
                int slot = next++;
                var sub = MeasureOrdered(tmp2, depth, ref next);
                _lengths[slot] = sub;
                len += 1 + ProtoWriter.State.MeasureRawVarint64((ulong)sub) + sub;
            }
            var tmp3 = value.Children;
            if (tmp3 != null)
            {
                foreach (var item3 in CollectionsMarshal.AsSpan(tmp3))
                {
                    if (item3 is null) ProtoWriter.State.ThrowNullRepeatedContents<Node>();
                    int slot = next++;
                    var sub = MeasureOrdered(item3, depth, ref next);
                    _lengths[slot] = sub;
                    len += 1 + ProtoWriter.State.MeasureRawVarint64((ulong)sub) + sub;
                }
            }
            return len;
        }

        private void WriteOrdered(ref ProtoWriter.State state, Node value, int depth, ref int next)
        {
            if (--depth < 0) ProtoWriter.State.ThrowRawTooDeep();
            ProtoBuf.Meta.TypeModel.ThrowUnexpectedSubtype(value);
            var tmp1 = value.Value;
            if (tmp1 != 0)
            {
                state.WriteRawTag((1 << 3) | 0);
                state.WriteRawVarint64(unchecked((ulong)(long)tmp1));
            }
            var tmp2 = value.Child;
            if (tmp2 != null)
            {
                state.WriteRawTag((2 << 3) | 2);
                state.WriteRawVarint64((ulong)_lengths[next++]);
                WriteOrdered(ref state, tmp2, depth, ref next);
            }
            var tmp3 = value.Children;
            if (tmp3 != null)
            {
                foreach (var item3 in CollectionsMarshal.AsSpan(tmp3))
                {
                    if (item3 is null) ProtoWriter.State.ThrowNullRepeatedContents<Node>();
                    state.WriteRawTag((3 << 3) | 2);
                    state.WriteRawVarint64((ulong)_lengths[next++]);
                    WriteOrdered(ref state, item3, depth, ref next);
                }
            }
        }
    }

    /// <summary>
    /// The generated services type is <c>private</c> inside the model, so the two raw entry points
    /// are surfaced here - from inside the same partial - rather than by widening the generator.
    /// </summary>
    public partial class DelimitedModel
    {
        internal static void RawWritePrefixed(ref ProtoWriter.State state, Node value)
            => ProtoBufGeneratedServices.RawWrite_Benchmark_DelimitedEncodingBenchmarks_LengthPrefixedNode(
                ref state, value, state.RawDepthBudget);

        internal static void RawWriteDelimited(ref ProtoWriter.State state, DNode value)
            => ProtoBufGeneratedServices.RawWrite_Benchmark_DelimitedEncodingBenchmarks_DelimitedNode(
                ref state, value, state.RawDepthBudget);
    }

    /// <summary>A reusable single-array <see cref="IBufferWriter{T}"/>, so the target costs nothing.</summary>
    internal sealed class Bw : IBufferWriter<byte>
    {
        private byte[] _buffer = new byte[256 * 1024];
        public int WrittenCount { get; private set; }
        public void Reset() => WrittenCount = 0;
        public void Advance(int count) => WrittenCount += count;
        public System.Memory<byte> GetMemory(int sizeHint = 0) => new(_buffer, WrittenCount, _buffer.Length - WrittenCount);
        public System.Span<byte> GetSpan(int sizeHint = 0) => new(_buffer, WrittenCount, _buffer.Length - WrittenCount);
        public byte[] ToArray() { var r = new byte[WrittenCount]; System.Array.Copy(_buffer, r, WrittenCount); return r; }
    }
}
#endif
