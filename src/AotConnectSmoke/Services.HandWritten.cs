using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProtoBuf.Connect;
using ProtoBuf.Connect.AspNetCore;
using ProtoBuf.Grpc;

namespace ProtoBuf.AotConnectSmoke;

// ---------------------------------------------------------------------------------------------
// THE GENERATED HALF, written by hand. The consumer's half is Services.cs - three lines.
//
// Stage 1 of notes/connect/findings.md §14: get the shape working and reviewed *before* a Roslyn
// generator is taught to emit it. AotRefGen exists so that expected generator output is derived and
// reviewable rather than invented; there is no ref-emit to derive Connect output from, so writing it,
// making it work and reviewing it is the substitute.
//
// It now carries TWO services, which is what [ProtoService] means by "Repeat for each contract" -
// see §26 for what that cost and what it settled.
//
// Properties to preserve when this becomes generated:
//   - nothing here reflects, and nothing here needs to;
//   - the service and method names are the only strings, and they come from the contract;
//   - protobuf-net.Grpc appears HERE, in generated code, not in the runtime libraries;
//   - no accessibility or `static` is restated on the partial: both are the consumer's to choose.
// ---------------------------------------------------------------------------------------------

partial class SmokeServices
{
    /// <summary>There is nothing to construct: every member here is static.</summary>
    private SmokeServices() { }

    /// <summary>Creates a client proxy for one of the contracts this container declares.</summary>
    public static TContract CreateClient<TContract>(ConnectChannel channel) where TContract : class
    {
        if (typeof(TContract) == typeof(IGreeter)) return (TContract)(object)new GreeterClientProxy(channel);
        if (typeof(TContract) == typeof(IFarewell)) return (TContract)(object)new FarewellClientProxy(channel);
        throw new InvalidOperationException(
            "No build-time Connect proxy for " + typeof(TContract).FullName + " in " + nameof(SmokeServices) + ".");
    }

    /// <summary>Maps every service this container declares.</summary>
    /// <remarks>
    /// One call for all of them, because that is what a consumer wants nine times in ten. The returned
    /// builder applies conventions to all of them at once - <c>.RequireAuthorization()</c> here covers
    /// every method of every service. Where that is too broad, bind the services separately with the
    /// generic overload.
    /// </remarks>
    public static IEndpointConventionBuilder BindServer(
        IEndpointRouteBuilder endpoints, string? routingPrefix = null)
        => new CompositeConventionBuilder(
        [
            BindServer<IGreeter>(endpoints, routingPrefix),
            BindServer<IFarewell>(endpoints, routingPrefix),
        ]);

    /// <summary>Maps one service, so that conventions can differ between them.</summary>
    /// <remarks>
    /// Keyed on the <em>contract</em>, matching <see cref="CreateClient{TContract}"/>, even though the
    /// runtime binds by implementation type - the consumer named the pairing once in Services.cs and
    /// should not have to remember which side each API wants.
    /// </remarks>
    public static IEndpointConventionBuilder BindServer<TContract>(
        IEndpointRouteBuilder endpoints, string? routingPrefix = null)
    {
        if (typeof(TContract) == typeof(IGreeter))
        {
            return endpoints.MapConnectService(new GreeterServerBindings(), routingPrefix);
        }

        if (typeof(TContract) == typeof(IFarewell))
        {
            return endpoints.MapConnectService(new FarewellServerBindings(), routingPrefix);
        }

        throw new InvalidOperationException(
            "No build-time Connect bindings for " + typeof(TContract).FullName + " in " + nameof(SmokeServices) + ".");
    }

    /// <summary>
    /// Registers what the server needs: the codec over this container's model, and the service
    /// implementations.
    /// </summary>
    /// <remarks>
    /// The counterpart of <c>GrpcProxyGenerator</c>'s <c>AddXxx</c>, and it exists because the
    /// alternative is a consumer hand-wiring a codec they should not have to know about and then
    /// discovering an unregistered implementation as a DI failure at first call. The model comes from
    /// <c>[ProtoConnect(Model = ...)]</c>, so the generator knows it.
    /// <para>
    /// <c>TryAdd</c> throughout: a consumer who registered an implementation themselves - with
    /// different lifetime, or a decorator - keeps theirs.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddSmokeServices(IServiceCollection services)
    {
        services.AddConnect(options =>
        {
            if (!options.Codecs.Any(c => c.Name == "proto")) options.Codecs.Add(new ProtoConnectCodec(SmokeModel.Instance));
        });
        services.TryAddScoped<GreeterService>();
        services.TryAddScoped<FarewellService>();
        return services;
    }

