using System;
using System.Collections.Generic;

namespace ProtoBuf.Connect
{
    /// <summary>
    /// One entry of an error's <c>details</c> array.
    /// </summary>
    /// <remarks>
    /// This is a <c>google.protobuf.Any</c> with the type-URL prefix trimmed: <see cref="TypeName"/> is the
    /// bare fully-qualified protobuf message name (<c>package.Message</c>), which is what appears on the
    /// wire, and <see cref="Value"/> is the serialized message. The trim is lossy - a non-default type-URL
    /// prefix cannot be recovered - and reconstructing an <c>Any</c> means prepending
    /// <c>type.googleapis.com/</c>.
    /// </remarks>
    /// <param name="TypeName">The fully-qualified protobuf message name.</param>
    /// <param name="Value">The serialized message.</param>
    public readonly record struct ConnectErrorDetail(string TypeName, byte[] Value);

    /// <summary>
    /// Thrown when a Connect RPC fails.
    /// </summary>
    /// <remarks>
    /// Every failure lands here whatever shape it arrived in: a unary error (non-200 plus a JSON body), a
    /// streaming error (200, with the error inside the terminating message), and a response that carries no
    /// Connect error object at all - an unrouted path answered by the HTTP layer, say - which is inferred
    /// from the status via <see cref="ConnectCodes.FromHttpStatus"/>.
    /// </remarks>
    public sealed class ConnectException : Exception
    {
        /// <summary>The protocol error code.</summary>
        public ConnectCode Code { get; }

        /// <summary>The HTTP status that carried the error, where there was one.</summary>
        public int? HttpStatus { get; }

        /// <summary>Any error details supplied by the server.</summary>
        public IReadOnlyList<ConnectErrorDetail> Details { get; }

        /// <summary>
        /// The message exactly as it appeared on the wire, without the code and status that
        /// <see cref="Exception.Message"/> prefixes for readability. This is what goes back out when an
        /// error is re-serialized, so that relaying one does not accumulate prefixes.
        /// </summary>
        public string? RawMessage { get; }

        /// <summary>
        /// <c>true</c> when the server did not supply a Connect error object and the code was inferred from
        /// the HTTP status - so the code is a guess about an intermediary, not a statement by the service.
        /// </summary>
        public bool CodeWasInferred { get; }

        /// <summary>Creates a new <see cref="ConnectException"/>.</summary>
        public ConnectException(
            ConnectCode code,
            string? message = null,
            int? httpStatus = null,
            IReadOnlyList<ConnectErrorDetail>? details = null,
            bool codeWasInferred = false,
            Exception? innerException = null)
            : base(Describe(code, message, httpStatus), innerException)
        {
            Code = code;
            RawMessage = message;
            HttpStatus = httpStatus;
            Details = details ?? Array.Empty<ConnectErrorDetail>();
            CodeWasInferred = codeWasInferred;
        }

        private static string Describe(ConnectCode code, string? message, int? httpStatus)
        {
            // the protocol explicitly allows an omitted or empty message, saying the client should
            // synthesize one; this is that synthesis
            var prefix = httpStatus is null
                ? code.ToWireName()
                : code.ToWireName() + " (HTTP " + httpStatus.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
            return string.IsNullOrEmpty(message) ? prefix : prefix + ": " + message;
        }
    }
}
