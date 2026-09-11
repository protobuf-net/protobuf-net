using System.Collections.Generic;
using Grpc.Core;
using ProtoBuf.Connect;
using ProtoBuf.Connect.AspNetCore;
using ProtoBuf.Grpc;

namespace ProtoBuf.AotConnectSmoke;

// ---------------------------------------------------------------------------------------------
// THIS FILE IS THE GENERATOR'S TARGET OUTPUT, written by hand.
//
// Stage 1 of notes/connect/findings.md §14: get the shape working and reviewed *before* a Roslyn
// generator is taught to emit it. The repo already works this way - AotRefGen exists so that expected
// generator output is derived and reviewable rather than invented - and here there is no ref-emit to
// derive from, so writing it, making it work and reviewing it is the substitute.
//
// Three properties to preserve when this becomes generated:
//   - nothing here reflects, and nothing here needs to;
//   - the service and method names are the only strings, and they come from the contract;
//   - protobuf-net.Grpc appears HERE, in generated code, and not in the runtime libraries. That is
//     what keeps the v2/v3 TypeModel collision in the consumer's project, where a reference to
//     protobuf-net v3 resolves it - and it is exactly what GrpcProxyGenerator already does, whose
//     server bindings likewise construct the CallContext themselves.
// ---------------------------------------------------------------------------------------------

/// <summary>The methods of <see cref="IGreeter"/>, shared by the client proxy and the server bindings.</summary>
/// <remarks>
/// The serializers are resolved <em>here</em>, once, in the static initialiser - not per message. This
/// is the closest thing to a marshaller in the design, and unlike protobuf-net.Grpc's it is not working
/// around anything: there is no MarshallerCache and no CanSerialize gate on this path (§18). It simply
/// hoists the model lookup - a static field read, a virtual call and a second static field read - out
/// of every request.
/// </remarks>
internal static class GreeterMethods
{
    public const string ServiceName = "aotconnectsmoke.v1.Greeter";

    public static readonly ConnectMethod<HelloRequest, HelloReply> SayHello =
        new(ConnectMethodType.Unary, ServiceName, "SayHello",
            requestSerializer: SmokeModel.Serializer<HelloRequest>(),
            responseSerializer: SmokeModel.Serializer<HelloReply>());

    public static readonly ConnectMethod<HelloRequest, HelloReply> Refuse =
        new(ConnectMethodType.Unary, ServiceName, "Refuse",
            requestSerializer: SmokeModel.Serializer<HelloRequest>(),
            responseSerializer: SmokeModel.Serializer<HelloReply>());

    public static readonly ConnectMethod<HelloRequest, HelloReply> Explode =
        new(ConnectMethodType.Unary, ServiceName, "Explode",
            requestSerializer: SmokeModel.Serializer<HelloRequest>(),
            responseSerializer: SmokeModel.Serializer<HelloReply>());

    public static readonly ConnectMethod<HelloRequest, HelloReply> Dawdle =
        new(ConnectMethodType.Unary, ServiceName, "Dawdle",
            requestSerializer: SmokeModel.Serializer<HelloRequest>(),
            responseSerializer: SmokeModel.Serializer<HelloReply>());
}

/// <summary>
/// Converts between protobuf-net.Grpc's <see cref="CallContext"/> and the Connect transport's own
/// options. Emitted once per assembly; it is the only place the two vocabularies meet.
/// </summary>
internal static class ConnectCallContextBridge
{
    /// <summary>Client side: a <see cref="CallContext"/>'s deadline and metadata, as transport options.</summary>
    public static ConnectCallOptions? ToOptions(in CallContext context)
    {
        var options = context.CallOptions;
        var deadline = options.Deadline;
        var headers = options.Headers;

        if (deadline is null && (headers is null || headers.Count == 0)) return null;

        List<KeyValuePair<string, string>>? converted = null;
        if (headers is not null)
        {
            foreach (var entry in headers)
            {
                // binary metadata travels as unpadded base64 under a -bin suffixed name
                (converted ??= new()).Add(new(
                    entry.Key, entry.IsBinary ? System.Convert.ToBase64String(entry.ValueBytes) : entry.Value));
            }
        }

        return new ConnectCallOptions
        {
            // gRPC states an absolute deadline; Connect states a relative timeout
            Timeout = deadline is { } at && at != System.DateTime.MaxValue
                ? at - System.DateTime.UtcNow
                : null,
            Headers = converted,
        };
    }
}

/// <summary>Server-side bindings: one typed delegate per method, no reflection.</summary>
internal sealed class GreeterBindings : IConnectServiceBinder<GreeterService>
{
    public void Bind(ConnectServiceBinderContext<GreeterService> context)
    {
        // `new CallContext(service, ctx)` is the same line GrpcProxyGenerator emits into its server
        // bindings; it is what keeps protobuf-net.Grpc out of protobuf-net.Connect.AspNetCore
        context.AddUnaryMethod(GreeterMethods.SayHello,
            static (service, request, ctx) => service.SayHelloAsync(request, new CallContext(service, ctx)));
        context.AddUnaryMethod(GreeterMethods.Refuse,
            static (service, request, ctx) => service.RefuseAsync(request, new CallContext(service, ctx)));
        context.AddUnaryMethod(GreeterMethods.Explode,
            static (service, request, ctx) => service.ExplodeAsync(request, new CallContext(service, ctx)));
        context.AddUnaryMethod(GreeterMethods.Dawdle,
            static (service, request, ctx) => service.DawdleAsync(request, new CallContext(service, ctx)));
    }
}

/// <summary>
/// Client proxy: the same contract, over a <see cref="ConnectChannel"/>.
/// </summary>
/// <remarks>
/// Note the signatures are <see cref="IGreeter"/>'s, unchanged - a caller written against the
/// protobuf-net.Grpc client sees no difference at all.
/// </remarks>
internal sealed class GreeterClient : IGreeter
{
    private readonly ConnectChannel _channel;

    public GreeterClient(ConnectChannel channel) => _channel = channel;

    public Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default)
        => _channel.UnaryAsync(GreeterMethods.SayHello, request,
            ConnectCallContextBridge.ToOptions(context), context.CancellationToken);

    public Task<HelloReply> RefuseAsync(HelloRequest request, CallContext context = default)
        => _channel.UnaryAsync(GreeterMethods.Refuse, request,
            ConnectCallContextBridge.ToOptions(context), context.CancellationToken);

    public Task<HelloReply> ExplodeAsync(HelloRequest request, CallContext context = default)
        => _channel.UnaryAsync(GreeterMethods.Explode, request,
            ConnectCallContextBridge.ToOptions(context), context.CancellationToken);

    public Task<HelloReply> DawdleAsync(HelloRequest request, CallContext context = default)
        => _channel.UnaryAsync(GreeterMethods.Dawdle, request,
            ConnectCallContextBridge.ToOptions(context), context.CancellationToken);

    /// <summary>Exposes the response metadata, which the contract's own signature does not carry.</summary>
    public Task<(HelloReply Response, ConnectCallResult Call)> SayHelloWithMetadataAsync(
        HelloRequest request, ConnectCallOptions? options = null)
        => _channel.UnaryWithMetadataAsync(GreeterMethods.SayHello, request, options);
}