    // Method descriptors. Named from the contract's SIMPLE name, which is unambiguous here; a
    // generator sees every contract in the container, so it can qualify only on collision rather than
    // always - GrpcProxyGenerator emits `Ns_Sub_IGreeter_ClientProxy` unconditionally, which is safe
    // but reads badly in the common case.
    //
    // The serializers are resolved here, once, in the static initialiser - not per message.
    private static class Greeter
    {
        public const string ServiceName = "aotconnectsmoke.v1.Greeter";

        public static readonly ConnectMethod<HelloRequest, HelloReply> SayHello = Unary("SayHello");
        public static readonly ConnectMethod<HelloRequest, HelloReply> Refuse = Unary("Refuse");
        public static readonly ConnectMethod<HelloRequest, HelloReply> Explode = Unary("Explode");
        public static readonly ConnectMethod<HelloRequest, HelloReply> Dawdle = Unary("Dawdle");
        public static readonly ConnectMethod<HelloRequest, HelloReply> Subscribe = Streaming("Subscribe");
        public static readonly ConnectMethod<HelloRequest, HelloReply> SubscribeThenFail = Streaming("SubscribeThenFail");
        public static readonly ConnectMethod<HelloRequest, HelloReply> Collect =
            Method(ConnectMethodType.ClientStreaming, "Collect");
        public static readonly ConnectMethod<HelloRequest, HelloReply> Chat =
            Method(ConnectMethodType.DuplexStreaming, "Chat");

        private static ConnectMethod<HelloRequest, HelloReply> Unary(string name)
            => Method(ConnectMethodType.Unary, name);

        private static ConnectMethod<HelloRequest, HelloReply> Streaming(string name)
            => Method(ConnectMethodType.ServerStreaming, name);

        private static ConnectMethod<HelloRequest, HelloReply> Method(ConnectMethodType type, string name)
            => new(type, ServiceName, name,
                requestSerializer: SmokeModel.Serializer<HelloRequest>(),
                responseSerializer: SmokeModel.Serializer<HelloReply>());
    }

    private static class Farewell
    {
        public const string ServiceName = "aotconnectsmoke.v1.Farewell";

        public static readonly ConnectMethod<HelloRequest, HelloReply> Goodbye =
            Method(ConnectMethodType.Unary, "Goodbye");
        public static readonly ConnectMethod<HelloRequest, HelloReply> Wave =
            Method(ConnectMethodType.ServerStreaming, "Wave");

        private static ConnectMethod<HelloRequest, HelloReply> Method(ConnectMethodType type, string name)
            => new(type, ServiceName, name,
                requestSerializer: SmokeModel.Serializer<HelloRequest>(),
                responseSerializer: SmokeModel.Serializer<HelloReply>());
    }

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

        public IAsyncEnumerable<HelloReply> Subscribe(HelloRequest request, CallContext context = default)
            => _channel.ServerStreaming(Greeter.Subscribe, request,
                ConnectCallOptions.From(context.CallOptions), context.CancellationToken);

        public IAsyncEnumerable<HelloReply> SubscribeThenFail(HelloRequest request, CallContext context = default)
            => _channel.ServerStreaming(Greeter.SubscribeThenFail, request,
                ConnectCallOptions.From(context.CallOptions), context.CancellationToken);

        public Task<HelloReply> CollectAsync(IAsyncEnumerable<HelloRequest> requests, CallContext context = default)
            => _channel.ClientStreamingAsync(Greeter.Collect, requests,
                ConnectCallOptions.From(context.CallOptions), context.CancellationToken);

