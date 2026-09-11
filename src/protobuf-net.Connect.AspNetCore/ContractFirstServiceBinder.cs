using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using ProtoBuf.Connect.Internal;

namespace ProtoBuf.Connect.AspNetCore
{
    /// <summary>
    /// Serves an existing <c>protoc</c>-generated service base over Connect, with no generator, no
    /// annotation and no change to the consumer's contracts.
    /// </summary>
    /// <remarks>
    /// This is the contract-first path, and the reason it needs nothing from the consumer is that
    /// <c>protoc</c> already emits everything required: <c>Greeter.BindService(ServiceBinderBase,
    /// GreeterBase)</c> enumerates the service's methods, and each <see cref="Method{TRequest, TResponse}"/>
    /// carries its own name, shape and marshallers. <c>ServiceBinderBase</c> is documented as the hook for
    /// "alternative serving stacks"; Connect is one.
    /// <para>
    /// Nothing is re-encoded. For <c>application/proto</c> a Connect body and a gRPC body are the same
    /// bytes - only the framing around them differs - so the marshallers <c>protoc</c> generated are used
    /// unchanged, through <see cref="MarshallerMessageCodec{T}"/>.
    /// </para>
    /// </remarks>
    public static class ContractFirstConnectExtensions
    {
        /// <summary>
        /// Maps a <c>protoc</c>-generated service over Connect.
        /// </summary>
        /// <example>
        /// <code>
        /// app.MapConnectService&lt;GreeterImpl&gt;(Greeter.BindService);
        /// </code>
        /// </example>
        /// <typeparam name="TImplementation">
        /// The consumer's service implementation, resolved from the request's services on each call, as it
        /// would be under <c>Grpc.AspNetCore.Server</c>. It derives from the generated base, which is what
        /// lets the generated <c>BindService</c> be passed here directly - a method group converts to
        /// <c>Action&lt;ServiceBinderBase, TImplementation&gt;</c> even though it declares the base type,
        /// since delegate parameters are contravariant.
        /// </typeparam>
        /// <param name="endpoints">The route builder.</param>
        /// <param name="bindService">
        /// The generated <c>BindService(ServiceBinderBase, TBase)</c> method, passed as a method group.
        /// </param>
        /// <param name="routingPrefix">An optional prefix in front of every method path.</param>
        /// <param name="metadata">
        /// Supplies endpoint metadata per method - <c>[Authorize]</c>, a CORS policy, a rate-limiter policy.
        /// <strong>Nothing is inferred</strong>: <c>Grpc.AspNetCore.Server</c> collects those by reflecting
        /// over the implementation's methods, and this path deliberately does not reflect, so an
        /// <c>[Authorize]</c> attribute on a contract-first service method is <em>not</em> honoured unless
        /// it is supplied here. That is a silently more permissive endpoint if it is overlooked, which is
        /// why it is a parameter rather than a default.
        /// </param>
        public static IEndpointConventionBuilder MapConnectService<TImplementation>(
            this IEndpointRouteBuilder endpoints,
            Action<ServiceBinderBase, TImplementation> bindService,
            string? routingPrefix = null,
            Func<IMethod, IReadOnlyList<object>>? metadata = null)
            where TImplementation : class
        {
            ArgumentNullException.ThrowIfNull(endpoints);
            ArgumentNullException.ThrowIfNull(bindService);

            return endpoints.MapConnectService(
                new ContractFirstServiceBinder<TImplementation>(bindService, metadata), routingPrefix);
        }
    }

