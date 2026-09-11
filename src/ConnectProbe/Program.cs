using System.Buffers;
using System.Diagnostics;
using ProtoBuf.Connect;
using ProtoBuf.ConnectProbe;

// Stage 0 of the Connect MVP (notes/connect/findings.md §14): prove that protobuf-net's code-first
// bytes are interoperable with a reference Connect implementation, and that the request, response and
// error shapes are understood.
//
// The oracle is demo.connectrpc.com - the Eliza demo service, running connect-go. Public, always on,
// and needing no toolchain, which is what makes it usable on day one.
//
// Hits the network, so it is a manually-run tool rather than a test: `dotnet run --project src/ConnectProbe`.
// CI builds it (Build.csproj globs src\*\*.csproj) but does not run it.

const string BaseAddress = "https://demo.connectrpc.com";
const string Service = "connectrpc.eliza.v1.ElizaService";

var http = new HttpClient();
var channel = new ConnectChannel(http, new ProtoConnectCodec(ElizaModel.Instance), new Uri(BaseAddress));

var say = new ConnectMethod<SayRequest, SayResponse>(ConnectMethodType.Unary, Service, "Say");
var missing = new ConnectMethod<SayRequest, SayResponse>(ConnectMethodType.Unary, Service, "NoSuchMethod");
var mismatched = new ConnectMethod<WireTypeMismatchRequest, SayResponse>(ConnectMethodType.Unary, Service, "Say");

// a second channel over a codec that emits supplied bytes verbatim, so the probe can put genuinely
// malformed wire data on the wire and exercise the error path for real. That this needs no change to
// ConnectChannel is a small confirmation that the codec seam is in the right place.
var rawChannel = new ConnectChannel(http, new RawCodec(), new Uri(BaseAddress));
var rawSay = new ConnectMethod<byte[], SayResponse>(ConnectMethodType.Unary, Service, "Say");

var introduce = new ConnectMethod<IntroduceRequest, IntroduceResponse>(
    ConnectMethodType.ServerStreaming, Service, "Introduce");

var probe = new Probe();

await probe.Run("unary round-trip, binary codec", async () =>
{
    var reply = await channel.UnaryAsync(say, new SayRequest { Sentence = "hello from protobuf-net" });
    Probe.Require(!string.IsNullOrWhiteSpace(reply.Sentence), "the reply carries a sentence");
    return $"\"{reply.Sentence}\"";
});

await probe.Run("leading metadata is surfaced", async () =>
{
    var (reply, call) = await channel.UnaryWithMetadataAsync(say, new SayRequest { Sentence = "metadata?" });
    Probe.Require(reply.Sentence is not null, "the reply carries a sentence");
    Probe.Require(call.Headers.Count > 0, "at least one response header was surfaced");
    // connect-go sends no trailing metadata here; the accessor exists and is empty, which is the point
    return $"{call.Headers.Count} header(s), {call.Trailers.Count} trailer(s)";
});

await probe.Run("custom leading metadata is accepted", async () =>
{
    var options = new ConnectCallOptions
    {
        Headers = [new("x-probe-source", "protobuf-net"), new("x-probe-run", Guid.NewGuid().ToString("N"))],
    };
    var reply = await channel.UnaryAsync(say, new SayRequest { Sentence = "with headers" }, options);
    Probe.Require(reply.Sentence is not null, "the call succeeded with custom headers attached");
    return "accepted";
});

await probe.Run("timeout header is accepted", async () =>
{
    var options = new ConnectCallOptions { Timeout = TimeSpan.FromSeconds(30) };
    var reply = await channel.UnaryAsync(say, new SayRequest { Sentence = "with a deadline" }, options);
    Probe.Require(reply.Sentence is not null, "the call succeeded with connect-timeout-ms set");
    return "accepted";
});

await probe.Run("a wire-type mismatch is an unknown field, not an error", async () =>
{
    // protobuf skips a field whose wire type does not match the schema, so this SUCCEEDS and the
    // service sees an empty sentence. Recorded because the opposite was assumed first.
    var reply = await channel.UnaryAsync(mismatched, new WireTypeMismatchRequest { Sentence = 42 });
    Probe.Require(reply.Sentence is not null, "the call succeeded despite the disagreeing schemas");
    return $"tolerated; service replied \"{reply.Sentence}\"";
});