        public IAsyncEnumerable<HelloReply> Chat(IAsyncEnumerable<HelloRequest> requests, CallContext context = default)
            => _channel.Duplex(Greeter.Chat, requests,
                ConnectCallOptions.From(context.CallOptions), context.CancellationToken);
    }

    private sealed class FarewellClientProxy : IFarewell
    {
        private readonly ConnectChannel _channel;

        public FarewellClientProxy(ConnectChannel channel) => _channel = channel;

        public Task<HelloReply> GoodbyeAsync(HelloRequest request, CallContext context = default)
            => _channel.UnaryAsync(Farewell.Goodbye, request,
                ConnectCallOptions.From(context.CallOptions), context.CancellationToken);

        public IAsyncEnumerable<HelloReply> WaveAsync(HelloRequest request, CallContext context = default)
            => _channel.ServerStreaming(Farewell.Wave, request,
                ConnectCallOptions.From(context.CallOptions), context.CancellationToken);
    }

    private sealed class GreeterServerBindings : IConnectServiceBinder<GreeterService>
    {
        public void Bind(ConnectServiceBinderContext<GreeterService> context)
        {
            // `new CallContext(service, ctx)` is the same line GrpcProxyGenerator emits; it is what
            // keeps protobuf-net.Grpc out of protobuf-net.Connect.AspNetCore
            context.AddUnaryMethod(Greeter.SayHello,
                static (service, request, ctx) => service.SayHelloAsync(request, new CallContext(service, ctx)));
            context.AddUnaryMethod(Greeter.Refuse,
                static (service, request, ctx) => service.RefuseAsync(request, new CallContext(service, ctx)));
            context.AddUnaryMethod(Greeter.Explode,
                static (service, request, ctx) => service.ExplodeAsync(request, new CallContext(service, ctx)));
            context.AddUnaryMethod(Greeter.Dawdle,
                static (service, request, ctx) => service.DawdleAsync(request, new CallContext(service, ctx)));
            context.AddServerStreamingMethod(Greeter.Subscribe,
                static (service, request, ctx) => service.Subscribe(request, new CallContext(service, ctx)));
            context.AddServerStreamingMethod(Greeter.SubscribeThenFail,
                static (service, request, ctx) => service.SubscribeThenFail(request, new CallContext(service, ctx)));
            context.AddClientStreamingMethod(Greeter.Collect,
                static (service, requests, ctx) => service.CollectAsync(requests, new CallContext(service, ctx)));
            context.AddDuplexMethod(Greeter.Chat,
                static (service, requests, ctx) => service.Chat(requests, new CallContext(service, ctx)));
        }
    }

    private sealed class FarewellServerBindings : IConnectServiceBinder<FarewellService>
    {
        public void Bind(ConnectServiceBinderContext<FarewellService> context)
        {
            context.AddUnaryMethod(Farewell.Goodbye,
                static (service, request, ctx) => service.GoodbyeAsync(request, new CallContext(service, ctx)));
            context.AddServerStreamingMethod(Farewell.Wave,
                static (service, request, ctx) => service.WaveAsync(request, new CallContext(service, ctx)));
        }
    }

    /// <summary>Applies conventions across several services' endpoints at once.</summary>
    private sealed class CompositeConventionBuilder : IEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder[] _inner;

        public CompositeConventionBuilder(IEndpointConventionBuilder[] inner) => _inner = inner;

        public void Add(Action<EndpointBuilder> convention)
        {
            foreach (var builder in _inner) builder.Add(convention);
        }

        public void Finally(Action<EndpointBuilder> finallyConvention)
        {
            foreach (var builder in _inner) builder.Finally(finallyConvention);
        }
    }
}

/// <summary>
/// The idiomatic front doors, as <c>GrpcProxyGenerator</c>'s <c>AddXxx</c> is for gRPC: ASP.NET Core
/// consumers reach for <c>services.AddXxx()</c> and <c>app.MapXxx()</c>. Both are one-line aliases and
/// add no capability of their own.
/// </summary>
/// <remarks>
/// <c>internal</c> because the container is: the generated surface mirrors whatever the consumer
/// declared in Services.cs rather than picking for them.
/// </remarks>
internal static class SmokeServicesExtensions
{
    internal static IServiceCollection AddSmokeServices(this IServiceCollection services)
        => SmokeServices.AddSmokeServices(services);

    internal static IEndpointConventionBuilder MapSmokeServices(
        this IEndpointRouteBuilder endpoints, string? routingPrefix = null)
        => SmokeServices.BindServer(endpoints, routingPrefix);
}
