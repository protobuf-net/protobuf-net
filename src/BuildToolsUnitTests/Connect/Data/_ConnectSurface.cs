#nullable enable
// A SNAPSHOT of the protobuf-net.Connect / protobuf-net.Grpc surface the generated code binds to,
// compiled alongside each fixture so that a signature which does not line up fails *here* rather than
// in a consumer's build.
//
// It is a snapshot and can drift, exactly as Grpc/Data/_ContractSurface.cs can. src/AotConnectSmoke is
// what catches drift, because it compiles the generated code against the real assemblies.
//
// Only what the generated code names is here. Bodies throw: nothing is executed.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProtoBuf.Connect
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ProtoConnectAttribute : Attribute
    {
        public Type? Model { get; set; }
    }

    public enum ConnectMethodType { Unary, ClientStreaming, ServerStreaming, DuplexStreaming }

    public sealed class ConnectMethod<TRequest, TResponse>
    {
        public ConnectMethod(ConnectMethodType type, string serviceName, string methodName,
            bool idempotent = false, object? requestSerializer = null, object? responseSerializer = null) { }
    }

    public sealed class ConnectCallOptions
    {
        public static ConnectCallOptions? From(in global::Grpc.Core.CallOptions options) => null;
    }

    public abstract class ConnectCodec
    {
        public abstract string Name { get; }
    }

    public sealed class ProtoConnectCodec : ConnectCodec
    {
        public ProtoConnectCodec(global::ProtoBuf.Meta.TypeModel model) { }
        public override string Name => "proto";
    }

    public sealed class ConnectServerStream<TResponse> : IAsyncEnumerable<TResponse>
    {
        public IAsyncEnumerator<TResponse> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    public sealed class ConnectChannel
    {
        public Task<TResponse> UnaryAsync<TRequest, TResponse>(ConnectMethod<TRequest, TResponse> method,
            TRequest request, ConnectCallOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> ServerStreaming<TRequest, TResponse>(ConnectMethod<TRequest, TResponse> method,
            TRequest request, ConnectCallOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TResponse> ClientStreamingAsync<TRequest, TResponse>(ConnectMethod<TRequest, TResponse> method,
            IAsyncEnumerable<TRequest> requests, ConnectCallOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> Duplex<TRequest, TResponse>(ConnectMethod<TRequest, TResponse> method,
            IAsyncEnumerable<TRequest> requests, ConnectCallOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}

namespace ProtoBuf.Connect.AspNetCore
{
    public sealed class ConnectServerCallContext : global::Grpc.Core.ServerCallContext { }

    public sealed class ConnectServerOptions
    {
        public IList<global::ProtoBuf.Connect.ConnectCodec> Codecs { get; } = null!;
    }

    public interface IConnectServiceBinder<TImplementation> where TImplementation : class
    {
        void Bind(ConnectServiceBinderContext<TImplementation> context);
    }

    public delegate Task<TResponse> ConnectUnaryHandler<in TImplementation, in TRequest, TResponse>(
        TImplementation service, TRequest request, ConnectServerCallContext context);

    public delegate IAsyncEnumerable<TResponse> ConnectServerStreamingHandler<in TImplementation, in TRequest, out TResponse>(
        TImplementation service, TRequest request, ConnectServerCallContext context);

    public delegate Task<TResponse> ConnectClientStreamingHandler<in TImplementation, TRequest, TResponse>(
        TImplementation service, IAsyncEnumerable<TRequest> requests, ConnectServerCallContext context);

    public delegate IAsyncEnumerable<TResponse> ConnectDuplexHandler<in TImplementation, TRequest, out TResponse>(
        TImplementation service, IAsyncEnumerable<TRequest> requests, ConnectServerCallContext context);

    public sealed class ConnectServiceBinderContext<TImplementation> where TImplementation : class
    {
        public void AddUnaryMethod<TRequest, TResponse>(global::ProtoBuf.Connect.ConnectMethod<TRequest, TResponse> method,
            ConnectUnaryHandler<TImplementation, TRequest, TResponse> handler, IReadOnlyList<object>? metadata = null) { }

        public void AddServerStreamingMethod<TRequest, TResponse>(global::ProtoBuf.Connect.ConnectMethod<TRequest, TResponse> method,
            ConnectServerStreamingHandler<TImplementation, TRequest, TResponse> handler, IReadOnlyList<object>? metadata = null) { }

        public void AddClientStreamingMethod<TRequest, TResponse>(global::ProtoBuf.Connect.ConnectMethod<TRequest, TResponse> method,
            ConnectClientStreamingHandler<TImplementation, TRequest, TResponse> handler, IReadOnlyList<object>? metadata = null) { }

        public void AddDuplexMethod<TRequest, TResponse>(global::ProtoBuf.Connect.ConnectMethod<TRequest, TResponse> method,
            ConnectDuplexHandler<TImplementation, TRequest, TResponse> handler, IReadOnlyList<object>? metadata = null) { }
    }

    public static class ConnectEndpointRouteBuilderExtensions
    {
        public static global::Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapConnectService<TImplementation>(
            this global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints,
            IConnectServiceBinder<TImplementation> binder, string? routingPrefix = null)
            where TImplementation : class => throw new NotSupportedException();
    }

    public static class ConnectServiceCollectionExtensions
    {
        public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddConnect(
            this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services,
            Action<ConnectServerOptions>? configure = null) => services;
    }
}

// The ASP.NET Core surface the generated binding names. Not in _ContractSurface.cs, which only needed
// Grpc.AspNetCore.Server's model types.
namespace Microsoft.AspNetCore.Builder
{
    public class EndpointBuilder { }

    public interface IEndpointConventionBuilder
    {
        void Add(Action<EndpointBuilder> convention);
        void Finally(Action<EndpointBuilder> finallyConvention);
    }
}

namespace Microsoft.AspNetCore.Routing
{
    public interface IEndpointRouteBuilder { }
}