    /// <summary>
    /// Adapts a generated <c>BindService</c> to <see cref="IConnectServiceBinder{TImplementation}"/>.
    /// </summary>
    internal sealed class ContractFirstServiceBinder<TImplementation> : IConnectServiceBinder<TImplementation>
        where TImplementation : class
    {
        private readonly Action<ServiceBinderBase, TImplementation> _bindService;
        private readonly Func<IMethod, IReadOnlyList<object>>? _metadata;

        // one-entry cache of the handler table, keyed on the instance it closes over. A service registered
        // as a singleton - the common shape for a stateless gRPC service - therefore builds its handlers
        // once for the life of the process; a scoped one rebuilds them per call, which is a handful of
        // delegate allocations and no reflection.
        private HandlerTable? _cached;

        public ContractFirstServiceBinder(
            Action<ServiceBinderBase, TImplementation> bindService, Func<IMethod, IReadOnlyList<object>>? metadata)
        {
            _bindService = bindService;
            _metadata = metadata;
        }

        /// <remarks>
        /// The descriptor pass. <c>protoc</c> emits
        /// <c>serviceBinder.AddMethod(__Method_X, serviceImpl == null ? null : new UnaryServerMethod&lt;,&gt;(...))</c>,
        /// so binding with a <c>null</c> instance yields every method's descriptor and no handlers at all.
        /// That null-tolerance is not incidental - it is there so <c>Grpc.AspNetCore.Server</c> can do
        /// exactly this - and it is what lets the endpoints be built at startup without constructing the
        /// consumer's service, while the handlers are still bound per call against a real instance.
        /// </remarks>
        public void Bind(ConnectServiceBinderContext<TImplementation> context)
            => _bindService(new DescriptorBinder(this, context), null!);

        private HandlerTable Handlers(TImplementation service)
        {
            var cached = Volatile.Read(ref _cached);
            if (cached is not null && ReferenceEquals(cached.Service, service)) return cached;

            var collector = new HandlerCollector();
            _bindService(collector, service);

            cached = new HandlerTable(service, collector.Handlers);
            Volatile.Write(ref _cached, cached);
            return cached;
        }

        private THandler Handler<THandler>(TImplementation service, IMethod method) where THandler : Delegate
        {
            if (Handlers(service).Lookup.TryGetValue(method.FullName, out var handler) && handler is THandler typed)
            {
                return typed;
            }

            // the two binds disagreed, which can only happen if BindService is not deterministic
            throw new InvalidOperationException(
                $"'{method.FullName}' was declared when binding the service's methods, but no {typeof(THandler).Name} was produced for it.");
        }

        private sealed record HandlerTable(TImplementation Service, Dictionary<string, Delegate> Lookup);

        /// <summary>Captures every method's descriptor, and registers a Connect endpoint for each.</summary>
        private sealed class DescriptorBinder : ServiceBinderBase
        {
            private readonly ContractFirstServiceBinder<TImplementation> _owner;
            private readonly ConnectServiceBinderContext<TImplementation> _context;

            public DescriptorBinder(
                ContractFirstServiceBinder<TImplementation> owner, ConnectServiceBinderContext<TImplementation> context)
            {
                _owner = owner;
                _context = context;
            }

            public override void AddMethod<TRequest, TResponse>(
                Method<TRequest, TResponse> method, UnaryServerMethod<TRequest, TResponse>? handler)
                => _context.AddUnaryMethod(ConnectMethod.FromGrpc(method), (service, request, context)
                    => _owner.Handler<UnaryServerMethod<TRequest, TResponse>>(service, method)(request, context),
                    Metadata(method));

            public override void AddMethod<TRequest, TResponse>(
                Method<TRequest, TResponse> method, ServerStreamingServerMethod<TRequest, TResponse>? handler)
                => _context.AddServerStreamingMethod(ConnectMethod.FromGrpc(method), (service, request, context)
                    => GrpcStreamAdapters.ToAsyncEnumerable<TResponse>(
                        writer => _owner.Handler<ServerStreamingServerMethod<TRequest, TResponse>>(service, method)(request, writer, context),
                        context.CancellationToken),
                    Metadata(method));

            public override void AddMethod<TRequest, TResponse>(
                Method<TRequest, TResponse> method, ClientStreamingServerMethod<TRequest, TResponse>? handler)
                => _context.AddClientStreamingMethod(ConnectMethod.FromGrpc(method), (service, requests, context)
                    => _owner.Handler<ClientStreamingServerMethod<TRequest, TResponse>>(service, method)(
                        GrpcStreamAdapters.ToStreamReader(requests, context.CancellationToken), context),
                    Metadata(method));

            public override void AddMethod<TRequest, TResponse>(
                Method<TRequest, TResponse> method, DuplexStreamingServerMethod<TRequest, TResponse>? handler)
                => _context.AddDuplexMethod(ConnectMethod.FromGrpc(method), (service, requests, context)
                    => GrpcStreamAdapters.ToAsyncEnumerable<TResponse>(
                        writer => _owner.Handler<DuplexStreamingServerMethod<TRequest, TResponse>>(service, method)(
                            GrpcStreamAdapters.ToStreamReader(requests, context.CancellationToken), writer, context),
                        context.CancellationToken),
                    Metadata(method));

            private IReadOnlyList<object>? Metadata(IMethod method) => _owner._metadata?.Invoke(method);
        }

        /// <summary>Captures the handlers a bind against a real instance produces.</summary>
        private sealed class HandlerCollector : ServiceBinderBase
        {
            public Dictionary<string, Delegate> Handlers { get; } = new(StringComparer.Ordinal);

            public override void AddMethod<TRequest, TResponse>(
                Method<TRequest, TResponse> method, UnaryServerMethod<TRequest, TResponse>? handler)
                => Add(method, handler);

            public override void AddMethod<TRequest, TResponse>(
                Method<TRequest, TResponse> method, ServerStreamingServerMethod<TRequest, TResponse>? handler)
                => Add(method, handler);

            public override void AddMethod<TRequest, TResponse>(
                Method<TRequest, TResponse> method, ClientStreamingServerMethod<TRequest, TResponse>? handler)
                => Add(method, handler);

            public override void AddMethod<TRequest, TResponse>(
                Method<TRequest, TResponse> method, DuplexStreamingServerMethod<TRequest, TResponse>? handler)
                => Add(method, handler);

            private void Add(IMethod method, Delegate? handler)
            {
                if (handler is not null) Handlers[method.FullName] = handler;
            }
        }
    }
}
