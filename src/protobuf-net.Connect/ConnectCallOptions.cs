using System;
using System.Collections.Generic;

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
