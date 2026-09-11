using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProtoBuf.AotConnectSmoke;
using Grpc.Core;
using ProtoBuf.Connect;
using ProtoBuf.Connect.AspNetCore;
using ProtoBuf.Grpc;

// Stage 1 of notes/connect/findings.md §14: a defined service, a working ASP.NET Core server, and a
// working client, .NET to .NET. Exits non-zero on any mismatch, and is published with PublishAot so
// that "it works" means "it works natively", not "it works where ref-emit still exists".
//
//   dotnet run --project src/AotConnectSmoke              # self-check
//   dotnet run --project src/AotConnectSmoke -- --serve   # just the server, for an external client

// The service name as it must appear on the wire, stated here independently of the implementation.
// Deriving it from SmokeServices would make the raw-HTTP checks below agree with whatever the bindings
// happen to do, including a wrong service name.
const string ServiceOnTheWire = "aotconnectsmoke.v1.Greeter";

var serveOnly = args.Contains("--serve");
var port = serveOnly ? 8080 : 0;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

// HTTP/1.1 is the default for a plaintext Kestrel endpoint, and this test deliberately leaves it that
// way: gRPC could not be served here at all without switching the port to HTTP/2, which is the whole
// operational argument for Connect. Nothing below asks for HTTP/2 and everything works.
builder.Services.AddConnect(options =>
{
    options.Codecs.Add(new ProtoConnectCodec(SmokeModel.Instance));
    // IncludeExceptionDetailInErrors stays off, which one of the checks below relies on
});
builder.Services.AddScoped<GreeterService>();

var app = builder.Build();
app.MapSmokeServices();
await app.StartAsync();

var address = app.Services.GetRequiredService<IServer>().Features
    .Get<IServerAddressesFeature>()!.Addresses.First();

if (serveOnly)
{
    Console.WriteLine($"Connect server listening on {address}");
    Console.WriteLine($"  curl -sS --http1.1 -H 'Content-Type: application/proto' \\");
    Console.WriteLine($"       --data-binary @req.bin {address}/{ServiceOnTheWire}/SayHello | xxd");
    await app.WaitForShutdownAsync();
    return 0;
}

using var http = new HttpClient();
var channel = new ConnectChannel(http, new ProtoConnectCodec(SmokeModel.Instance), new Uri(address));
IGreeter client = SmokeServices.CreateClient<IGreeter>(channel);
var checks = new Checks();

await checks.Run("unary round-trip over HTTP/1.1", async () =>
{
    var reply = await client.SayHelloAsync(new HelloRequest { Name = "marc", Repeat = 2 });
    Checks.Require(reply.Message == "hello marc; hello marc", $"the greeting, was \"{reply.Message}\"");
    Checks.Require(reply.Length == reply.Message!.Length, "a second field survived the round-trip");
    return reply.Message;
});

await checks.Run("trailing metadata arrives", async () =>
{
    // straight down the transport, since the contract's own signature carries no response metadata.
    // The descriptor is built here rather than borrowed from SmokeServices: those are private, and a
    // check that borrowed them could not notice the two disagreeing.
    var sayHello = new ConnectMethod<HelloRequest, HelloReply>(
        ConnectMethodType.Unary, ServiceOnTheWire, "SayHello");
    var (reply, call) = await channel.UnaryWithMetadataAsync(sayHello, new HelloRequest { Name = "trailers" });
    Checks.Require(reply.Message is not null, "the call succeeded");
    var trailer = call.Trailers.FirstOrDefault(t => t.Key == "greeter-version");
    Checks.Require(trailer.Value == "1", $"greeter-version=1, was \"{trailer.Value}\"");
    // the prefix is a wire detail and must not leak into the surfaced name
    Checks.Require(!call.Trailers.Any(t => t.Key.StartsWith("trailer-")), "the trailer- prefix was stripped");
    Checks.Require(!call.Headers.Any(t => t.Key.StartsWith("trailer-")), "trailers are not also reported as headers");
    return $"{call.Trailers.Count} trailer(s), prefix stripped";
});

await checks.Run("a deliberate failure keeps its code and message", async () =>
{
    var ex = await Checks.Throws(() => client.RefuseAsync(new HelloRequest { Name = "mallory" }));
    Checks.Require(ex.Code == ConnectCode.PermissionDenied, $"permission_denied, was {ex.Code.ToWireName()}");
    Checks.Require(ex.HttpStatus == 403, $"HTTP 403, was {ex.HttpStatus}");
    Checks.Require(!ex.CodeWasInferred, "the code was parsed, not inferred");
    Checks.Require(ex.RawMessage == "'mallory' may not greet.", $"the message survived, was \"{ex.RawMessage}\"");
    return ex.Message;
});

