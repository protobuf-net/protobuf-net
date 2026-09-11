using ProtoBuf.Connect;
using ProtoBuf.Grpc;

namespace ProtoBuf.AotConnectSmoke;

// THE GENERATED HALF of the client-only container; the consumer's is ClientOnly.cs.
//
// The whole surface is ONE verb. No BindServer, because no implementation was named and
// IConnectServiceBinder<TService> is generic in the implementation - without one there is nothing to
// close the generic with, and binding would have to go through MakeGenericMethod, which is precisely
// what this generator exists to avoid. No AddXxx either: a client needs no DI registration at all,
// since the codec lives on the ConnectChannel rather than in the container.
//
// So "client only" is not a trimmed-down version of the full shape - it is a genuinely smaller one.

partial class SmokeClientOnly
{
    private SmokeClientOnly() { }

    /// <summary>Creates a client proxy for one of the contracts this container declares.</summary>
    public static TService CreateClient<TService>(ConnectChannel channel) where TService : class
    {
        if (typeof(TService) == typeof(IFarewell)) return (TService)(object)new FarewellClientProxy(channel);
        throw new InvalidOperationException(
            "No build-time Connect proxy for " + typeof(TService).FullName + " in " + nameof(SmokeClientOnly) + ".");
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
}
