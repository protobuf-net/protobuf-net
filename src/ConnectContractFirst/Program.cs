using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using Connectcontractfirst.V1;
using Google.Protobuf;
using Grpc.Core;
using ProtoBuf.Connect;
using ProtoBuf.Connect.AspNetCore;

// Serves an ORDINARY protoc-generated gRPC service over Connect.
//
// Nothing in this project is protobuf-net: the messages are Google.Protobuf, the service base and the
// client are protoc's, and greeter.proto carries no annotation of ours. The consumer's entire opt-in is
// the one MapConnectService line below. That is the "your existing contract-first gRPC service now
// speaks Connect" claim, run rather than asserted.
//
// The checks use a raw HttpClient and hand-rolled framing on purpose. Driving the server with our own
// ConnectChannel would let a matched pair of bugs pass, and would prove nothing about interoperability;
// this way the bytes on the wire are built from the specification.

const string Service = "connectcontractfirst.v1.Greeter";

var serveOnly = args.Contains("--serve");
var (httpPort, http2Port) = serveOnly ? (8090, 8091) : (FreePort(), FreePort());

static int FreePort()
{
    using var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
    probe.Start();
    var port = ((IPEndPoint)probe.LocalEndpoint).Port;
    probe.Stop();
    return port;
}

var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.ConfigureKestrel(o =>
{
    // as recorded in notes §24: a plaintext endpoint cannot serve both, there being no ALPN
    o.ListenLocalhost(httpPort, l => l.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
    o.ListenLocalhost(http2Port, l => l.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
});

// the codec marshals nothing itself - every method carries its own, taken from protoc's descriptors
builder.Services.AddConnect(o => o.Codecs.Add(MarshallerConnectCodec.Instance));

// scoped, deliberately: it is what Grpc.AspNetCore.Server defaults to, and it exercises the per-call
// bind rather than the cached-singleton fast path
builder.Services.AddScoped<GreeterImpl>();

var app = builder.Build();

// THE ENTIRE OPT-IN. Greeter.BindService is protoc's own method group; nothing else is written.
app.MapConnectService<GreeterImpl>(Greeter.BindService);

await app.StartAsync();

var address = $"http://127.0.0.1:{httpPort}";
var http2Address = $"http://127.0.0.1:{http2Port}";

if (serveOnly)
{
    Console.WriteLine($"contract-first Connect server on {address} (HTTP/1.1), {http2Address} (HTTP/2)");
    Console.WriteLine($"  POST {address}/{Service}/SayHello   Content-Type: application/proto");
    await app.WaitForShutdownAsync();
    return 0;
}

using var http = new HttpClient();
var failures = new List<string>();

void Check(string what, bool ok, string? detail = null)
{
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {what}{(detail is null ? "" : "  [" + detail + "]")}");
    if (!ok) failures.Add(what);
}

// ---------------------------------------------------------------- unary: the bare message, no framing
{
    var request = new HelloRequest { Name = "Marc", Repeat = 3 };
    using var content = new ByteArrayContent(request.ToByteArray());
    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/proto");

    using var response = await http.PostAsync($"{address}/{Service}/SayHello", content);
    var body = await response.Content.ReadAsByteArrayAsync();
    var reply = HelloReply.Parser.ParseFrom(body);

    Check("unary answers 200", response.StatusCode == HttpStatusCode.OK, response.StatusCode.ToString());
    Check("unary content-type is application/proto",
        response.Content.Headers.ContentType?.MediaType == "application/proto");
    Check("unary round-trips the message", reply.Message == "Hello Marc" && reply.Length == 4,
        $"{reply.Message}/{reply.Length}");
    // the marshaller can't measure, so the server must omit Content-Length rather than guess one
    Check("unary body is complete without a stated length", body.Length > 0,
        $"len={response.Content.Headers.ContentLength?.ToString() ?? "<unset>"}");
}

// ------------------------------------------------------- server streaming: enveloped in both directions
{
    var messages = await StreamAsync(address, "Subscribe", HttpVersion.Version11,
        new HelloRequest { Name = "Ada", Repeat = 3 });

    Check("server-streaming yields every message", messages.Count == 3, $"{messages.Count} of 3");
    Check("server-streaming messages are in order",
        messages.Count == 3 && messages[0].Message == "Ada #1" && messages[2].Message == "Ada #3",
        messages.Count == 3 ? messages[0].Message + ".." + messages[2].Message : "-");
}

// ------------------------------------------------------------------ client streaming: many in, one out
{
    var messages = await StreamAsync(address, "Collect", HttpVersion.Version11,
        new HelloRequest { Name = "a" }, new HelloRequest { Name = "bb" }, new HelloRequest { Name = "ccc" });

    Check("client-streaming answers exactly one message", messages.Count == 1, $"{messages.Count}");
    Check("client-streaming saw every request",
        messages.Count == 1 && messages[0].Message == "a,bb,ccc" && messages[0].Length == 6,
        messages.Count == 1 ? messages[0].Message : "-");
}

// --------------------------------------------------------------------- duplex: HTTP/2, and interleaved
{
    var messages = await StreamAsync(http2Address, "Chat", HttpVersion.Version20,
        new HelloRequest { Name = "one" }, new HelloRequest { Name = "two" });

    Check("duplex answers one message per request", messages.Count == 2, $"{messages.Count} of 2");
    Check("duplex echoes each request",
        messages.Count == 2 && messages[0].Message == "re: one" && messages[1].Message == "re: two",
        messages.Count == 2 ? messages[0].Message + "/" + messages[1].Message : "-");
}

// ------------------------------------------------- an RpcException is a status, not an internal failure
{
    var request = new HelloRequest { Name = "boom" };
    using var content = new ByteArrayContent(request.ToByteArray());
    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/proto");

    using var response = await http.PostAsync($"{address}/{Service}/SayHello", content);
    var json = await response.Content.ReadAsStringAsync();

    // permission_denied maps to 403 per the protocol's code/status table
    Check("RpcException becomes its Connect status", response.StatusCode == HttpStatusCode.Forbidden,
        response.StatusCode.ToString());
    Check("RpcException keeps its code and message",
        json.Contains("permission_denied") && json.Contains("not for you"), json);
}

await app.StopAsync();

Console.WriteLine();
if (failures.Count == 0)
{
    Console.WriteLine("All contract-first checks passed.");
    return 0;
}

Console.WriteLine($"{failures.Count} check(s) failed: {string.Join("; ", failures)}");
return 1;

// Enveloped request, enveloped response: one code path for all three streaming shapes, because Connect
// frames them identically and differs only in cardinality.
async Task<List<HelloReply>> StreamAsync(string root, string method, Version version, params HelloRequest[] requests)
{
    var body = new ArrayBufferWriter<byte>();
    foreach (var request in requests) WriteEnvelope(body, request.ToByteArray());

    using var content = new ByteArrayContent(body.WrittenSpan.ToArray());
    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/connect+proto");

    using var message = new HttpRequestMessage(HttpMethod.Post, $"{root}/{Service}/{method}")
    {
        Content = content,
        Version = version,
        VersionPolicy = HttpVersionPolicy.RequestVersionExact,
    };

    using var response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
    var bytes = await response.Content.ReadAsByteArrayAsync();

    var replies = new List<HelloReply>();
    var remaining = new ReadOnlySequence<byte>(bytes);
    var sawEnd = false;
    while (ConnectEnvelope.TryRead(ref remaining, out var flags, out var payload))
    {
        if ((flags & ConnectEnvelope.FlagEndOfStream) != 0)
        {
            // the terminating envelope carries an EndStreamResponse, which is JSON even under a binary
            // codec; an empty object is the success form
            sawEnd = true;
            continue;
        }

        replies.Add(HelloReply.Parser.ParseFrom(payload.ToArray()));
    }

    Check($"{method} terminates with an end-of-stream envelope", sawEnd);
    Check($"{method} consumes its body exactly", remaining.IsEmpty, $"{remaining.Length} bytes left over");
    return replies;
}

static void WriteEnvelope(IBufferWriter<byte> destination, byte[] payload)
{
    var span = destination.GetSpan(5);
    span[0] = 0;
    BinaryPrimitives.WriteUInt32BigEndian(span[1..], (uint)payload.Length);
    destination.Advance(5);
    destination.Write(payload);
}

/// <summary>
/// An ordinary contract-first service implementation: it derives from protoc's generated base and knows
/// nothing about Connect.
/// </summary>
sealed class GreeterImpl : Greeter.GreeterBase
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        if (request.Name == "boom")
        {
            // the gRPC way to state a failure; it must survive as a status rather than becoming a 500
            throw new RpcException(new Status(StatusCode.PermissionDenied, "not for you"));
        }

        return Task.FromResult(new HelloReply { Message = "Hello " + request.Name, Length = request.Name.Length });
    }

    public override async Task Subscribe(HelloRequest request, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
    {
        for (var i = 1; i <= request.Repeat; i++)
        {
            await responseStream.WriteAsync(new HelloReply { Message = $"{request.Name} #{i}", Length = i });
        }
    }

    public override async Task<HelloReply> Collect(IAsyncStreamReader<HelloRequest> requestStream, ServerCallContext context)
    {
        var names = new List<string>();
        var total = 0;
        while (await requestStream.MoveNext(context.CancellationToken))
        {
            names.Add(requestStream.Current.Name);
            total += requestStream.Current.Name.Length;
        }

        return new HelloReply { Message = string.Join(",", names), Length = total };
    }

    public override async Task Chat(IAsyncStreamReader<HelloRequest> requestStream, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
    {
        while (await requestStream.MoveNext(context.CancellationToken))
        {
            await responseStream.WriteAsync(new HelloReply
            {
                Message = "re: " + requestStream.Current.Name,
                Length = requestStream.Current.Name.Length,
            });
        }
    }
}
