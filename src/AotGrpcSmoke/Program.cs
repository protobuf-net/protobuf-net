// A real client and a real server, over a real socket, with both halves generated at compile time:
// the serializers by [ProtoModel] and the proxies/bindings by [ProtoGrpc]. Returns non-zero on any
// mismatch, so it works as a gate.
//
// This is the only thing here that proves the actual goal. Everything else runs on a JIT runtime
// where ref-emit still exists and can quietly paper over a reflective step.
using AotGrpcSmoke;
using Grpc.Net.Client;
using System.IO;
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

    // Client-streaming and duplex: the two of the five shapes the goldens compile but never ran, so
    // until now they were unmeasured under ILC rather than known-good. Each reaches its own Reshape
    // helper on the client and its own AddXxxMethod on the server.
    var summed = await greeter.SumAsync(Requests());
    Check("client-streaming round-trip", summed.Message == "a+b+c" && summed.Count == 6,
        $"{summed.Message} / {summed.Count}");

    var echoed = new List<int>();
    await foreach (var item in greeter.EchoAsync(Requests()))
    {
        echoed.Add(item.Count);
    }
    Check("duplex round-trip", echoed.SequenceEqual(new[] { 2, 4, 6 }), string.Join(",", echoed));

    // THE INTERCEPTOR CHECK. Note what is missing: no factory argument. Written plainly like this the
    // call means `ClientFactory.Default`, i.e. the ref-emit proxy - which ILC has removed, so under a
    // native publish this can only work if the generator rewrote the call to use SmokeServices.Instance.
    // On JIT it would also work reflectively, which is why the native leg is the one that proves it; the
    // proxy type name is asserted for that reason rather than just the round-trip.
    var intercepted = channel.CreateGrpcService<IGreeter>();
    Check("plain CreateGrpcService was intercepted, not reflective",
        intercepted.GetType().Name.EndsWith("_ClientProxy", StringComparison.Ordinal),
        intercepted.GetType().FullName);

    var interceptedReply = await intercepted.SayHelloAsync(new HelloRequest { Name = "intercepted", Count = 7 });
    Check("intercepted client round-trip",
        interceptedReply.Message == "hello, intercepted" && interceptedReply.Count == 7,
        $"{interceptedReply.Message} / {interceptedReply.Count}");

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

    // A byte stream: Task<Stream>, carried as a server-stream of BytesValue with a marshaller that
    // does NOT come from the model. Worth running natively rather than trusting the golden - the
    // golden proves the emitted code compiles, and says nothing about whether the framing works.
    using (var download = await greeter.DownloadAsync(new HelloRequest { Name = "world" }))
    using (var buffer = new MemoryStream())
    {
        await download.CopyToAsync(buffer);
        var text = System.Text.Encoding.UTF8.GetString(buffer.ToArray());
        Check("byte-stream round-trip", text == "stream:world", text);
    }

    // ---- endpoint metadata: does it reach the endpoint once ILC has been through? ----
    //
    // Read off the *routing table*, not by calling GetMetadata: the generated binding now constructs
    // this list at compile time, so asking the binder would test a path the server no longer uses. What
    // the endpoint carries is what ASP.NET Core will enforce, which is the only thing that matters.
    //
    // Until this existed nothing in the fixture carried an attribute, so the metadata path returned an
    // empty list twelve times and was unmeasured rather than fine - and the stake is real, because a
    // dropped [Authorize] is a more permissive endpoint with nothing to notice it.
    var __endpoints = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>()
        .Endpoints;
    var __secure = __endpoints.FirstOrDefault(static e
        => (e as Microsoft.AspNetCore.Routing.RouteEndpoint)?.RoutePattern.RawText
            == "/AotGrpcSmoke.Greeter/Secure");
    Check("the Secure endpoint was bound", __secure is not null,
        string.Join(", ", __endpoints.OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .Select(static e => e.RoutePattern.RawText)));

    var __tags = __secure?.Metadata.OfType<SmokeTagAttribute>().Select(static x => x.Name).ToArray() ?? [];
    Check("endpoint metadata survives: contract method attribute",
        __tags.Contains("contract-method"), string.Join(",", __tags));
    Check("endpoint metadata survives: service type attribute",
        __tags.Contains("service-type"), string.Join(",", __tags));
    Check("endpoint metadata survives: service method attribute",
        __tags.Contains("service-method"), string.Join(",", __tags));

    // the one that is not a fidelity nicety: ASP.NET Core reads this to enforce authorization, so it
    // has to arrive as a constructed instance carrying its arguments, not merely as a type that is named
    var __authorize = __secure?.Metadata.OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
        .FirstOrDefault();
    Check("endpoint metadata survives: [Authorize] arrives as an instance, with its arguments",
        __authorize?.Roles == "admin", __authorize is null ? "absent" : ("Roles=" + __authorize.Roles));

    var bytes = Measure(SmokeModel.Instance, new HelloRequest { Name = "world", Count = 42 });
    Check("generated model serializes the payload",
        Convert.ToHexString(bytes) == "0A05776F726C64102A", Convert.ToHexString(bytes));

    // The generated marshaller has two arms: write into the context's IBufferWriter, or - if the
    // context does not offer one - serialize to an array and hand that over. Every real
    // SerializationContext in grpc-dotnet offers a buffer writer, so the array arm never runs above,
    // on either side of the call, and would happily rot. Drive it directly with a context that
    // refuses.
    //
    // This also pins the state-machine question: SetPayloadLength is called on both arms, and it is
    // only valid from Initialized. It leaves the state there (it just records the length), so the
    // Complete(byte[]) that follows is legal - the same reason Complete() is legal after
    // GetBufferWriter has moved the state on. Both grpc-dotnet contexts, client and server, agree.
    var refusing = new RefusesBufferWriter();
    config.GetMarshaller<HelloRequest>().ContextualSerializer(
        new HelloRequest { Name = "world", Count = 42 }, refusing);
    Check("marshaller's array fallback produces the same bytes",
        refusing.Payload is not null && Convert.ToHexString(refusing.Payload) == "0A05776F726C64102A",
        refusing.Payload is null ? "no payload" : Convert.ToHexString(refusing.Payload));
    Check("...and set a payload length matching them",
        refusing.PayloadLength == bytes.Length, refusing.PayloadLength?.ToString() ?? "unset");
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
// the request stream for the two streaming checks; deliberately three distinct values, so a shape that
// dropped or duplicated one is visible in the totals rather than merely plausible
static async IAsyncEnumerable<HelloRequest> Requests()
{
    yield return new HelloRequest { Name = "a", Count = 1 };
    yield return new HelloRequest { Name = "b", Count = 2 };
    yield return new HelloRequest { Name = "c", Count = 3 };
    await Task.CompletedTask;
}

static byte[] Measure(ProtoBuf.Meta.TypeModel model, HelloRequest value)
{
    using var ms = new MemoryStream();
    model.Serialize(ms, value);
    return ms.ToArray();
}

/// <summary>
/// A <see cref="Grpc.Core.SerializationContext"/> that declines to supply an
/// <c>IBufferWriter&lt;byte&gt;</c>, which is what the base class does and what the generated
/// marshaller's second arm is for.
/// </summary>
/// <remarks>
/// <c>NotImplementedException</c> specifically, because that is what
/// <c>SerializationContext.GetBufferWriter</c> throws when a derived context has not overridden it -
/// the generated code catches that and <c>NotSupportedException</c>, since both spellings are in the
/// wild.
/// </remarks>
file sealed class RefusesBufferWriter : Grpc.Core.SerializationContext
{
    public byte[]? Payload { get; private set; }

    public int? PayloadLength { get; private set; }

    public override void SetPayloadLength(int payloadLength) => PayloadLength = payloadLength;

    public override System.Buffers.IBufferWriter<byte> GetBufferWriter() => throw new NotImplementedException();

    public override void Complete(byte[] payload) => Payload = payload;

    public override void Complete()
        => throw new InvalidOperationException("the array arm must not call the parameterless Complete");
}

