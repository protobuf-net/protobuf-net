using System.Buffers;
using BenchmarkDotNet.Attributes;
using Google.Protobuf;
using Grpc.Core;
using ProtoBuf.Connect;
using ProtoBuf.Connect.Google;

namespace ProtoBuf.ConnectBenchmark;

/// <summary>
/// What the three codecs cost, and whether the two predictions on record hold.
/// </summary>
/// <remarks>
/// The predictions, both made on reasoning and never measured (findings §47, and the JSON spike):
/// <list type="number">
/// <item>contract-first JSON pays a <b>transcode</b> - <c>JsonFormatter.Format</c> and
/// <c>JsonParser.Parse</c> speak <c>string</c>, so it goes bytes → string → bytes - where code-first
/// JSON writes UTF-8 straight through <c>Utf8JsonWriter</c>;</item>
/// <item>JSON cannot <c>Measure</c>, so an <b>enveloped</b> message must be buffered before its
/// length prefix can be written, where binary states the length up front.</item>
/// </list>
/// <para>
/// <b>The two JSON rows are not a like-for-like comparison of the transcode alone</b>, and saying so
/// matters more than the number: they serialize different CLR types through different implementations
/// of the same mapping. What they do compare fairly is the choice a consumer actually faces - "I have
/// a contract-first service" versus "I have a code-first one" - which is the question worth answering.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class CodecBenchmarks
{
    private ConnectCodec _proto;
    private ConnectCodec _jsonCodeFirst;
    private ConnectCodec _jsonContractFirst;

    private Payload _codeFirst;
    private Bench.Payload _contractFirst;
    private IConnectMessageCodec<Bench.Payload> _marshaller;

    private ArrayBufferWriter<byte> _buffer;
    private ReadOnlySequence<byte> _protoBytes, _jsonCodeFirstBytes, _jsonContractFirstBytes;

    [GlobalSetup]
    public void Setup()
    {
        _proto = new ProtoConnectCodec(BenchModel.Instance);
        _jsonCodeFirst = new JsonConnectCodec((IJsonModel)BenchModel.Instance);
        _jsonContractFirst = GoogleJsonConnectCodec.Instance;

        _codeFirst = new Payload
        {
            Id = 12345,
            Ticks = 638_000_000_000_000_000L,
            Name = "a moderately typical name",
            Active = true,
            Score = 98.25,
            Blob = new byte[64],
            Level = Level.High,
            Detail = new Detail { Note = "some detail text", Weight = 7 },
            Tags = { "alpha", "beta", "gamma" },
            Counts = { 1, 2, 3, 5, 8, 13 },
        };
        Random.Shared.NextBytes(_codeFirst.Blob);

        // the same bridge the JSON sweep uses: binary is the one encoding both sides already agree
        // on, so the contract-first message is guaranteed to hold identical data rather than whatever
        // a second hand-written literal happened to say
        var bridge = new ArrayBufferWriter<byte>();
        _proto.Write(bridge, _codeFirst);
        _contractFirst = Bench.Payload.Parser.ParseFrom(bridge.WrittenSpan.ToArray());

        // a contract-first method carries its marshalling from the generated descriptor; the Google
        // JSON codec mines it for a MessageDescriptor, so it must be present here as it would be in a
        // real call (§47)
        _marshaller = new MarshallerMessageCodec<Bench.Payload>(
            Marshallers.Create(x => x.ToByteArray(), Bench.Payload.Parser.ParseFrom));

        _buffer = new ArrayBufferWriter<byte>(1024);
        _protoBytes = Encode(_proto, _codeFirst, null);
        _jsonCodeFirstBytes = Encode(_jsonCodeFirst, _codeFirst, null);
        _jsonContractFirstBytes = Encode(_jsonContractFirst, _contractFirst, _marshaller);

        // A benchmark comparing two encoders is worthless if they are encoding different messages,
        // and `Payload` is not one of the shapes src/ConnectJsonDifferential covers - so the
        // equivalence is asserted here rather than assumed. The two byte counts genuinely differ
        // (Google's formatter spaces its separators); what must match is the parsed document.
        var mine = Canonical(_jsonCodeFirstBytes);
        var theirs = Canonical(_jsonContractFirstBytes);
        if (mine != theirs)
        {
            throw new InvalidOperationException(
                $"the two JSON codecs disagree about this payload, so the timings compare nothing:"
                + $"\n  code-first:     {mine}\n  contract-first: {theirs}");
        }

        // recursive, and it has to be: GetRawText() preserves the source's whitespace, so a shallow
        // comparison reported a difference for `[ 1, 2 ]` versus `[1,2]` - Google's formatter spaces
        // its separators, which is also the whole of the 37-byte size gap
        static string Canonical(ReadOnlySequence<byte> utf8)
        {
            using var document = System.Text.Json.JsonDocument.Parse(utf8);
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new System.Text.Json.Utf8JsonWriter(buffer)) Write(document.RootElement, writer);
            return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);

            static void Write(System.Text.Json.JsonElement element, System.Text.Json.Utf8JsonWriter writer)
            {
                switch (element.ValueKind)
                {
                    case System.Text.Json.JsonValueKind.Object:
                        writer.WriteStartObject();
                        foreach (var property in element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                        {
                            writer.WritePropertyName(property.Name);
                            Write(property.Value, writer);
                        }
                        writer.WriteEndObject();
                        return;
                    case System.Text.Json.JsonValueKind.Array:
                        writer.WriteStartArray();
                        foreach (var item in element.EnumerateArray()) Write(item, writer);
                        writer.WriteEndArray();
                        return;
                    default:
                        element.WriteTo(writer);
                        return;
                }
            }
        }

        ReadOnlySequence<byte> Encode<T>(ConnectCodec codec, T value, IConnectMessageCodec<T> over)
        {
            var writer = new ArrayBufferWriter<byte>();
            codec.Write(writer, value, over);
            return new ReadOnlySequence<byte>(writer.WrittenMemory);
        }
    }

    /// <summary>
    /// The encoded sizes, which matter as much as the times and which no timing column shows.
    /// </summary>
    /// <remarks>
    /// Exposed rather than printed from a <c>[GlobalCleanup]</c>: BenchmarkDotNet runs the benchmark
    /// in a generated child process and that console output does not reach the summary, so it was
    /// written and never seen.
    /// </remarks>
    public string Sizes => $"proto={_protoBytes.Length}B, "
        + $"json code-first={_jsonCodeFirstBytes.Length}B, "
        + $"json contract-first={_jsonContractFirstBytes.Length}B";

    // ---- write ------------------------------------------------------------------------------

    [Benchmark(Baseline = true), BenchmarkCategory("write")]
    public int Write_Proto()
    {
        _buffer.ResetWrittenCount();
        _proto.Write(_buffer, _codeFirst);
        return _buffer.WrittenCount;
    }

    [Benchmark, BenchmarkCategory("write")]
    public int Write_Json_CodeFirst()
    {
        _buffer.ResetWrittenCount();
        _jsonCodeFirst.Write(_buffer, _codeFirst);
        return _buffer.WrittenCount;
    }

    [Benchmark, BenchmarkCategory("write")]
    public int Write_Json_ContractFirst()
    {
        _buffer.ResetWrittenCount();
        _jsonContractFirst.Write(_buffer, _contractFirst, _marshaller);
        return _buffer.WrittenCount;
    }

    // ---- read -------------------------------------------------------------------------------

    [Benchmark, BenchmarkCategory("read")]
    public Payload Read_Proto() => _proto.Read<Payload>(_protoBytes);

    [Benchmark, BenchmarkCategory("read")]
    public Payload Read_Json_CodeFirst() => _jsonCodeFirst.Read<Payload>(_jsonCodeFirstBytes);

    [Benchmark, BenchmarkCategory("read")]
    public Bench.Payload Read_Json_ContractFirst()
        => _jsonContractFirst.Read(_jsonContractFirstBytes, _marshaller);

    // ---- enveloped write, which is where Measure earns or costs ------------------------------

    /// <summary>
    /// The streaming framing: a flag byte, a big-endian length, then the message.
    /// </summary>
    /// <remarks>
    /// Binary can state the length without encoding twice (<c>TypeModel.Measure</c>), so it writes
    /// straight into the destination. JSON returns <c>null</c> from <c>Measure</c> and therefore has
    /// to encode into a scratch buffer, read the length off it, and copy. This pair is what says
    /// whether that costs anything worth caring about.
    /// </remarks>
    [Benchmark, BenchmarkCategory("envelope")]
    public int Envelope_Proto()
    {
        _buffer.ResetWrittenCount();
        ConnectEnvelope.WriteMessage(_buffer, _proto, _codeFirst);
        return _buffer.WrittenCount;
    }

    [Benchmark, BenchmarkCategory("envelope")]
    public int Envelope_Json_CodeFirst()
    {
        _buffer.ResetWrittenCount();
        ConnectEnvelope.WriteMessage(_buffer, _jsonCodeFirst, _codeFirst);
        return _buffer.WrittenCount;
    }
}
