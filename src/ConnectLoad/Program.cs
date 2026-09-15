using System.Diagnostics;
using System.Net;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProtoBuf.Connect;
using ProtoBuf.ConnectLoad;
using ProtoBuf.Grpc.Client;

// What does choosing Connect over gRPC actually cost, on the same stack?
//
// The comparison is only worth making because everything else is held still: one [Service] contract,
// one implementation, one Kestrel, one DI container, one protobuf-net model producing the marshalling
// for BOTH sides. The single variable is the protocol and its implementation.
//
// Sustained throughput rather than per-operation timing: BenchmarkDotNet over a loopback socket
// mostly measures the loopback. Here a fixed number of workers issue calls back to back for a fixed
// wall-clock window, and the reported figure is completed operations per second with the latency
// distribution beside it.
//
// READ THE CAVEATS AT THE BOTTOM before quoting any of this.

var seconds = Arg("--seconds", 3);
var concurrency = Arg("--concurrency", 32);
var payload = Arg("--payload", 256);
var streamCount = Arg("--stream", 10);

var connectPort = FreePort();
var grpcPort = FreePort();

var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.ConfigureKestrel(o =>
{
    // three listeners: Connect gets an HTTP/1.1 one AND an HTTP/2 one, so the protocol comparison is
    // like-for-like on HTTP/2 while the HTTP/1.1 figure still shows what gRPC cannot do at all
    o.Listen(IPAddress.Loopback, connectPort, l => l.Protocols = HttpProtocols.Http1);
    o.Listen(IPAddress.Loopback, grpcPort, l => l.Protocols = HttpProtocols.Http2);
});

builder.Services.AddSingleton<GreeterService>();
builder.Services.AddGrpcSide();
builder.Services.AddConnectSide();

var app = builder.Build();
app.MapGrpcService<GreeterService>();
// gRPC owns the canonical path, so Connect goes under a prefix - see findings §55
ConnectSide.BindServices(app, "connect");
await app.StartAsync();

var http1 = $"http://127.0.0.1:{connectPort}";
var http2 = $"http://127.0.0.1:{grpcPort}";

try
{
    // CONNECTION PARALLELISM HAS TO BE EQUALISED, or this measures sockets rather than protocols.
    // HttpClient opens a connection per concurrent HTTP/1.1 call, while HTTP/2 multiplexes every call
    // over ONE - so left at stock defaults the HTTP/1.1 row gets 32 sockets and the HTTP/2 rows get
    // one apiece, and the resulting gap says nothing about Connect or gRPC. Both HTTP/2 clients are
    // therefore allowed multiple connections, and HTTP/1.1 is capped at the same worker count.
    using var h1 = new HttpClient(new SocketsHttpHandler { MaxConnectionsPerServer = concurrency });
    using var h2 = new HttpClient(new SocketsHttpHandler { EnableMultipleHttp2Connections = true });

    using var grpcChannel = GrpcChannel.ForAddress(http2, new GrpcChannelOptions
    {
        HttpHandler = new SocketsHttpHandler { EnableMultipleHttp2Connections = true },
    });
    var overGrpc = grpcChannel.CreateGrpcService<IGreeter>(GrpcSide.Instance);

    var connectOverHttp2 = ConnectSide.CreateClient<IGreeter>(new ConnectChannel(
        h2, new ProtoConnectCodec(LoadModel.Instance), new Uri(http2 + "/connect"),
        httpVersion: HttpVersion.Version20));

    var connectOverHttp1 = ConnectSide.CreateClient<IGreeter>(new ConnectChannel(
        h1, new ProtoConnectCodec(LoadModel.Instance), new Uri(http1 + "/connect")));

    var request = new Request { Name = "load", Count = streamCount, Blob = new byte[payload] };
    Random.Shared.NextBytes(request.Blob);

    Console.WriteLine($"// {concurrency} workers, {seconds}s per scenario, {payload}B payload, "
        + $"{streamCount} messages per stream");
    Console.WriteLine($"// {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}, "
        + $"{Environment.ProcessorCount} logical cores, client and server in ONE process");
    Console.WriteLine();
    Console.WriteLine($"| {"scenario",-34} | {"ops/sec",10} | {"p50",8} | {"p99",8} |");
    Console.WriteLine($"| {new string('-', 34)} | {new string('-', 10)} | {new string('-', 8)} | {new string('-', 8)} |");

    await Measure("unary, gRPC over HTTP/2", () => Once(overGrpc, request, payload));
    await Measure("unary, Connect over HTTP/2", () => Once(connectOverHttp2, request, payload));
    await Measure("unary, Connect over HTTP/1.1", () => Once(connectOverHttp1, request, payload));

    await Measure($"stream x{streamCount}, gRPC over HTTP/2", () => Drain(overGrpc, request, streamCount, payload));
    await Measure($"stream x{streamCount}, Connect over HTTP/2", () => Drain(connectOverHttp2, request, streamCount, payload));
    await Measure($"stream x{streamCount}, Connect over HTTP/1.1", () => Drain(connectOverHttp1, request, streamCount, payload));

    Console.WriteLine();
    Console.WriteLine("""
        // Caveats, which matter more than the numbers:
        //  - client and server share one process and one set of cores, so both sides contend. That is
        //    fair BETWEEN the rows (each protocol gets the same treatment) and makes none of these
        //    figures a capacity estimate.
        //  - loopback: no real network, so per-call overhead is exaggerated relative to production.
        //  - stock defaults on both stacks; tuning one and not the other is how this goes wrong.
        //  - the HTTP/1.1 rows have no gRPC counterpart by construction. gRPC cannot serve that port.
        //  - and READ THE HTTP/1.1 ROWS CAREFULLY. HttpClient opens additional HTTP/2 connections only
        //    once a connection's streams are exhausted (100 by default), so at this concurrency the
        //    HTTP/2 rows still multiplex onto ONE socket while HTTP/1.1 gets one per worker. That row
        //    is therefore as much a connection-count measurement as a protocol one. What it does show
        //    without qualification is that the option exists at all.
        //  - run for at least 5 seconds. At 3s the unary rows had not settled and read ~35% low,
        //    which invented an anomaly that went away on a longer run.
        """);
}
finally
{
    await app.StopAsync();
}

