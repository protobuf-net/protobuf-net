using System.Buffers.Binary;
using Connectrpc.Conformance.V1;
using Google.Protobuf;
using ProtoBuf.Connect;
using ProtoBuf.Connect.AspNetCore;
using ProtoBuf.ConnectConformance;

// The server half of connectrpc/conformance, which is the only external oracle there is for this
// protocol: the runner drives a real Connect client against us and checks the wire, not our opinion of
// it.
//
// The bootstrap is a tiny stdin/stdout protocol, described by `connectconformance --help`: read one
// ServerCompatRequest, start a server implementing ConformanceService, write one ServerCompatResponse
// saying where it is listening, then run until SIGTERM. The runner may invoke this repeatedly, with
// different properties each time.
//
// Note what the service itself is: a .proto, compiled by protoc, served through the CONTRACT-FIRST
// path. So this exercises that adapter as well as the protocol - and it is the honest way round, since
// writing a protobuf-net code-first mirror of someone else's contract would be testing our translation
// of the suite rather than the suite.

var request = ReadRequest();

var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();

// The runner owns stdout (the bootstrap protocol) and shows nothing of our stderr, so a server-side
// fault is otherwise completely silent - which matters, because the interesting failures here are
// exactly the ones that abort a response after it has committed. CONNECT_CONFORMANCE_LOG names a file
// to append diagnostics to; unset, nothing is written and nothing costs anything.
var logPath = Environment.GetEnvironmentVariable("CONNECT_CONFORMANCE_LOG");
if (!string.IsNullOrEmpty(logPath))
{
    AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
    {
        // first-chance, deliberately: a handled exception is still the thing that turned a good
        // response into a reset connection, and by the time it is observed the cause is gone
        try
        {
            lock (logPath!) File.AppendAllText(logPath, e.Exception + Environment.NewLine + new string('-', 60) + Environment.NewLine);
        }
        catch
        {
            // logging must never be the reason a conformance run fails
        }
    };
}

// The runner names a minimum HTTP version and we may exceed it - but a PLAINTEXT endpoint cannot
// serve both, there being no ALPN to negotiate with (notes §24). So the stated minimum picks the
// listener outright: HTTP/2 when asked for, HTTP/1.1 otherwise.
var protocols = request.HttpVersion == HTTPVersion._2
    ? Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2
    : Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;

// 127.0.0.1 explicitly, not ListenLocalhost: dynamic port binding ("port 0") is refused for the
// localhost alias, because it would have to pick a free port on IPv4 and IPv6 independently
builder.WebHost.ConfigureKestrel(o =>
    o.Listen(System.Net.IPAddress.Loopback, 0, l => l.Protocols = protocols));

builder.Services.AddConnect(o =>
{
    o.Codecs.Add(MarshallerConnectCodec.Instance);
    // the suite asserts on error messages, so an unexpected failure must say what it was rather than
    // reporting a bare "internal"
    o.IncludeExceptionDetailInErrors = true;
});
builder.Services.AddSingleton<ConformanceServiceImpl>();

var app = builder.Build();

if (!string.IsNullOrEmpty(logPath))
{
    app.Use(async (ctx, next) =>
    {
        ctx.Response.OnStarting(() =>
        {
            try { lock (logPath!) File.AppendAllText(logPath, $"RESPONSE {ctx.Response.StatusCode} headers=[{string.Join(", ", ctx.Response.Headers.Keys)}]\n"); } catch { }
            return Task.CompletedTask;
        });
        await next();
    });
}
app.MapConnectService<ConformanceServiceImpl>(ConformanceService.BindService);

await app.StartAsync();

WriteResponse(app);

// the runner stops us with SIGTERM; WaitForShutdownAsync already handles that
await app.WaitForShutdownAsync();
return 0;

static ServerCompatRequest ReadRequest()
{
    using var stdin = Console.OpenStandardInput();

    // "a binary-encoded Protobuf message ... prefixed with a fixed-32-bit length" - big-endian, as
    // everywhere else in this protocol
    var header = ReadExactly(stdin, 4);
    var length = BinaryPrimitives.ReadUInt32BigEndian(header);
    var payload = ReadExactly(stdin, checked((int)length));

    return ServerCompatRequest.Parser.ParseFrom(payload);

    static byte[] ReadExactly(Stream source, int count)
    {
        var buffer = new byte[count];
        var read = 0;
        while (read < count)
        {
            var got = source.Read(buffer, read, count - read);
            if (got <= 0) throw new EndOfStreamException($"Expected {count} bytes, got {read}.");
            read += got;
        }

        return buffer;
    }
}

static void WriteResponse(WebApplication app)
{
    var addresses = app.Services
        .GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
        .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()
        ?? throw new InvalidOperationException("Kestrel reported no addresses.");

    // port 0 was requested, so the real one is only knowable after StartAsync
    var bound = new Uri(addresses.Addresses.Single());

    var response = new ServerCompatResponse { Host = "127.0.0.1", Port = (uint)bound.Port };

    var payload = response.ToByteArray();
    Span<byte> header = stackalloc byte[4];
    BinaryPrimitives.WriteUInt32BigEndian(header, (uint)payload.Length);

    using var stdout = Console.OpenStandardOutput();
    stdout.Write(header);
    stdout.Write(payload, 0, payload.Length);
    stdout.Flush();
}
