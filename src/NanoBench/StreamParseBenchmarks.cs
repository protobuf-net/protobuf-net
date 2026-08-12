using BenchmarkDotNet.Attributes;
using ProtoBuf;
using System;
using System.Buffers;
using System.IO;
using Model = ProtoBuf.Nano.Bench.DescriptorModel;
using Pbn = Google.Protobuf.Reflection; // protobuf-net.Reflection's DTOs (legacy row)

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// The buffer-model brick, measured: the descriptor composite payload parsed from a Stream and
/// from multi-segment sequences, nano vs legacy's real Stream backend. The stream is a
/// non-MemoryStream chunk-feeding wrapper BY DESIGN - both stacks special-case MemoryStream and
/// collapse to their span paths (legacy even reaches the private buffer by reflection), so a
/// MemoryStream-fed "stream" benchmark measures no streaming at all (Marc's like-for-like catch;
/// see docs/nano-core.md). Memory rows are the control: what the refill machinery costs when it
/// never fires.
///
/// GlobalSetup is the correctness battery for the refill core, all census-gated against the
/// single-segment parse:
///  - streams at chunk sizes 1 / 7 / 4096 / 65536, hinted and unhinted (unbounded root);
///  - the descriptor payload as a two-segment sequence SPLIT AT EVERY BYTE OFFSET - every
///    straddle case for every wire construct in the document;
///  - the all-single-byte-segments sequence (the pathological case);
///  - legacy parsing from the same chunked stream (cross-stack agreement).
/// </summary>
[MemoryDiagnoser]
public class StreamParseBenchmarks
{
    /// <summary>Max bytes handed over per Stream.Read call.</summary>
    [Params(4096, 65536)]
    public int Chunk = 4096;

    private byte[] _data = [];
    private string _census = "";

    [GlobalSetup]
    public void Setup()
    {
        _data = DescriptorParseBenchmarks.BuildPayload();
        _census = DescriptorParseBenchmarks.CensusNano(ParseNanoMemory());

        // streams: brutal-to-comfortable chunk sizes, hinted and unhinted roots
        foreach (var chunk in new[] { 1, 7, 4096, 65536 })
        {
            Gate(DescriptorParseBenchmarks.CensusNano(ParseNanoStream(new ChunkedStream(_data, chunk), hint: -1)),
                $"nano stream chunk={chunk} unhinted");
            Gate(DescriptorParseBenchmarks.CensusNano(ParseNanoStream(new ChunkedStream(_data, chunk), hint: _data.Length)),
                $"nano stream chunk={chunk} hinted");
        }

        // the split sweep: every straddle case for every construct in the document
        for (int split = 1; split < _data.Length; split++)
        {
            var seq = TwoSegments(_data, split);
            var state = new ReaderState(in seq);
            try
            {
                var parsed = Model.DescriptorNanoReader.ReadFileDescriptorSet(ref state, null);
                if (DescriptorParseBenchmarks.CensusNano(parsed) != _census)
                {
                    throw new InvalidOperationException($"split sweep disagreement at offset {split}");
                }
            }
            finally
            {
                state.Dispose();
            }
        }

        // the pathological sequence: one segment per byte
        {
            var seq = SingleByteSegments(_data);
            var state = new ReaderState(in seq);
            try
            {
                Gate(DescriptorParseBenchmarks.CensusNano(
                    Model.DescriptorNanoReader.ReadFileDescriptorSet(ref state, null)),
                    "all-single-byte segments");
            }
            finally
            {
                state.Dispose();
            }
        }

        // cross-stack: legacy from the same chunked stream
        Gate(DescriptorParseBenchmarks.CensusLegacy(ParseLegacyStream(new ChunkedStream(_data, 4096))),
            "legacy stream");

        // the raw scalar family the descriptor schema never exercises: zigzag, float, double -
        // checked resident AND through a 1-byte-chunk stream (the fixed straddle arms)
        RawScalarGate(bytes => new ReaderState(bytes, 0, bytes.Length), "resident");
        RawScalarGate(bytes => new ReaderState(new ChunkedStream(bytes, 1)), "1-byte chunks");

        Console.WriteLine($"// gates green over {_data.Length}-byte payload ({_data.Length - 1} splits); census {_census}");

        void Gate(string actual, string name)
        {
            if (actual != _census)
            {
                throw new InvalidOperationException($"{name} disagreement:\n{actual}\nvs\n{_census}");
            }
        }
    }

    [Benchmark(Baseline = true)]
    public object LegacyStream() => ParseLegacyStream(new ChunkedStream(_data, Chunk));

