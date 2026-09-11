using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
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
// The shape mirrors GrpcProxyGenerator's output: everything is nested inside one consumer-declared
// partial class, which is what a [ProtoConnect(Model = typeof(SmokeModel))] attribute would mark. That
// container is not decoration - it is where the model is named, where CreateClient<T> lives, and what
// the registration extension hangs off. SmokeServices stands in for it here.
//
// Three properties to preserve when this becomes generated:
//   - nothing here reflects, and nothing here needs to;
//   - the service and method names are the only strings, and they come from the contract;
//   - protobuf-net.Grpc appears HERE, in generated code, and not in the runtime libraries. That is
//     what keeps the v2/v3 TypeModel collision in the consumer's project, where a reference to
//     protobuf-net v3 resolves it. Note the collision is per-*usage*, not per-assembly: it fires only
//     where TypeModel is named, which is why ConnectCallOptions.From can live in the library.
// ---------------------------------------------------------------------------------------------

/// <summary>Stands in for a <c>[ProtoConnect(Model = typeof(SmokeModel))] partial class</c>.</summary>
internal sealed class SmokeServices
{
    /// <summary>Use <see cref="Instance"/>; this holds per-service state and is meant to be shared.</summary>
    private SmokeServices() { }

    /// <summary>A shared instance; thread-safe, and intended to be reused.</summary>
    public static SmokeServices Instance { get; } = new SmokeServices();

    /// <summary>Creates a client proxy for one of the services this container knows about.</summary>
    public TService CreateClient<TService>(ConnectChannel channel) where TService : class
    {
        if (typeof(TService) == typeof(IGreeter)) return (TService)(object)new GreeterClientProxy(channel);
        throw new InvalidOperationException(
            "No build-time Connect proxy for " + typeof(TService).FullName + " in " + nameof(SmokeServices) + ".");
    }

    /// <summary>
    /// The method descriptors for <see cref="IGreeter"/>, shared by the proxy and the bindings.
    /// </summary>
    /// <remarks>
    /// The serializers are resolved <em>here</em>, once, in the static initialiser - not per message.
    /// This is the closest thing to a marshaller in the design, and unlike protobuf-net.Grpc's it is
    /// not working around anything: there is no MarshallerCache and no CanSerialize gate on this path
    /// (§18). It simply hoists the model lookup out of every request.
    /// </remarks>
    internal static class Greeter
    {
        public const string ServiceName = "aotconnectsmoke.v1.Greeter";

        public static readonly ConnectMethod<HelloRequest, HelloReply> SayHello = Unary("SayHello");
        public static readonly ConnectMethod<HelloRequest, HelloReply> Refuse = Unary("Refuse");
        public static readonly ConnectMethod<HelloRequest, HelloReply> Explode = Unary("Explode");
        public static readonly ConnectMethod<HelloRequest, HelloReply> Dawdle = Unary("Dawdle");

        private static ConnectMethod<HelloRequest, HelloReply> Unary(string name)
            => new(ConnectMethodType.Unary, ServiceName, name,
                requestSerializer: SmokeModel.Serializer<HelloRequest>(),
                responseSerializer: SmokeModel.Serializer<HelloReply>());
    }

    /// <summary>
    /// Client proxy. Private: a consumer reaches it through <see cref="CreateClient{TService}"/> and
    /// only ever sees <see cref="IGreeter"/>, so the proxy type itself is not API.
    /// </summary>
    /// <remarks>
    /// The signatures are <see cref="IGreeter"/>'s, unchanged - a caller written against the
    /// protobuf-net.Grpc client sees no difference at all.
    /// </remarks>
    private sealed class GreeterClientProxy : IGreeter
    {
        private readonly ConnectChannel _channel;

        public GreeterClientProxy(ConnectChannel channel) => _channel = channel;

        public Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default)
            => _channel.UnaryAsync(Greeter.SayHello, request,
                ConnectCallOptions.From(context.CallOptions), context.CancellationToken);

        public Task<HelloReply> RefuseAsync(HelloRequest request, CallContext context = default)
            => _channel.UnaryAsync(Greeter.Refuse, request,
                ConnectCallOptions.From(context.CallOptions), context.CancellationToken);

        public Task<HelloReply> ExplodeAsync(HelloRequest request, CallContext context = default)
            => _channel.UnaryAsync(Greeter.Explode, request,
                ConnectCallOptions.From(context.CallOptions), context.CancellationToken);

        public Task<HelloReply> DawdleAsync(HelloRequest request, CallContext context = default)
            => _channel.UnaryAsync(Greeter.Dawdle, request,
                ConnectCallOptions.From(context.CallOptions), context.CancellationToken);
    }

    /// <summary>
    /// Server bindings: one typed delegate per method, no reflection. Internal rather than private
    /// only because the registration extension below has to construct it.
    /// </summary>
    internal sealed class GreeterServerBindings : IConnectServiceBinder<GreeterService>
    {
        public void Bind(ConnectServiceBinderContext<GreeterService> context)
        {
            // `new CallContext(service, ctx)` is the same line GrpcProxyGenerator emits into its server
            // bindings; it is what keeps protobuf-net.Grpc out of protobuf-net.Connect.AspNetCore
            context.AddUnaryMethod(Greeter.SayHello,
                static (service, request, ctx) => service.SayHelloAsync(request, new CallContext(service, ctx)));
            context.AddUnaryMethod(Greeter.Refuse,
                static (service, request, ctx) => service.RefuseAsync(request, new CallContext(service, ctx)));
            context.AddUnaryMethod(Greeter.Explode,
                static (service, request, ctx) => service.ExplodeAsync(request, new CallContext(service, ctx)));
            context.AddUnaryMethod(Greeter.Dawdle,
                static (service, request, ctx) => service.DawdleAsync(request, new CallContext(service, ctx)));
        }
    }
}

/// <summary>Registers everything <see cref="SmokeServices"/> declares, as GrpcProxyGenerator's
/// <c>AddXxx</c> extension does for gRPC.</summary>
internal static class SmokeServicesEndpointExtensions
{
    internal static IEndpointConventionBuilder MapSmokeServices(this IEndpointRouteBuilder endpoints)
        => endpoints.MapConnectService(new SmokeServices.GreeterServerBindings());
}
