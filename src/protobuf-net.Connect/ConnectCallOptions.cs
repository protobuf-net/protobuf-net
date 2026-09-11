using System;
using System.Collections.Generic;
using Grpc.Core;

namespace ProtoBuf.Connect
{
    /// <summary>
    /// Per-call options: leading metadata and a deadline.
    /// </summary>
    /// <remarks>
    /// Deliberately a small, transport-shaped type rather than an attempt at protobuf-net.Grpc's
    /// <c>CallContext</c>. The contract-facing vocabulary is a separate decision (see
    /// <c>notes/connect/findings.md</c>); this is what the transport itself needs.
    /// </remarks>
    public sealed class ConnectCallOptions
    {
        /// <summary>Leading metadata to send as request headers.</summary>
        public IReadOnlyList<KeyValuePair<string, string>>? Headers { get; init; }

        /// <summary>
        /// How long the server may take, sent as <c>connect-timeout-ms</c>. Omitted means no limit, which is
        /// what the protocol assumes when the header is absent.
        /// </summary>
        public TimeSpan? Timeout { get; init; }

        /// <summary>
        /// The HTTP version to send this call over, pinned exactly. Omitted leaves it to the
        /// <see cref="System.Net.Http.HttpClient"/>.
        /// </summary>
        /// <remarks>
        /// Worth having because a plaintext endpoint has no ALPN to negotiate with, so the version is a
        /// decision rather than an outcome - and because the one call shape that pins a version for you
        /// gets it wrong for half the cases.
        /// <para>
        /// <c>Duplex</c> defaults to HTTP/2, since a <em>full</em>-duplex call deadlocks without it: the
        /// caller waits for a response the server cannot send until the request body completes. But a
        /// <em>half</em>-duplex bidi call - every request sent, then every response read - is ordinary
        /// over HTTP/1.1, is explicitly allowed, and is exercised by the conformance suite. Only the
        /// caller knows which it is about to do, so only the caller can say. Setting this to
        /// <c>HttpVersion.Version11</c> is how.
        /// </para>
        /// </remarks>
        public Version? HttpVersion { get; init; }

        /// <summary>
        /// Compress the request with this. Omitted, or <see cref="ConnectCompression.Identity"/>, sends
        /// it uncompressed.
        /// </summary>
        /// <remarks>
        /// Only the <em>request</em> direction: what the response uses is the server's choice, from
        /// whatever we advertised as acceptable, and is read off the response rather than assumed.
        /// </remarks>
        public ConnectCompression? Compression { get; init; }

        /// <summary>
        /// Translates a caller's gRPC-shaped <see cref="CallOptions"/> into transport options.
        /// </summary>
        /// <remarks>
        /// Library code rather than generated code, because it depends on nothing about the service: it
        /// is a function of two types and nothing else. It takes <see cref="CallOptions"/> rather than
        /// protobuf-net.Grpc's <c>CallContext</c> deliberately - that keeps the dependency at
        /// <c>Grpc.Core.Api</c>, which has no protobuf-net dependency of its own, where
        /// <c>protobuf-net.Grpc</c> would drag in protobuf-net 2.4.8 and collide with
        /// <c>protobuf-net.Core</c> on <c>TypeModel</c>. Generated code passes
        /// <c>context.CallOptions</c>.
        /// <para>
        /// Returns <c>null</c> where there is nothing to say, so the common case allocates nothing.
        /// </para>
        /// </remarks>
        public static ConnectCallOptions? From(in CallOptions options)
        {
            var deadline = options.Deadline;
            var headers = options.Headers;
            if (deadline is null && (headers is null || headers.Count == 0)) return null;

            List<KeyValuePair<string, string>>? converted = null;
            if (headers is not null)
            {
                foreach (var entry in headers)
                {
                    // binary metadata travels as base64 under its -bin suffixed name, and Connect's
                    // base64 is unpadded - see Internal.ConnectBase64
                    (converted ??= new()).Add(new(
                        entry.Key, entry.IsBinary ? Internal.ConnectBase64.Encode(entry.ValueBytes) : entry.Value));
                }
            }

            return new ConnectCallOptions
            {
                // gRPC states an absolute deadline; Connect states a relative timeout
                Timeout = deadline is { } at && at != DateTime.MaxValue ? at - DateTime.UtcNow : null,
                Headers = converted,
            };
        }
    }

    /// <summary>
    /// The response side of a call: the status that was reported, plus leading and trailing metadata.
    /// </summary>
    /// <remarks>
    /// Trailing metadata is an abstraction rather than "the <c>trailer-</c> prefixed response headers",
    /// deliberately: that is where it lives for a unary call and it is emphatically not where it lives for
    /// a streaming one, which carries it in the terminating message. Anything reading trailers should read
    /// them from here.
    /// </remarks>
    public sealed class ConnectCallResult
    {
        internal ConnectCallResult(
            IReadOnlyList<KeyValuePair<string, string>> headers,
            IReadOnlyList<KeyValuePair<string, string>> trailers)
        {
            Headers = headers;
            Trailers = trailers;
        }

        /// <summary>Leading metadata.</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Headers { get; }

        /// <summary>Trailing metadata.</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Trailers { get; }
    }
}