    private Pbn.FileDescriptorSet ParseLegacyStream(Stream stream)
        => Serializer.Deserialize<Pbn.FileDescriptorSet>(stream); // the standard public entry

    [Benchmark]
    public object NanoStream() => ParseNanoStream(new ChunkedStream(_data, Chunk), hint: -1);

    private Model.FileDescriptorSet ParseNanoStream(Stream stream, long hint)
    {
        var state = new ReaderState(stream, hint);
        try
        {
            return Model.DescriptorNanoReader.ReadFileDescriptorSet(ref state, null);
        }
        finally
        {
            state.Dispose();
        }
    }

    // the controls: identical payload, no refills ever fire
    [Benchmark]
    public object LegacyMemory()
        => Serializer.Deserialize<Pbn.FileDescriptorSet>(new ReadOnlyMemory<byte>(_data));

    [Benchmark]
    public object NanoMemory() => ParseNanoMemory();

    private Model.FileDescriptorSet ParseNanoMemory()
    {
        var state = new ReaderState(_data, 0, _data.Length);
        try
        {
            return Model.DescriptorNanoReader.ReadFileDescriptorSet(ref state, null);
        }
        finally
        {
            state.Dispose();
        }
    }

    private delegate ReaderState StateFactory(byte[] bytes);

    private static void RawScalarGate(StateFactory factory, string name)
    {
        // zigzag(-1)=1, zigzag(1)=2, zigzag(int.MinValue)=0xFFFFFFFF; then 1.5f and -2.75
        var payload = new System.IO.MemoryStream();
        void Varint(ulong v)
        {
            while (v >= 0x80) { payload.WriteByte((byte)(v | 0x80)); v >>= 7; }
            payload.WriteByte((byte)v);
        }
        Varint(1);            // zigzag32 -> -1
        Varint(0xFFFFFFFFuL); // zigzag32 -> int.MinValue
        Varint(2);            // zigzag64 -> 1
        Varint(0xFFFFFFFFFFFFFFFFuL); // zigzag64 -> long.MinValue
        payload.Write(BitConverter.GetBytes(1.5f), 0, 4);
        payload.Write(BitConverter.GetBytes(-2.75d), 0, 8);
        var bytes = payload.ToArray();

        var state = factory(bytes);
        try
        {
            if (state.ReadRawZigZag32() != -1
                || state.ReadRawZigZag32() != int.MinValue
                || state.ReadRawZigZag64() != 1
                || state.ReadRawZigZag64() != long.MinValue
                || state.ReadRawSingle() != 1.5f
                || state.ReadRawDouble() != -2.75d)
            {
                throw new InvalidOperationException($"raw scalar gate failed ({name})");
            }
        }
        finally
        {
            state.Dispose();
        }
    }

    // ---------------------------------------------------------------- plumbing

    /// <summary>Non-seekable read-only stream returning at most maxChunk bytes per Read - the
    /// unwrap-defeating wrapper: it is deliberately NOT a MemoryStream.</summary>
    private sealed class ChunkedStream : Stream
    {
        private readonly byte[] _data;
        private readonly int _maxChunk;
        private int _position;

        public ChunkedStream(byte[] data, int maxChunk)
        {
            _data = data;
            _maxChunk = maxChunk;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int take = Math.Min(Math.Min(count, _maxChunk), _data.Length - _position);
            if (take <= 0) return 0;
            Buffer.BlockCopy(_data, _position, buffer, offset, take);
            _position += take;
            return take;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory, Segment previous)
        {
            Memory = memory;
            if (previous is not null)
            {
                previous.Next = this;
                RunningIndex = previous.RunningIndex + previous.Memory.Length;
            }
        }
    }

    private static ReadOnlySequence<byte> TwoSegments(byte[] data, int split)
    {
        var first = new Segment(new ReadOnlyMemory<byte>(data, 0, split), null);
        var second = new Segment(new ReadOnlyMemory<byte>(data, split, data.Length - split), first);
        return new ReadOnlySequence<byte>(first, 0, second, second.Memory.Length);
    }

    private static ReadOnlySequence<byte> SingleByteSegments(byte[] data)
    {
        var first = new Segment(new ReadOnlyMemory<byte>(data, 0, 1), null);
        var previous = first;
        for (int i = 1; i < data.Length; i++)
        {
            previous = new Segment(new ReadOnlyMemory<byte>(data, i, 1), previous);
        }
        return new ReadOnlySequence<byte>(first, 0, previous, previous.Memory.Length);
    }
}
