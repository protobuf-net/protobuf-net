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

// The consumer declared this container `static` (Services.cs), so no constructor is emitted - a static
// class cannot have one. That is the ONLY thing `static` changes: everything fluent lives in the
// companion SmokeServicesExtensions below, which is emitted either way, so the generator does not
// branch on where things go. See ClientOnly.HandWritten.cs for a non-static container.
static partial class SmokeServices
{

    /// <summary>Creates a client proxy for one of the contracts this container declares.</summary>
    public static TService CreateClient<TService>(ConnectChannel channel) where TService : class
    {
        if (typeof(TService) == typeof(IGreeter)) return (TService)(object)new GreeterClientProxy(channel);
        if (typeof(TService) == typeof(IFarewell)) return (TService)(object)new FarewellClientProxy(channel);
        throw new InvalidOperationException(
            "No build-time Connect proxy for " + typeof(TService).FullName + " in " + nameof(SmokeServices) + ".");
    }

    /// <summary>Maps every service this container declares.</summary>
    /// <remarks>
    /// One call for all of them, because that is what a consumer wants nine times in ten. The returned
    /// builder applies conventions to all of them at once - <c>.RequireAuthorization()</c> here covers
    /// every method of every service. Where that is too broad, bind the services separately with the
    /// generic overload.
    /// </remarks>
    public static IEndpointConventionBuilder BindServices(
        IEndpointRouteBuilder endpoints, string? routingPrefix = null)
        => new CompositeConventionBuilder(
        [
            BindService<IGreeter>(endpoints, routingPrefix),
            BindService<IFarewell>(endpoints, routingPrefix),
        ]);

    /// <summary>Maps one service, so that conventions can differ between them.</summary>
    /// <remarks>
    /// Keyed on the <em>contract</em>, matching <see cref="CreateClient{TService}"/>, even though the
    /// runtime binds by implementation type - the consumer named the pairing once in Services.cs and
    /// should not have to remember which side each API wants.
    /// </remarks>
    public static IEndpointConventionBuilder BindService<TService>(
        IEndpointRouteBuilder endpoints, string? routingPrefix = null)
    {
        if (typeof(TService) == typeof(IGreeter))
        {
            return endpoints.MapConnectService(new GreeterServerBindings(), routingPrefix);
        }

        if (typeof(TService) == typeof(IFarewell))
        {
            return endpoints.MapConnectService(new FarewellServerBindings(), routingPrefix);
        }

        throw new InvalidOperationException(
            "No build-time Connect bindings for " + typeof(TService).FullName + " in " + nameof(SmokeServices) + ".");
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
    public static IServiceCollection AddServices(IServiceCollection services)
    {
        AddCodec(services);
        services.TryAddScoped<GreeterService>();
        services.TryAddScoped<FarewellService>();
        return services;
    }

    /// <summary>Registers what one service needs: the codec, and that service's implementation.</summary>
    public static IServiceCollection AddService<TService>(IServiceCollection services)
    {
        AddCodec(services);
        if (typeof(TService) == typeof(IGreeter)) { services.TryAddScoped<GreeterService>(); return services; }
        if (typeof(TService) == typeof(IFarewell)) { services.TryAddScoped<FarewellService>(); return services; }
        throw new InvalidOperationException(
            "No build-time Connect bindings for " + typeof(TService).FullName + " in " + nameof(SmokeServices) + ".");
    }

    /// <summary>
    /// The container-level half of registration: the codec over this container's model.
    /// </summary>
    /// <remarks>
    /// Separate because it is <em>not</em> per-service, and every <c>Add</c> entry point needs it - so
    /// calling <c>AddGreeter()</c> and <c>AddFarewell()</c> must not register it twice. The guard is
    /// inside the configure delegate rather than around <c>AddConnect</c>, because options delegates
    /// accumulate and all of them run: checking at registration time would look right and still add two.
    /// </remarks>
    private static void AddCodec(IServiceCollection services)
        => services.AddConnect(options =>
        {
            if (!options.Codecs.Any(c => c.Name == "proto"))
            {
                options.Codecs.Add(new ProtoConnectCodec(SmokeModel.Instance));
            }
        });

    // Method descriptors. Named from the contract's SIMPLE name, which is unambiguous here; a
    // generator sees every contract in the container, so it can qualify only on collision rather than
    // always - GrpcProxyGenerator emits `Ns_Sub_IGreeter_ClientProxy` unconditionally, which is safe
    // but reads badly in the common case.
    //
    // The serializers are resolved here, once, in the static initialiser - not per message.
    // Method descriptors, one per operation, shared by the proxy and the bindings so the service and
    // method names exist once. The serializers are resolved HERE, in the static initialiser - once per
    // method, not once per message.
    //
    // Written out in full rather than through a helper. An earlier draft had `Unary(name)` and
    // `Streaming(name)` factories, which read better but were not emittable: they only compiled
    // because every method in this fixture happened to share one request/response pair. A real
    // contract does not, so a generator must name each method's own types - as Farewell now shows.
    private static class Greeter
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
        public static readonly ConnectMethod<HelloRequest, HelloReply> Subscribe =
            new(ConnectMethodType.ServerStreaming, ServiceName, "Subscribe",
                requestSerializer: SmokeModel.Serializer<HelloRequest>(),
                responseSerializer: SmokeModel.Serializer<HelloReply>());
        public static readonly ConnectMethod<HelloRequest, HelloReply> SubscribeThenFail =
            new(ConnectMethodType.ServerStreaming, ServiceName, "SubscribeThenFail",
                requestSerializer: SmokeModel.Serializer<HelloRequest>(),
                responseSerializer: SmokeModel.Serializer<HelloReply>());
        public static readonly ConnectMethod<HelloRequest, HelloReply> Collect =
            new(ConnectMethodType.ClientStreaming, ServiceName, "Collect",
                requestSerializer: SmokeModel.Serializer<HelloRequest>(),
                responseSerializer: SmokeModel.Serializer<HelloReply>());
        public static readonly ConnectMethod<HelloRequest, HelloReply> Chat =
            new(ConnectMethodType.DuplexStreaming, ServiceName, "Chat",
                requestSerializer: SmokeModel.Serializer<HelloRequest>(),
                responseSerializer: SmokeModel.Serializer<HelloReply>());
    }