await checks.Run("an unhandled exception is not leaked", async () =>
{
    var ex = await Checks.Throws(() => client.ExplodeAsync(new HelloRequest { Name = "boom" }));
    Checks.Require(ex.Code == ConnectCode.Internal, $"internal, was {ex.Code.ToWireName()}");
    Checks.Require(ex.HttpStatus == 500, $"HTTP 500, was {ex.HttpStatus}");
    Checks.Require(
        ex.RawMessage?.Contains("secret") != true && ex.Message.Contains("secret") == false,
        "the exception message did not reach the caller");
    return "internal, with no detail";
});

await checks.Run("a gRPC deadline becomes connect-timeout-ms", async () =>
{
    // stated the protobuf-net.Grpc way - an absolute deadline on a CallOptions - and translated to the
    // protocol's relative connect-timeout-ms by the generated bridge. A caller writes gRPC and gets Connect.
    CallContext context = new CallOptions(deadline: DateTime.UtcNow.AddMilliseconds(300));
    var sw = Stopwatch.StartNew();
    var ex = await Checks.Throws(() => client.DawdleAsync(new HelloRequest { Name = "slow" }, context));
    sw.Stop();
    Checks.Require(ex.Code == ConnectCode.DeadlineExceeded, $"deadline_exceeded, was {ex.Code.ToWireName()}");
    Checks.Require(sw.ElapsedMilliseconds < 5000, $"it gave up promptly, took {sw.ElapsedMilliseconds}ms");
    return $"{ex.Code.ToWireName()} after {sw.ElapsedMilliseconds}ms";
});

await checks.Run("leading metadata set on a CallContext reaches the service", async () =>
{
    // the other half of the bridge: gRPC Metadata out, Connect request headers on the wire
    var metadata = new Metadata { { "x-caller", "grpc-shaped" } };
    CallContext context = new CallOptions(headers: metadata);
    var reply = await client.SayHelloAsync(new HelloRequest { Name = "metadata" }, context);
    Checks.Require(reply.Message == "hello metadata", $"the call succeeded, was \"{reply.Message}\"");
    return "x-caller travelled as an ordinary request header";
});

await checks.Run("server-streaming round-trip", async () =>
{
    var messages = new List<string>();
    await foreach (var reply in client.Subscribe(new HelloRequest { Name = "marc", Repeat = 3 }))
    {
        messages.Add(reply.Message!);
        Checks.Require(reply.Length == reply.Message!.Length, "a second field survived each message");
    }

    Checks.Require(messages.Count == 3, $"three messages, got {messages.Count}");
    Checks.Require(messages[2] == "hello marc #3", $"in order, last was \"{messages[2]}\"");
    return string.Join(", ", messages);
});

await checks.Run("streaming trailers travel in the terminator, not the headers", async () =>
{
    var subscribe = new ConnectMethod<HelloRequest, HelloReply>(
        ConnectMethodType.ServerStreaming, ServiceOnTheWire, "Subscribe");
    var stream = await channel.ServerStreamingAsync(subscribe, new HelloRequest { Name = "t", Repeat = 2 });

    // before enumeration the trailers cannot exist yet - that is the whole distinction from unary
    Checks.Require(stream.Trailers.Count == 0, "no trailers before the stream is drained");
    Checks.Require(!stream.Headers.Any(h => h.Key.StartsWith("trailer-")), "and none smuggled in as headers");

    var count = 0;
    await foreach (var _ in stream) count++;

    Checks.Require(count == 2, $"two messages, got {count}");
    var trailer = stream.Trailers.FirstOrDefault(t => t.Key == "greeter-count");
    Checks.Require(trailer.Value == "2", $"greeter-count=2 from the terminator, was \"{trailer.Value}\"");
    return $"{stream.Trailers.Count} trailer(s), from the terminating message";
});

await checks.Run("a mid-stream failure arrives under HTTP 200", async () =>
{
    var failing = new ConnectMethod<HelloRequest, HelloReply>(
        ConnectMethodType.ServerStreaming, ServiceOnTheWire, "SubscribeThenFail");
    var stream = await channel.ServerStreamingAsync(failing, new HelloRequest { Name = "x" });

    var received = new List<string>();
    var ex = await Checks.Throws(async () =>
    {
        await foreach (var reply in stream) received.Add(reply.Message!);
    });

    Checks.Require(received.Count == 2, $"the messages before the failure still arrived, got {received.Count}");
    Checks.Require(ex.Code == ConnectCode.ResourceExhausted, $"resource_exhausted, was {ex.Code.ToWireName()}");
    Checks.Require(ex.RawMessage == "the well ran dry", $"the message survived, was \"{ex.RawMessage}\"");
    // no HTTP status is the evidence: the response said 200 before anything went wrong, so this error
    // can only have come from the terminating message
    Checks.Require(ex.HttpStatus is null, $"no HTTP status - it came from the terminator, was {ex.HttpStatus}");
    return $"{received.Count} messages, then {ex.Code.ToWireName()} from the terminator";
});

