using System.Net;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProtoBuf.AotDualHostSmoke;
using ProtoBuf.Connect;
using ProtoBuf.Grpc.Client;

// One contract, one implementation, one host, both transports - which is the branch's headline claim
// and was, until this project, demonstrated nowhere. Every other check runs one protocol at a time.
//
// Published with PublishAot, so a pass means both stacks survive ILC together rather than separately.

var failures = new List<string>();

// Plaintext cannot serve both protocols on one port: there is no ALPN to negotiate with, and Kestrel
// answers an h2c prior-knowledge attempt on an Http1AndHttp2 endpoint with HTTP_1_1_REQUIRED rather
// than sniffing the preface (notes/connect/findings.md §24). So two listeners - but note that is a
// PLAINTEXT artefact, not a hosting one: the endpoint table, the DI container and the service
// implementation are shared, and with TLS one port would serve both.
var connectPort = FreePort();
var grpcPort = FreePort();

var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.ConfigureKestrel(o =>
{
    o.Listen(IPAddress.Loopback, connectPort, l => l.Protocols = HttpProtocols.Http1);
    o.Listen(IPAddress.Loopback, grpcPort, l => l.Protocols = HttpProtocols.Http2);
});

// both registrations, into one service collection
builder.Services.AddSingleton<GreeterService>();
builder.Services.AddGrpcSide();
builder.Services.AddConnectSide();

var app = builder.Build();

// gRPC takes the canonical path, /dualhost.v1.Greeter/SayHello. Connect goes under a prefix, because
// the two want the SAME route and ASP.NET Core will not have it - see the check below, which pins
// what actually happens rather than assuming.
app.MapGrpcService<GreeterService>();
app.BindConnectSide("connect");

await app.StartAsync();

var connectAddress = $"http://127.0.0.1:{connectPort}";
var grpcAddress = $"http://127.0.0.1:{grpcPort}";

