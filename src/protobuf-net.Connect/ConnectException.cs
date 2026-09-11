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

        /// <summary>
        /// Translates a <c>Grpc.Core</c> <see cref="Grpc.Core.RpcException"/> - the way a contract-first
        /// service reports a failure - into its Connect equivalent.
        /// </summary>
        /// <remarks>
        /// <see cref="ConnectCode"/> takes its ordinals from gRPC's <c>StatusCode</c>, so this could be a
        /// cast. It is written out instead: the two enums are maintained by different people in different
        /// repositories, a cast would silently mis-map the day either adds a value, and this codebase has
        /// already shipped exactly that bug once (<c>DataFormat</c> to <c>ProtoDataFormat</c>, where the
        /// ordinals looked aligned and were not). An unrecognised code becomes
        /// <see cref="ConnectCode.Unknown"/>, which is what the protocol asks of a reader.
        /// </remarks>
        public static ConnectException FromRpcException(Grpc.Core.RpcException exception)
        {
            if (exception is null) throw new ArgumentNullException(nameof(exception));

            var code = exception.StatusCode switch
            {
                Grpc.Core.StatusCode.OK => ConnectCode.Ok,
                Grpc.Core.StatusCode.Cancelled => ConnectCode.Cancelled,
                Grpc.Core.StatusCode.Unknown => ConnectCode.Unknown,
                Grpc.Core.StatusCode.InvalidArgument => ConnectCode.InvalidArgument,
                Grpc.Core.StatusCode.DeadlineExceeded => ConnectCode.DeadlineExceeded,
                Grpc.Core.StatusCode.NotFound => ConnectCode.NotFound,
                Grpc.Core.StatusCode.AlreadyExists => ConnectCode.AlreadyExists,
                Grpc.Core.StatusCode.PermissionDenied => ConnectCode.PermissionDenied,
                Grpc.Core.StatusCode.ResourceExhausted => ConnectCode.ResourceExhausted,
                Grpc.Core.StatusCode.FailedPrecondition => ConnectCode.FailedPrecondition,
                Grpc.Core.StatusCode.Aborted => ConnectCode.Aborted,
                Grpc.Core.StatusCode.OutOfRange => ConnectCode.OutOfRange,
                Grpc.Core.StatusCode.Unimplemented => ConnectCode.Unimplemented,
                Grpc.Core.StatusCode.Internal => ConnectCode.Internal,
                Grpc.Core.StatusCode.Unavailable => ConnectCode.Unavailable,
                Grpc.Core.StatusCode.DataLoss => ConnectCode.DataLoss,
                Grpc.Core.StatusCode.Unauthenticated => ConnectCode.Unauthenticated,
                _ => ConnectCode.Unknown,
            };

            // Status.Detail is gRPC's message; an empty one is normal and stays empty, since the protocol
            // lets a reader synthesize its own
            var detail = exception.Status.Detail;
            return new ConnectException(
                code, string.IsNullOrEmpty(detail) ? null : detail, innerException: exception);
        }

        /// <summary>
        /// The inverse of <see cref="FromRpcException"/>: the gRPC status code for a Connect code.
        /// </summary>
        /// <remarks>
        /// Written out for the reason given there. The two directions are separate maps rather than one
        /// table because they are not quite inverses at the edges - every gRPC code has a Connect code,
        /// but an unrecognised Connect code has no better answer than <c>Unknown</c>.
        /// </remarks>
        public static Grpc.Core.StatusCode ToStatusCode(ConnectCode code) => code switch
        {
            ConnectCode.Ok => Grpc.Core.StatusCode.OK,
            ConnectCode.Cancelled => Grpc.Core.StatusCode.Cancelled,
            ConnectCode.Unknown => Grpc.Core.StatusCode.Unknown,
            ConnectCode.InvalidArgument => Grpc.Core.StatusCode.InvalidArgument,
            ConnectCode.DeadlineExceeded => Grpc.Core.StatusCode.DeadlineExceeded,
            ConnectCode.NotFound => Grpc.Core.StatusCode.NotFound,
            ConnectCode.AlreadyExists => Grpc.Core.StatusCode.AlreadyExists,
            ConnectCode.PermissionDenied => Grpc.Core.StatusCode.PermissionDenied,
            ConnectCode.ResourceExhausted => Grpc.Core.StatusCode.ResourceExhausted,
            ConnectCode.FailedPrecondition => Grpc.Core.StatusCode.FailedPrecondition,
            ConnectCode.Aborted => Grpc.Core.StatusCode.Aborted,
            ConnectCode.OutOfRange => Grpc.Core.StatusCode.OutOfRange,
            ConnectCode.Unimplemented => Grpc.Core.StatusCode.Unimplemented,
            ConnectCode.Internal => Grpc.Core.StatusCode.Internal,
            ConnectCode.Unavailable => Grpc.Core.StatusCode.Unavailable,
            ConnectCode.DataLoss => Grpc.Core.StatusCode.DataLoss,
            ConnectCode.Unauthenticated => Grpc.Core.StatusCode.Unauthenticated,
            _ => Grpc.Core.StatusCode.Unknown,
        };

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