await checks.Run("client-streaming round-trip, chunked", async () =>
{
    var reply = await client.CollectAsync(Names());

    Checks.Require(reply.Message == "a+b+c", $"all three arrived in order, was \"{reply.Message}\"");
    // the service reports what Content-Length it saw: -1 means the header was absent, which is the
    // whole point - a body produced as it is sent cannot state a length
    Checks.Require(reply.Length == -1, $"the request went out chunked, server saw Content-Length {reply.Length}");
    return "3 messages, no Content-Length";

    static async IAsyncEnumerable<HelloRequest> Names()
    {
        foreach (var name in new[] { "a", "b", "c" })
        {
            await Task.Yield();
            yield return new HelloRequest { Name = name };
        }
    }
});

await checks.Run("framing must match the method's shape", async () =>
{
    // unary framing at a streaming method: the content-type states the framing, and disagreeing is 415
    using var request = new HttpRequestMessage(HttpMethod.Post, $"{address}/{ServiceOnTheWire}/Subscribe")
    {
        Content = new ByteArrayContent([]) { Headers = { ContentType = new MediaTypeHeaderValue("application/proto") } },
    };
    using var response = await http.SendAsync(request);
    Checks.Require((int)response.StatusCode == 415, $"HTTP 415, was {(int)response.StatusCode}");
    var body = await response.Content.ReadAsStringAsync();
    // note '+' arrives as \u002B: Utf8JsonWriter's default encoder escapes it. Valid JSON, and the
    // client parses it fine, but it is worth knowing before matching on message text.
    Checks.Require(body.Contains("ServerStreaming"), $"it names the shape, body was {body}");
    Checks.Require(body.Contains("unary framing"), $"and what was wrong, body was {body}");
    return body;
});

await checks.Run("an unknown codec is 415", async () =>
{
    using var request = new HttpRequestMessage(HttpMethod.Post, $"{address}/{ServiceOnTheWire}/SayHello)".TrimEnd(')'))
    {
        Content = new ByteArrayContent([]) { Headers = { ContentType = new MediaTypeHeaderValue("application/xml") } },
    };
    using var response = await http.SendAsync(request);
    Checks.Require((int)response.StatusCode == 415, $"HTTP 415, was {(int)response.StatusCode}");
    Checks.Require(response.Content.Headers.ContentType?.MediaType == "application/json", "the error is JSON");
    var body = await response.Content.ReadAsStringAsync();
    Checks.Require(body.Contains("\"unimplemented\""), $"code unimplemented, body was {body}");
    Checks.Require(body.Contains("proto"), "the supported codecs are named");
    return body;
});

await checks.Run("enveloped framing at a unary method is declined", async () =>
{
    using var request = new HttpRequestMessage(HttpMethod.Post, $"{address}/{ServiceOnTheWire}/SayHello")
    {
        Content = new ByteArrayContent([]) { Headers = { ContentType = new MediaTypeHeaderValue("application/connect+proto") } },
    };
    using var response = await http.SendAsync(request);
    Checks.Require((int)response.StatusCode == 415, $"HTTP 415, was {(int)response.StatusCode}");
    var body = await response.Content.ReadAsStringAsync();
    Checks.Require(body.Contains("this method is unary"), $"it says why, body was {body}");
    return body;
});

await checks.Run("the wire form is a bare message, no envelope", async () =>
{
    // HelloRequest { Name = "hi" } is 0a 02 68 69 - written by hand so the assertion is about the
    // protocol rather than about our own serializer agreeing with itself
    using var request = new HttpRequestMessage(HttpMethod.Post, $"{address}/{ServiceOnTheWire}/SayHello")
    {
        Content = new ByteArrayContent([0x0a, 0x02, 0x68, 0x69])
        {
            Headers = { ContentType = new MediaTypeHeaderValue("application/proto") },
        },
    };
    request.Headers.TryAddWithoutValidation("connect-protocol-version", "1");

    using var response = await http.SendAsync(request);
    Checks.Require(response.IsSuccessStatusCode, $"HTTP 200, was {(int)response.StatusCode}");
    Checks.Require(response.Content.Headers.ContentType?.MediaType == "application/proto", "the response names the codec");

    var bytes = await response.Content.ReadAsByteArrayAsync();
    // "hello hi" is 8 bytes: field 1, length-delimited, length 8 - with no 5-byte gRPC prefix in front
    Checks.Require(bytes[0] == 0x0a, $"field 1 length-delimited, first byte was 0x{bytes[0]:x2}");
    Checks.Require(bytes[1] == 8, $"an 8-byte greeting, length byte was {bytes[1]}");
    Checks.Require(response.Content.Headers.ContentLength == bytes.Length, "Content-Length was stated");
    Checks.Require(response.Version.Major == 1, $"served over HTTP/1.1, was HTTP/{response.Version}");
    return $"{bytes.Length} bytes, HTTP/{response.Version}, Content-Length stated";
});

await app.StopAsync();
return checks.Report();

internal sealed class Checks
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
