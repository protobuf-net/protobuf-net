extern alias gpb;

using BenchmarkDotNet.Attributes;
using ProtoBuf;
using System;
using System.Buffers;
using System.Linq;
using Model = ProtoBuf.Nano.Bench.DescriptorModel;
using ReaderState = ProtoBuf.ProtoReader.State;
using Pbn = Google.Protobuf.Reflection;          // protobuf-net.Reflection's DTOs (legacy row)
using Gpb = gpb::Google.Protobuf.Reflection;     // the Google.Protobuf package (home-turf row)
using GpbExt = gpb::Google.Protobuf.MessageExtensions;

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// The serialize composite again, but against <see cref="IBufferWriter{T}"/> rather than a
/// stream - which is the backend the buffer core is really about, and which nothing in this
/// rig measured until now (docs/nano-writer.md: "whatever it does not cover is not fine, it
/// is unmeasured"). The two backends are genuinely different engines: the stream writer owns
/// a byte[] and back-fills length prefixes into it, while the buffer-writer leases chunks
/// from the consumer and is forwards-only, so it is the one that measure-first was designed
/// for and the one the presized lease will size.
///
/// Rows mirror <see cref="DescriptorSerializeBenchmarks"/> exactly, minus the measure row
/// (which has no backend and is priced there). Do NOT compare these means against that
/// class's: the destination differs, so only the within-class ratios mean anything.
/// </summary>
[MemoryDiagnoser]
public class DescriptorSerializeBufferWriterBenchmarks
{
    private byte[] _data = [];
    private Pbn.FileDescriptorSet _pbnSet = null!;
    private Gpb.FileDescriptorSet _gpbSet = null!;
    private Model.FileDescriptorSet _nanoSet = null!;
    private ReusableBufferWriter _bw = null!;
    private Meta.TypeModel _protogenModel = null!;

    /// <summary>
    /// One pre-sized region, reset per invoke: the point here is to price the WRITER, so the
    /// destination must not allocate or grow inside the measured window. Hints are honoured
    /// by handing back everything that is left, which is what a real pooled buffer-writer
    /// does - and means the backend leases few, large chunks rather than one per member.
    /// </summary>
    private sealed class ReusableBufferWriter : IBufferWriter<byte>
    {
        private byte[] _buffer;
        private int _index;

        public ReusableBufferWriter(int capacity) => _buffer = new byte[capacity];

        public void Reset() => _index = 0;
        public byte[] ToArray() => new ReadOnlySpan<byte>(_buffer, 0, _index).ToArray();

        public void Advance(int count) => _index += count;

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            Ensure(sizeHint);
            return new Memory<byte>(_buffer, _index, _buffer.Length - _index);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            Ensure(sizeHint);
            return new Span<byte>(_buffer, _index, _buffer.Length - _index);
        }

        private void Ensure(int sizeHint)
        {
            if (sizeHint <= 0) sizeHint = 1;
            if (_buffer.Length - _index < sizeHint)
            {
                Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, _index + sizeHint));
            }
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        _data = DescriptorParseBenchmarks.BuildPayload();
        _bw = new ReusableBufferWriter((_data.Length * 2) + 4096);

        _pbnSet = Serializer.Deserialize<Pbn.FileDescriptorSet>(new ReadOnlyMemory<byte>(_data));
        _gpbSet = Gpb.FileDescriptorSet.Parser.ParseFrom(_data);
        var state = new ReaderState(_data, 0, _data.Length);
        try
        {
            _nanoSet = DescriptorParseBenchmarks.s_generatedRead(ref state, null);
        }
        finally
        {
            state.Dispose();
        }

        var protogenType = typeof(Pbn.FileDescriptorSet).Assembly
            .GetType("ProtoBuf.Reflection.Internal.CustomProtogenSerializer")
            ?? throw new InvalidOperationException("CustomProtogenSerializer not found");
        _protogenModel = (Meta.TypeModel)(protogenType
            .GetProperty("Instance")?.GetValue(null)
            ?? throw new InvalidOperationException("CustomProtogenSerializer.Instance not found"));

        // the same gates the stream rig applies: both Reflection-DTO rows must be byte-identical
        // to the payload, and every row must at least agree by census through a legacy reparse
        var legacy = Run(LegacyReal);
        if (!legacy.SequenceEqual(_data))
        {
            throw new InvalidOperationException("legacy buffer-writer serialize does not round-trip its own payload");
        }
        var protogen = Run(GeneratedProtogen);
        if (!protogen.SequenceEqual(_data))
        {
            throw new InvalidOperationException(
                $"generated protogen model output differs from legacy ({protogen.Length} vs {_data.Length} bytes)");
        }
        CensusGate(Run(NanoGenerated), "nano-generated");
        CensusGate(Run(GoogleProtobuf), "google");

        // the two backends must also agree with EACH OTHER, which nothing checked before: the
        // stream writer back-fills its length prefixes, the buffer-writer measures them ahead
        var viaStream = new System.IO.MemoryStream();
        Model.NanoDescriptorModel.Instance.Serialize(viaStream, _nanoSet);
        _bw.Reset();
        Model.NanoDescriptorModel.Instance.Serialize(_bw, _nanoSet);
        if (!viaStream.ToArray().SequenceEqual(_bw.ToArray()))
        {
            throw new InvalidOperationException("stream and buffer-writer backends disagree on the bytes");
        }
        Console.WriteLine($"// payload {_data.Length} bytes; stream and buffer-writer backends agree");

        byte[] Run(Func<object> row)
        {
            _bw.Reset();
            row();
            return _bw.ToArray();
        }
    }

    private void CensusGate(byte[] payload, string name)
    {
        var reparsed = Serializer.Deserialize<Pbn.FileDescriptorSet>(new ReadOnlyMemory<byte>(payload));
        var census = DescriptorParseBenchmarks.CensusLegacy(reparsed);
        var expected = DescriptorParseBenchmarks.CensusLegacy(_pbnSet);
        if (census != expected)
        {
            throw new InvalidOperationException($"census disagreement ({name}):\n{census}\nvs\n{expected}");
        }
    }

    [Benchmark(Baseline = true)]
    public object LegacyReal()
    {
        _bw.Reset();
        Meta.RuntimeTypeModel.Default.Serialize(_bw, _pbnSet);
        return _bw;
    }

    [Benchmark]
    public object GeneratedProtogen()
    {
        _bw.Reset();
        _protogenModel.Serialize(_bw, _pbnSet);
        return _bw;
    }

    [Benchmark]
    public object NanoGenerated()
    {
        _bw.Reset();
        Model.NanoDescriptorModel.Instance.Serialize(_bw, _nanoSet);
        return _bw;
    }

    /// <summary>
    /// The measured path: measure once, then write. It is the only route that knows the total
    /// before writing, so it is the only one where the presized lease fires today - which makes
    /// this row the price of "exact buffer, one chunk" against NanoGenerated's default blocks.
    /// </summary>
    [Benchmark]
    public object NanoGeneratedMeasured()
    {
        _bw.Reset();
        var output = (IMeasuredProtoOutput<IBufferWriter<byte>>)Model.NanoDescriptorModel.Instance;
        using var measured = output.Measure(_nanoSet);
        output.Serialize(measured, _bw);
        return _bw;
    }

    [Benchmark]
    public object GoogleProtobuf()
    {
        _bw.Reset();
        GpbExt.WriteTo(_gpbSet, _bw);
        return _bw;
    }
}
