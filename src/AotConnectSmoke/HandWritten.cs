using ProtoBuf.Connect;
using ProtoBuf.Connect.AspNetCore;

namespace ProtoBuf.AotConnectSmoke;

// ---------------------------------------------------------------------------------------------
// THIS FILE IS THE GENERATOR'S TARGET OUTPUT, written by hand.
//
// Stage 1 of notes/connect/findings.md §14: get the shape working and reviewed *before* a Roslyn
// generator is taught to emit it. The repo already works this way - AotRefGen exists so that expected
// generator output is derived and reviewable rather than invented - and here there is no ref-emit to
// derive from, so writing it, making it work and reviewing it is the substitute.
//
// Two properties to preserve when this becomes generated:
//   - nothing here reflects, and nothing here needs to;
//   - the service and method names are the only strings, and they come from the contract.
// ---------------------------------------------------------------------------------------------

/// <summary>The methods of <see cref="IGreeter"/>, shared by the client proxy and the server bindings.</summary>
internal static class GreeterMethods
{
    public const string ServiceName = "aotconnectsmoke.v1.Greeter";

    public static readonly ConnectMethod<HelloRequest, HelloReply> SayHello =
        new(ConnectMethodType.Unary, ServiceName, "SayHello");

    public static readonly ConnectMethod<HelloRequest, HelloReply> Refuse =
        new(ConnectMethodType.Unary, ServiceName, "Refuse");

    public static readonly ConnectMethod<HelloRequest, HelloReply> Explode =
        new(ConnectMethodType.Unary, ServiceName, "Explode");

    public static readonly ConnectMethod<HelloRequest, HelloReply> Dawdle =
        new(ConnectMethodType.Unary, ServiceName, "Dawdle");
}

/// <summary>Server-side bindings: one typed delegate per method, no reflection.</summary>
internal sealed class GreeterBindings : IConnectServiceBinder<GreeterService>
{
    public void Bind(ConnectServiceBinderContext<GreeterService> context)
    {
        context.AddUnaryMethod(GreeterMethods.SayHello, static (service, request, ctx) => service.SayHelloAsync(request, ctx));
        context.AddUnaryMethod(GreeterMethods.Refuse, static (service, request, ctx) => service.RefuseAsync(request, ctx));
        context.AddUnaryMethod(GreeterMethods.Explode, static (service, request, ctx) => service.ExplodeAsync(request, ctx));
        context.AddUnaryMethod(GreeterMethods.Dawdle, static (service, request, ctx) => service.DawdleAsync(request, ctx));
    }
}

/// <summary>Client proxy: the same contract, over a <see cref="ConnectChannel"/>.</summary>
internal sealed class GreeterClient : IGreeter
{
    private readonly ConnectChannel _channel;

    public GreeterClient(ConnectChannel channel) => _channel = channel;

    // the context parameter is the server's on the server side and unused on the client's; what the
    // client half should take instead is part of the open vocabulary decision, so it is left inert
    // rather than invented here
    public Task<HelloReply> SayHelloAsync(HelloRequest request, ConnectServerCallContext? context = null)
        => _channel.UnaryAsync(GreeterMethods.SayHello, request);

    public Task<HelloReply> RefuseAsync(HelloRequest request, ConnectServerCallContext? context = null)
        => _channel.UnaryAsync(GreeterMethods.Refuse, request);

    public Task<HelloReply> ExplodeAsync(HelloRequest request, ConnectServerCallContext? context = null)
        => _channel.UnaryAsync(GreeterMethods.Explode, request);

    public Task<HelloReply> DawdleAsync(HelloRequest request, ConnectServerCallContext? context = null)
        => _channel.UnaryAsync(GreeterMethods.Dawdle, request);

    /// <summary>Calls a method with explicit options, which the inert context parameter cannot carry.</summary>
    public Task<(HelloReply Response, ConnectCallResult Call)> SayHelloWithMetadataAsync(
        HelloRequest request, ConnectCallOptions? options = null)
        => _channel.UnaryWithMetadataAsync(GreeterMethods.SayHello, request, options);

    public Task<HelloReply> DawdleAsync(HelloRequest request, ConnectCallOptions options)
        => _channel.UnaryAsync(GreeterMethods.Dawdle, request, options);
}