await probe.Run("server error object is parsed", async () =>
{
    // 0x67 is field 12, wire type 7 - which is not a wire type at all, so this really is malformed
    var ex = await Probe.Throws(() => rawChannel.UnaryAsync(rawSay, "garbage"u8.ToArray()));
    Probe.Require(ex.HttpStatus == 400, $"HTTP 400, was {ex.HttpStatus}");
    Probe.Require(ex.Code == ConnectCode.InvalidArgument, $"code invalid_argument, was {ex.Code.ToWireName()}");
    Probe.Require(!ex.CodeWasInferred, "the code came from the error object rather than being inferred");
    Probe.Require(ex.Message.Contains(':'), "a server-supplied message survived");
    return ex.Message;
});

await probe.Run("server-streaming against connect-go", async () =>
{
    // the sharp interop check: our envelope reader, our EndStreamResponse parse, and our framing of the
    // request, all against a reference implementation rather than against ourselves
    var stream = await channel.ServerStreamingAsync(introduce, new IntroduceRequest { Name = "protobuf-net" });
    var sentences = new List<string>();
    await foreach (var response in stream)
    {
        Probe.Require(!string.IsNullOrWhiteSpace(response.Sentence), "each message carries a sentence");
        sentences.Add(response.Sentence!);
    }

    Probe.Require(sentences.Count >= 2, $"several messages, got {sentences.Count}");
    Probe.Require(sentences[0].Contains("protobuf-net"), $"the first echoes the name: \"{sentences[0]}\"");
    // reaching here at all means the terminating envelope was seen: the enumerator throws if the
    // stream ends without one, so a clean finish is itself the assertion
    return $"{sentences.Count} messages, {stream.Trailers.Count} trailer(s), terminator seen";
});

await probe.Run("a stream cannot be enumerated twice", async () =>
{
    var stream = await channel.ServerStreamingAsync(introduce, new IntroduceRequest { Name = "once" });
    await foreach (var _ in stream) { }
    try
    {
        await foreach (var _ in stream) { }
    }
    catch (InvalidOperationException ex)
    {
        return ex.Message.Length > 0 ? "refused, as a network stream must" : "refused";
    }
    throw new InvalidOperationException("expected the second enumeration to be refused");
});

await probe.Run("an unrouted path is inferred, not parsed", async () =>
{
    // measured: the HTTP layer answers this one with text/plain and no Connect error object at all,
    // which is precisely why the protocol carries a status-to-code inference table. A client that
    // assumes every non-200 carries JSON throws a parse error on the most ordinary failure there is.
    var ex = await Probe.Throws(() => channel.UnaryAsync(missing, new SayRequest { Sentence = "nobody home" }));
    Probe.Require(ex.HttpStatus == 404, $"HTTP 404, was {ex.HttpStatus}");
    Probe.Require(ex.Code == ConnectCode.Unimplemented, $"404 infers unimplemented, was {ex.Code.ToWireName()}");
    Probe.Require(ex.CodeWasInferred, "the code was inferred rather than parsed");
    return ex.Message;
});

return probe.Report();

/// <summary>
/// Emits supplied bytes verbatim, so the probe can send payloads no serializer would produce.
/// </summary>
internal sealed class RawCodec : ConnectCodec
{
    public override string Name => "proto";

    public override long? Measure<T>(T value) => value is byte[] bytes ? bytes.Length : null;

    public override void Write<T>(IBufferWriter<byte> destination, T value, ProtoBuf.Serializers.ISerializer<T>? serializer = null)
    {
        if (value is byte[] bytes) destination.Write(bytes);
    }

    // never reached: every call made through this codec is expected to fail
    public override T Read<T>(in ReadOnlySequence<byte> source, ProtoBuf.Serializers.ISerializer<T>? serializer = null) => default!;
}

internal sealed class Probe
{
    private int _passed, _failed;

    public async Task Run(string name, Func<Task<string>> check)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var detail = await check();
            _passed++;
            Console.WriteLine($"  pass  {name} ({sw.ElapsedMilliseconds}ms)");
            if (!string.IsNullOrEmpty(detail)) Console.WriteLine($"        {detail}");
        }
        catch (Exception ex)
        {
            _failed++;
            Console.WriteLine($"  FAIL  {name} ({sw.ElapsedMilliseconds}ms)");
            Console.WriteLine($"        {ex.GetType().Name}: {ex.Message}");
        }
    }

    public static void Require(bool condition, string expectation)
    {
        if (!condition) throw new InvalidOperationException("expected " + expectation);
    }

    public static async Task<ConnectException> Throws(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (ConnectException ex)
        {
            return ex;
        }
        throw new InvalidOperationException("expected a ConnectException; the call succeeded");
    }

    public int Report()
    {
        Console.WriteLine();
        Console.WriteLine($"{_passed} passed, {_failed} failed");
        return _failed == 0 ? 0 : 1;
    }
}
