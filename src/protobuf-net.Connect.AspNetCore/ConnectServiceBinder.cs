using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProtoBuf.Connect.AspNetCore
{
    /// <summary>
    /// Handles one unary call.
    /// </summary>
    /// <remarks>
    /// A delegate per shape, with the runtime owning the reading and writing, is the shape that lets the
    /// other method shapes be added by adding delegate types rather than by growing a second pipeline.
    /// The alternative - a generated handler that took a request and returned a response - is
    /// unreachable from any streaming shape.
    /// <para>
    /// The context is a <see cref="ConnectServerCallContext"/>, i.e. a <c>Grpc.Core.ServerCallContext</c>,
    /// rather than protobuf-net.Grpc's <c>CallContext</c>. Constructing that is the generated code's job -
    /// one line, exactly as in <c>GrpcProxyGenerator</c>'s output - which keeps protobuf-net.Grpc out of
    /// this assembly.
    /// </para>
    /// </remarks>
    public delegate Task<TResponse> ConnectUnaryHandler<in TService, in TRequest, TResponse>(
        TService service, TRequest request, ConnectServerCallContext context);

    /// <summary>
    /// Handles one server-streaming call: one request in, a sequence of responses out.
    /// </summary>
    /// <remarks>
    /// A second delegate type rather than a second pipeline, which is exactly what §14.1 of
    /// <c>notes/connect/findings.md</c> required the shape to allow. It did.
    /// </remarks>
    public delegate IAsyncEnumerable<TResponse> ConnectServerStreamingHandler<in TService, in TRequest, out TResponse>(
        TService service, TRequest request, ConnectServerCallContext context);

    /// <summary>
    /// Handles one client-streaming call: a sequence of requests in, one response out.
    /// </summary>
    public delegate Task<TResponse> ConnectClientStreamingHandler<in TService, TRequest, TResponse>(
        TService service, IAsyncEnumerable<TRequest> requests, ConnectServerCallContext context);

    /// <summary>
    /// Implemented by generated code to describe a service's methods to the runtime.
    /// </summary>
    /// <typeparam name="TService">The service implementation type, resolved per call from DI.</typeparam>
    public interface IConnectServiceBinder<TService> where TService : class
    {
        /// <summary>Called once at startup to enumerate the service's methods.</summary>
        void Bind(ConnectServiceBinderContext<TService> context);
    }

    /// <summary>
    /// Collects the methods a service declares. One instance per <c>MapConnectService</c> call.
    /// </summary>
    /// <typeparam name="TService">The service implementation type.</typeparam>
    public sealed class ConnectServiceBinderContext<TService> where TService : class
    {
        private readonly List<ConnectMethodRegistration<TService>> _methods = new();

        internal ConnectServiceBinderContext() { }

        internal IReadOnlyList<ConnectMethodRegistration<TService>> Methods => _methods;

        /// <summary>Declares a unary method.</summary>
        /// <param name="method">The method's name and shape.</param>
        /// <param name="handler">Invokes the service.</param>
        /// <param name="metadata">
        /// Endpoint metadata - <c>[Authorize]</c>, a CORS policy, a rate-limiter policy, and so on. This
        /// is why each method becomes its own endpoint: ASP.NET Core resolves all of those from the
        /// matched endpoint, in middleware that runs before any handler, so a service-wide endpoint
        /// could not carry per-method values at all.
        /// </param>
        public void AddUnaryMethod<TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            ConnectUnaryHandler<TService, TRequest, TResponse> handler,
            IReadOnlyList<object>? metadata = null)
            => Add(method, ConnectMethodType.Unary, metadata,
                Internal.ConnectUnaryInvoker.Create(method, Require(handler)));

        /// <summary>Declares a server-streaming method.</summary>
        /// <inheritdoc cref="AddUnaryMethod{TRequest, TResponse}" path="/param"/>
        public void AddServerStreamingMethod<TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            ConnectServerStreamingHandler<TService, TRequest, TResponse> handler,
            IReadOnlyList<object>? metadata = null)
            => Add(method, ConnectMethodType.ServerStreaming, metadata,
                Internal.ConnectServerStreamingInvoker.Create(method, Require(handler)));

        /// <summary>Declares a client-streaming method.</summary>
        /// <inheritdoc cref="AddUnaryMethod{TRequest, TResponse}" path="/param"/>
        public void AddClientStreamingMethod<TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            ConnectClientStreamingHandler<TService, TRequest, TResponse> handler,
            IReadOnlyList<object>? metadata = null)
            => Add(method, ConnectMethodType.ClientStreaming, metadata,
                Internal.ConnectClientStreamingInvoker.Create(method, Require(handler)));

        private void Add<TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            ConnectMethodType expected,
            IReadOnlyList<object>? metadata,
            Internal.ConnectInvoker<TService> invoker)
        {
            ArgumentNullException.ThrowIfNull(method);
            if (method.Type != expected)
            {
                throw new ArgumentException($"'{method}' is {method.Type}, not {expected}.", nameof(method));
            }

            _methods.Add(new ConnectMethodRegistration<TService>(
                method.Path, method.ToString(), expected, metadata ?? Array.Empty<object>(), invoker));
        }

        private static T Require<T>(T handler) where T : Delegate
            => handler ?? throw new ArgumentNullException(nameof(handler));
    }

    internal sealed record ConnectMethodRegistration<TService>(
        string Path,
        string DisplayName,
        ConnectMethodType Type,
        IReadOnlyList<object> Metadata,
        Internal.ConnectInvoker<TService> Invoker) where TService : class;
}