return 0;

/// <summary>
/// Enumerates a stream and <b>checks it delivered</b>, because a benchmark of a call that quietly
/// does nothing is the easiest wrong number to publish.
/// </summary>
static async Task Drain(IGreeter client, Request request, int expected, int payload)
{
    var seen = 0;
    await foreach (var item in client.StreamAsync(request))
    {
        if (item.Blob is null || item.Blob.Length != payload)
        {
            throw new InvalidOperationException($"stream item {seen} carried {item.Blob?.Length ?? -1} bytes, expected {payload}");
        }
        seen++;
    }
    if (seen != expected) throw new InvalidOperationException($"stream delivered {seen} messages, expected {expected}");
}

static async Task Once(IGreeter client, Request request, int payload)
{
    var reply = await client.UnaryAsync(request);
    if (reply.Blob is null || reply.Blob.Length != payload)
    {
        throw new InvalidOperationException($"unary reply carried {reply.Blob?.Length ?? -1} bytes, expected {payload}");
    }
}

async Task Measure(string name, Func<Task> operation)
{
    // warm up outside the measured window: JIT, the connection handshake, and the first-call
    // serializer resolution all land here rather than in the figure
    for (var i = 0; i < 50; i++) await operation();

    var latencies = new List<double>[concurrency];
    var deadline = Stopwatch.GetTimestamp() + (long)(seconds * Stopwatch.Frequency);
    var started = Stopwatch.GetTimestamp();

    var workers = new Task[concurrency];
    for (var w = 0; w < concurrency; w++)
    {
        var slot = w;
        latencies[slot] = new List<double>(4096);
        workers[slot] = Task.Run(async () =>
        {
            while (Stopwatch.GetTimestamp() < deadline)
            {
                var at = Stopwatch.GetTimestamp();
                await operation();
                latencies[slot].Add(Stopwatch.GetElapsedTime(at).TotalMicroseconds);
            }
        });
    }
    await Task.WhenAll(workers);

    var elapsed = Stopwatch.GetElapsedTime(started).TotalSeconds;
    var all = latencies.SelectMany(x => x).OrderBy(x => x).ToArray();
    Console.WriteLine($"| {name,-34} | {all.Length / elapsed,10:N0} | "
        + $"{Percentile(all, 0.50),7:N0}us | {Percentile(all, 0.99),7:N0}us |");

    static double Percentile(double[] sorted, double p)
        => sorted.Length == 0 ? 0 : sorted[Math.Min(sorted.Length - 1, (int)(sorted.Length * p))];
}

int Arg(string name, int fallback)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out var value)
        ? value : fallback;
}

static int FreePort()
{
    using var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
    probe.Start();
    var port = ((IPEndPoint)probe.LocalEndpoint).Port;
    probe.Stop();
    return port;
}
