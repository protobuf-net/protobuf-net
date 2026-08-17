// A real client and a real server, over a real socket, with both halves generated at compile time:
// the serializers by [ProtoModel] and the proxies/bindings by [ProtoGrpc]. Returns non-zero on any
// mismatch, so it works as a gate.
//
// This is the only thing here that proves the actual goal. Everything else runs on a JIT runtime
// where ref-emit still exists and can quietly paper over a reflective step.
using AotGrpcSmoke;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc.Client;

var failures = new List<string>();

void Check(string what, bool ok, string? detail = null)
{
    Console.WriteLine($"{(ok ? "ok  " : "FAIL")}  {what}{(detail is null ? "" : "  -> " + detail)}");
    if (!ok) failures.Add(what);
}

// ---- the server: note AddSmokeServices(), generated into this assembly, not AddCodeFirstGrpc() ----
var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);
builder.WebHost.ConfigureKestrel(static options
    => options.ListenLocalhost(5199, static listen => listen.Protocols
        = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2));

builder.Services.AddSmokeServices();

var app = builder.Build();
app.MapGrpcService<GreeterService>();
await app.StartAsync();

try
{
    using var channel = GrpcChannel.ForAddress("http://localhost:5199");

    // ---- the client: the factory is named, so nothing is resolved by Type at run time ----
    var greeter = channel.CreateGrpcService<IGreeter>(SmokeServices.Instance);

    Check("client proxy is the generated one, not a ref-emit proxy",
        greeter.GetType().Name.EndsWith("_ClientProxy", StringComparison.Ordinal),
        greeter.GetType().FullName);

    var reply = await greeter.SayHelloAsync(new HelloRequest { Name = "world", Count = 42 });
    Check("unary round-trip", reply.Message == "hello, world" && reply.Count == 42,
        $"{reply.Message} / {reply.Count}");

    var seen = new List<int>();
    await foreach (var item in greeter.CountAsync(new HelloRequest { Name = "n", Count = 3 }))
    {
        seen.Add(item.Count);
    }
    Check("server-streaming round-trip", seen.SequenceEqual(new[] { 0, 1, 2 }),
        string.Join(",", seen));

    // A void response, i.e. ProtoBuf.Grpc.Internal.Empty on the wire. Worth its own check because its
    // marshaller comes from MarshallerCache's pre-seeded entry rather than from the generated
    // SetMarshaller block - and because it must NOT have been seeded into the model, which carries no
    // [ProtoSerializable] at all. Asserted on the server's own state, since there is nothing to return.
    await greeter.NudgeAsync(new HelloRequest { Name = "nudged" });
    Check("void round-trip (Empty on the wire)", GreeterService.Nudged == "nudged",
        GreeterService.Nudged);

    // The payload side is the half that neither source PR closes: proxies can be perfectly static
    // and still marshal through RuntimeTypeModel.Default, which reflects. Two checks, because
    // neither alone is conclusive on a JIT run:
    //
    //  - the configuration is not the process-wide default, so the marshallers cannot be coming from
    //    RuntimeTypeModel.Default;
    //  - the generated model serializes the payload itself, to the bytes that went over the wire.
    //
    // What actually settles it is the *native* publish: under AOT the reflective model cannot build
    // a serializer at all, so a round-trip there can only have come from SmokeModel.
    ProtoBuf.Grpc.Configuration.BinderConfiguration config = SmokeServices.Instance;
    Check("binder configuration is not the reflective default",
        !ReferenceEquals(config, ProtoBuf.Grpc.Configuration.BinderConfiguration.Default));

    var bytes = Measure(SmokeModel.Instance, new HelloRequest { Name = "world", Count = 42 });
    Check("generated model serializes the payload",
        Convert.ToHexString(bytes) == "0A05776F726C64102A", Convert.ToHexString(bytes));
}
finally
{
    await app.StopAsync();
}

Console.WriteLine(failures.Count == 0
    ? "\nAll checks passed."
    : $"\n{failures.Count} check(s) FAILED: {string.Join("; ", failures)}");
return failures.Count == 0 ? 0 : 1;

// deliberately concrete rather than generic: a generic helper would have to restate
// TypeModel.Serialize<T>'s [DynamicallyAccessedMembers] demand, which is a claim about reflection
// this path does not make. Same "which axis does the annotation belong on" question the AOT notes
// in AGENTS.md record.
static byte[] Measure(ProtoBuf.Meta.TypeModel model, HelloRequest value)
{
    using var ms = new MemoryStream();
    model.Serialize(ms, value);
    return ms.ToArray();
}

