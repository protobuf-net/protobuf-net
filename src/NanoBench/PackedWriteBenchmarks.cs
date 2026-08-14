using BenchmarkDotNet.Attributes;
using ProtoBuf;
using System;
using System.Buffers;

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// <b>Writing</b> a packed varint column — gap B21 tier 1, isolated from the model.
/// </summary>
/// <remarks>
/// <para>
/// This exists because <c>PackedMatrixBenchmarks</c> <b>could not resolve the question</b>. That
/// harness serializes a four-member contract end to end, where each member carries roughly a
/// microsecond of per-member overhead (gaps.md, item 3) — so a change to the element loop arrives
/// diluted, and the observed run-to-run spread on a single arm (7.26–7.84 µs for the same code)
/// was <i>larger than the effect being claimed</i>. Two successive runs put the same arm on
/// opposite sides of its baseline. An end-to-end number is the right final check and the wrong
/// instrument for attributing a delta to one loop.
/// </para>
/// <para>
/// So this measures the primitive directly, against the scalar loop it replaces, on the two
/// distributions that decide whether tier 1 fires at all:
/// </para>
/// <list type="bullet">
/// <item><description><c>small</c> — every value under 128, so every block is uniform and the
/// blit always fires. This is the <b>upper bound</b> on the win, and the census says real columns
/// look like this.</description></item>
/// <item><description><c>spread</c> — a quarter of values in each width class, so a block of 32 is
/// essentially never uniform and the check <b>always fails</b>. This is the cost side, and the
/// number that decides whether tier 1 is safe to enable unconditionally.</description></item>
/// <item><description><c>oneWide</c> — small except for a single wide value near the end: the
/// pathological case for a block-at-a-time check, since the scan pays for itself repeatedly and
/// then bails. Included because "small" and "spread" between them would let this hide.</description></item>
/// </list>
/// <para>
/// Writes go to an <see cref="IBufferWriter{T}"/> that hands back the same buffer every time, so
/// the measurement is the encoding rather than allocation or growth.
/// </para>
/// </remarks>
[MemoryDiagnoser(false)]
public class PackedWriteBenchmarks
{
    [Params(999)]
    public int Count { get; set; }

    [Params("small", "spread", "oneWide")]
    public string Distribution { get; set; } = "small";

    private uint[] _values = [];
    private int[] _signed = [];
    private ulong[] _wide = [];
    private readonly Sink _sink = new();

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(12345);
        _values = new uint[Count];
        _signed = new int[Count];
        _wide = new ulong[Count];
        for (int i = 0; i < Count; i++)
        {
            _values[i] = Distribution switch
            {
                "small" => (uint)rand.Next(0, 128),
                "oneWide" => i == Count - 3 ? uint.MaxValue : (uint)rand.Next(0, 128),
                _ => (i & 3) switch
                {
                    0 => (uint)rand.Next(0, 128),
                    1 => (uint)rand.Next(128, 1 << 14),
                    2 => (uint)rand.Next(1 << 14, 1 << 21),
                    _ => (uint)rand.Next(1 << 21, int.MaxValue),
                },
            };
            _signed[i] = (int)(_values[i] & 0x7FFFFFFF);
            // widened, not re-drawn: the 64-bit arm then sees the SAME width classes,
            // so its number is comparable with the 32-bit one rather than a fresh draw.
            _wide[i] = Distribution == "spread" ? ((ulong)_values[i] << 20) | _values[i] : _values[i];
        }
    }

    /// <summary>The loop tier 1 replaces: one <c>WriteRawVarint32</c> per element.</summary>
    [Benchmark(Baseline = true)]
    public long ScalarUInt32()
    {
        var state = ProtoWriter.State.Create(_sink, null);
        try
        {
            foreach (var value in _values) state.WriteRawVarint32(value);
            state.Flush();
            return state.GetPosition();
        }
        finally { state.Dispose(); _sink.Reset(); }
    }

    /// <summary>Tier 1: a uniform block of single-byte values narrows and blits.</summary>
    [Benchmark]
    public long BlockUInt32()
    {
        var state = ProtoWriter.State.Create(_sink, null);
        try
        {
            ProtoBuf.Internal.PackedVarintMeasure.WritePackedUInt32(ref state, _values);
            state.Flush();
            return state.GetPosition();
        }
        finally { state.Dispose(); _sink.Reset(); }
    }

    [Benchmark]
    public long ScalarUInt64()
    {
        var state = ProtoWriter.State.Create(_sink, null);
        try
        {
            foreach (var value in _wide) state.WriteRawVarint64(value);
            state.Flush();
            return state.GetPosition();
        }
        finally { state.Dispose(); _sink.Reset(); }
    }

    /// <summary>Three narrowing steps rather than two, so a block is eight vectors.</summary>
    [Benchmark]
    public long BlockUInt64()
    {
        var state = ProtoWriter.State.Create(_sink, null);
        try
        {
            ProtoBuf.Internal.PackedVarintMeasure.WritePackedUInt64(ref state, _wide);
            state.Flush();
            return state.GetPosition();
        }
        finally { state.Dispose(); _sink.Reset(); }
    }

    [Benchmark]
    public long ScalarInt32()
    {
        var state = ProtoWriter.State.Create(_sink, null);
        try
        {
            foreach (var value in _signed) state.WriteRawVarint64(unchecked((ulong)(long)value));
            state.Flush();
            return state.GetPosition();
        }
        finally { state.Dispose(); _sink.Reset(); }
    }

    [Benchmark]
    public long BlockInt32()
    {
        var state = ProtoWriter.State.Create(_sink, null);
        try
        {
            ProtoBuf.Internal.PackedVarintMeasure.WritePackedInt32(ref state, _signed);
            state.Flush();
            return state.GetPosition();
        }
        finally { state.Dispose(); _sink.Reset(); }
    }

    /// <summary>
    /// Hands back the same oversized buffer every time — the point is to measure encoding, not
    /// the buffer writer. Not <c>ArrayBufferWriter&lt;T&gt;</c>, which is net5+ while this project
    /// also targets net472.
    /// </summary>
    private sealed class Sink : IBufferWriter<byte>
    {
        private readonly byte[] _buffer = new byte[1024 * 64];
        private int _written;
        public void Reset() => _written = 0;
        public void Advance(int count) => _written += count;
        public Memory<byte> GetMemory(int sizeHint = 0) => new(_buffer, _written, _buffer.Length - _written);
        public Span<byte> GetSpan(int sizeHint = 0) => new(_buffer, _written, _buffer.Length - _written);
    }
}