    private static class Farewell
    {
        public const string ServiceName = "aotconnectsmoke.v1.Farewell";

        public static readonly ConnectMethod<HelloRequest, HelloReply> Goodbye =
            new(ConnectMethodType.Unary, ServiceName, "Goodbye",
                requestSerializer: SmokeModel.Serializer<HelloRequest>(),
                responseSerializer: SmokeModel.Serializer<HelloReply>());
        public static readonly ConnectMethod<WaveRequest, WaveReply> Wave =
            new(ConnectMethodType.ServerStreaming, ServiceName, "Wave",
                requestSerializer: SmokeModel.Serializer<WaveRequest>(),
                responseSerializer: SmokeModel.Serializer<WaveReply>());
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

        public IAsyncEnumerable<WaveReply> WaveAsync(WaveRequest request, CallContext context = default)
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
/// The fluent surface for <see cref="SmokeServices"/>, emitted alongside it.
/// </summary>
/// <remarks>
/// Always emitted, whether or not the container is <c>static</c>, so that the fluent form does not
/// depend on how the consumer declared their half. Accessibility mirrors the container's.
/// <para>
/// Per-contract operations are named after the <b>contract</b>, never generic, and that is what makes
/// them safe as extensions: a generic <c>CreateClient&lt;TService&gt;</c> or <c>Bind&lt;TService&gt;</c>
/// is ambiguous between <em>any</em> two containers in scope, whereas <c>GreeterClient</c> and
/// <c>BindGreeter</c> can only collide with another container declaring the same contract. Both
/// measured, not assumed.
/// <para>
/// Operations covering <em>all</em> services carry the container's own name instead -
/// <c>AddSmokeServices</c>, <c>BindSmokeServices</c> - which is equally collision-proof and needs no
/// surgery on the consumer's identifier. Note "Services" there is the consumer's, not a suffix we add.
/// </para>
/// <para>
/// The generic forms stay on the container, where they cannot be ambiguous at all, and are the way
/// through when two containers do share a contract.
/// </para>
/// </para>
/// </remarks>
internal static class SmokeServicesExtensions
{
    /// <summary>Registers the codec and every service implementation this container declares.</summary>
    public static IServiceCollection AddSmokeServices(this IServiceCollection services)
        => SmokeServices.AddServices(services);

    /// <summary>Maps every service this container declares.</summary>
    public static IEndpointConventionBuilder BindSmokeServices(
        this IEndpointRouteBuilder endpoints, string? routingPrefix = null)
        => SmokeServices.BindServices(endpoints, routingPrefix);

    /// <summary>Registers <see cref="IGreeter"/>'s implementation, and the codec.</summary>
    public static IServiceCollection AddGreeter(this IServiceCollection services)
        => SmokeServices.AddService<IGreeter>(services);

    /// <summary>Registers <see cref="IFarewell"/>'s implementation, and the codec.</summary>
    public static IServiceCollection AddFarewell(this IServiceCollection services)
        => SmokeServices.AddService<IFarewell>(services);

    /// <summary>Maps <see cref="IGreeter"/> alone, so its conventions can differ from the others'.</summary>
    public static IEndpointConventionBuilder BindGreeter(
        this IEndpointRouteBuilder endpoints, string? routingPrefix = null)
        => SmokeServices.BindService<IGreeter>(endpoints, routingPrefix);

    /// <summary>Maps <see cref="IFarewell"/> alone.</summary>
    public static IEndpointConventionBuilder BindFarewell(
        this IEndpointRouteBuilder endpoints, string? routingPrefix = null)
        => SmokeServices.BindService<IFarewell>(endpoints, routingPrefix);

    /// <summary>Creates an <see cref="IGreeter"/> client over the channel.</summary>
    public static IGreeter GreeterClient(this ConnectChannel channel)
        => SmokeServices.CreateClient<IGreeter>(channel);

    /// <summary>Creates an <see cref="IFarewell"/> client over the channel.</summary>
    public static IFarewell FarewellClient(this ConnectChannel channel)
        => SmokeServices.CreateClient<IFarewell>(channel);
}