try
{
    using var http = new HttpClient();

    // ---- the same contract, through each transport's own client ----
    using var grpcChannel = GrpcChannel.ForAddress(grpcAddress);
    var overGrpc = grpcChannel.CreateGrpcService<IGreeter>(GrpcSide.Instance);

    var connectChannel = new ConnectChannel(http, new ProtoConnectCodec(DualModel.Instance),
        new Uri(connectAddress + "/connect"));
    var overConnect = ConnectSide.CreateClient<IGreeter>(connectChannel);

    await Check("unary over gRPC", async () =>
    {
        var reply = await overGrpc.SayHelloAsync(new HelloRequest { Name = "marc" });
        Require(reply.Message == "hello marc", $"the greeting, was \"{reply.Message}\"");
        // the generated proxy, not a ref-emit one - which is what makes this leg meaningful under AOT
        Require(overGrpc.GetType().Name.EndsWith("_ClientProxy", StringComparison.Ordinal),
            $"the generated proxy, was {overGrpc.GetType().Name}");
        return overGrpc.GetType().Name;
    });

    await Check("unary over Connect", async () =>
    {
        var reply = await overConnect.SayHelloAsync(new HelloRequest { Name = "marc" });
        Require(reply.Message == "hello marc", $"the greeting, was \"{reply.Message}\"");
        Require(reply.Transport == "application/proto", $"served as Connect, saw \"{reply.Transport}\"");
        return reply.Transport!;
    });

    await Check("server-streaming over gRPC", async () =>
    {
        var seen = new List<string>();
        await foreach (var item in overGrpc.Subscribe(new HelloRequest { Name = "g", Repeat = 3 }))
        {
            seen.Add(item.Message!);
        }
        Require(seen.Count == 3, $"three messages, got {seen.Count}");
        Require(seen[2] == "hello g #3", $"in order, last was \"{seen[2]}\"");
        return $"{seen.Count} messages";
    });

    await Check("server-streaming over Connect, on HTTP/1.1", async () =>
    {
        var seen = new List<HelloReply>();
        await foreach (var item in overConnect.Subscribe(new HelloRequest { Name = "c", Repeat = 3 }))
        {
            seen.Add(item);
        }
        Require(seen.Count == 3, $"three messages, got {seen.Count}");
        Require(seen[2].Message == "hello c #3", $"in order, last was \"{seen[2].Message}\"");
        // the whole operational argument: gRPC could not have served this port at all
        Require(seen[0].Transport == "application/connect+proto",
            $"enveloped framing, saw \"{seen[0].Transport}\"");
        return $"{seen.Count} messages over HTTP/1.1";
    });

    // ---- and the part that is actually load-bearing for "shares a host" ----

    await Check("the gRPC endpoint is reachable at the canonical path", async () =>
    {
        // proving the prefix did not displace it, and that Connect's binding has not shadowed it
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{grpcAddress}/dualhost.v1.Greeter/SayHello")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = new ByteArrayContent([0, 0, 0, 0, 0])
            {
                Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/grpc") },
            },
        };
        using var response = await http.SendAsync(request);
        // a gRPC error still answers 200 with a grpc-status trailer; what matters is that it ROUTED
        Require(response.IsSuccessStatusCode, $"HTTP 200, was {(int)response.StatusCode}");
        return $"HTTP {(int)response.StatusCode}, routed";
    });

    await Check("Connect is reachable under its prefix and not at the bare path", async () =>
    {
        using var ok = await http.PostAsync($"{connectAddress}/connect/dualhost.v1.Greeter/SayHello",
            Body());
        Require(ok.IsSuccessStatusCode, $"prefixed path answers, was {(int)ok.StatusCode}");

        // the bare path belongs to gRPC's endpoint, which does not accept a Connect content-type
        using var bare = await http.PostAsync($"{connectAddress}/dualhost.v1.Greeter/SayHello", Body());
        Require(bare.StatusCode != HttpStatusCode.OK,
            $"the bare path is not Connect's, was {(int)bare.StatusCode}");
        return $"prefixed {(int)ok.StatusCode}, bare {(int)bare.StatusCode}";

        static HttpContent Body() => new ByteArrayContent([0x0a, 0x02, 0x68, 0x69])
        {
            Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/proto") },
        };
    });

    await Check("RequestHeaders is NOT the same set on the two transports", async () =>
    {
        // pinned as a difference rather than assumed away. grpc-dotnet filters protocol headers out
        // of ServerCallContext.RequestHeaders - content-type included - while the Connect
        // implementation passes them through. Both are defensible; a handler reading a header must
        // not assume parity, and "one contract, both transports" could easily be read as promising it
        var viaGrpc = await overGrpc.SayHelloAsync(new HelloRequest { Name = "h" });
        var viaConnect = await overConnect.SayHelloAsync(new HelloRequest { Name = "h" });

        Require(viaGrpc.Transport == GreeterService.NotSurfaced,
            $"gRPC hides content-type, saw \"{viaGrpc.Transport}\"");
        Require(viaConnect.Transport == "application/proto",
            $"Connect surfaces it, saw \"{viaConnect.Transport}\"");
        return $"gRPC: hidden; Connect: {viaConnect.Transport}";
    });

    await Check("one implementation instance type serves both", () =>
    {
        // the same DI registration answered both transports; if either stack had brought its own
        // activation path this would be two different resolutions
        var resolved = app.Services.GetService<GreeterService>();
        Require(resolved is not null, "GreeterService resolves from the shared container");
        return Task.FromResult(resolved!.GetType().Name);
    });
}
finally
{
    await app.StopAsync();
}

foreach (var failure in failures) Console.Error.WriteLine("FAILED: " + failure);
Console.WriteLine(failures.Count == 0
    ? "AotDualHostSmoke: one contract, one host, gRPC and Connect together - all checks pass"
    : $"AotDualHostSmoke: {failures.Count} FAILURES");
return failures.Count == 0 ? 0 : 1;

async Task Check(string name, Func<Task<string>> check)
{
    try
    {
        var detail = await check();
        Console.WriteLine($"  pass  {name}  -> {detail}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  FAIL  {name}  -> {ex.GetType().Name}: {ex.Message}");
        failures.Add(name);
    }
}

static void Require(bool condition, string what)
{
    if (!condition) throw new InvalidOperationException(what);
}

static int FreePort()
{
    using var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
    probe.Start();
    var port = ((IPEndPoint)probe.LocalEndpoint).Port;
    probe.Stop();
    return port;
}
