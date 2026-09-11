using System;
using ProtoBuf.Serializers;

namespace ProtoBuf.Connect
{
    /// <summary>
    /// The shape of an RPC.
    /// </summary>
    /// <remarks>
    /// Only <see cref="Unary"/> is implemented today, but the distinction exists from the start because it
    /// selects the framing: unary sends the bare message, everything else sends enveloped messages under a
    /// different content-type.
    /// </remarks>
    public enum ConnectMethodType
    {
        /// <summary>One request, one response.</summary>
        Unary,
        /// <summary>A stream of requests, one response.</summary>
        ClientStreaming,
        /// <summary>One request, a stream of responses.</summary>
        ServerStreaming,
        /// <summary>A stream of requests and a stream of responses.</summary>
        DuplexStreaming,
    }

    /// <summary>
    /// Describes a single RPC: what it is called, what shape it is, and what goes in and out.
    /// </summary>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    public sealed class ConnectMethod<TRequest, TResponse>
    {
        /// <summary>Creates a new <see cref="ConnectMethod{TRequest, TResponse}"/>.</summary>
        /// <param name="type">The shape of the RPC.</param>
        /// <param name="serviceName">The fully-qualified service name, e.g. <c>connectrpc.eliza.v1.ElizaService</c>.</param>
        /// <param name="methodName">The method name, e.g. <c>Say</c>.</param>
        /// <param name="idempotent">
        /// Whether the RPC is free of side effects, i.e. <c>idempotency_level = NO_SIDE_EFFECTS</c>. Only
        /// such an RPC may be invoked with GET. Recorded but not yet acted on.
        /// </param>
        /// <param name="requestCodec">
        /// The model's serializer for <typeparamref name="TRequest"/>, resolved once here rather than per
        /// message. Optional: omitted, the codec resolves it each time. A generated model can hand these
        /// out because <c>SerializerCache.Get&lt;TProvider, T&gt;()</c> is public and the provider is a
        /// nested type of the model, so another part of the same partial class can name it.
        /// </param>
        /// <param name="responseCodec">As <paramref name="requestCodec"/>, for the response.</param>
        public ConnectMethod(ConnectMethodType type, string serviceName, string methodName, bool idempotent = false,
            IConnectMessageCodec<TRequest>? requestCodec = null, IConnectMessageCodec<TResponse>? responseCodec = null)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) throw new ArgumentException("A service name is required.", nameof(serviceName));
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentException("A method name is required.", nameof(methodName));

            Type = type;
            ServiceName = serviceName;
            MethodName = methodName;
            IsIdempotent = idempotent;
            RequestCodec = requestCodec;
            ResponseCodec = responseCodec;
            // the Connect path is the gRPC path: "/" package.Service "/" Method, case-sensitive.
            // The protocol also allows a routing PREFIX in front of it, which is what lets Connect sit
            // beside gRPC on one host - so the relative form is kept too, since that is what combines
            // with a base address that carries one.
            RelativePath = serviceName + "/" + methodName;
            Path = "/" + RelativePath;
        }

        /// <summary>The shape of the RPC.</summary>
        public ConnectMethodType Type { get; }

        /// <summary>The fully-qualified service name.</summary>
        public string ServiceName { get; }

        /// <summary>The method name.</summary>
        public string MethodName { get; }

        /// <summary>The canonical request path, with a leading slash and no routing prefix.</summary>
        public string Path { get; }

        /// <summary>
        /// The path without its leading slash, for combining with a base address that carries a routing
        /// prefix.
        /// </summary>
        /// <remarks>
        /// The distinction is not cosmetic: <c>new Uri(new Uri("http://host/connect/"), "/pkg.Svc/M")</c>
        /// yields <c>http://host/pkg.Svc/M</c>, because a leading slash makes the relative reference
        /// absolute-path and discards the base's own path. Using this form instead yields
        /// <c>http://host/connect/pkg.Svc/M</c>, which is what the caller asked for.
        /// </remarks>
        public string RelativePath { get; }

        /// <summary>Whether the RPC is declared free of side effects.</summary>
        public bool IsIdempotent { get; }

        /// <summary>The pre-resolved request serializer, if one was supplied.</summary>
        /// <remarks>
        /// Binary-specific by nature, so it is a fast path rather than the mechanism: a codec that cannot
        /// use it - a JSON one - ignores it and resolves its own. That is the same
        /// <c>serializer ??= ...</c> idiom protobuf-net uses throughout.
        /// </remarks>
        public IConnectMessageCodec<TRequest>? RequestCodec { get; }

        /// <summary>The pre-resolved response serializer, if one was supplied.</summary>
        public IConnectMessageCodec<TResponse>? ResponseCodec { get; }

        /// <inheritdoc/>
        public override string ToString() => Path;
    }
}
