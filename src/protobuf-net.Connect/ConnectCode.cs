using System;

namespace ProtoBuf.Connect
{
    /// <summary>
    /// The error codes defined by the Connect protocol.
    /// </summary>
    /// <remarks>
    /// The numeric values are gRPC's <c>StatusCode</c> values, deliberately: the two sets are the same
    /// sixteen codes, and a Connect implementation is expected to interoperate with gRPC, so keeping the
    /// ordinals aligned makes the conversion a cast rather than a table.
    /// </remarks>
    public enum ConnectCode
    {
        /// <summary>Not an error; the RPC completed successfully.</summary>
        Ok = 0,
        /// <summary>The operation was cancelled by the caller.</summary>
        Cancelled = 1,
        /// <summary>An error whose origin is unclear.</summary>
        Unknown = 2,
        /// <summary>The request is invalid, regardless of the state of the system.</summary>
        InvalidArgument = 3,
        /// <summary>The deadline expired before the operation completed.</summary>
        DeadlineExceeded = 4,
        /// <summary>The requested resource was not found.</summary>
        NotFound = 5,
        /// <summary>The resource already exists.</summary>
        AlreadyExists = 6,
        /// <summary>The caller is not authorized to perform the operation.</summary>
        PermissionDenied = 7,
        /// <summary>A quota or per-resource limit was exhausted.</summary>
        ResourceExhausted = 8,
        /// <summary>The system is not in the state the operation requires.</summary>
        FailedPrecondition = 9,
        /// <summary>The operation was aborted, typically for concurrency reasons.</summary>
        Aborted = 10,
        /// <summary>The operation was attempted past the valid range.</summary>
        OutOfRange = 11,
        /// <summary>The operation is not implemented or not supported.</summary>
        Unimplemented = 12,
        /// <summary>An internal invariant was broken.</summary>
        Internal = 13,
        /// <summary>The service is transiently unavailable; retrying may succeed.</summary>
        Unavailable = 14,
        /// <summary>Unrecoverable data loss or corruption.</summary>
        DataLoss = 15,
        /// <summary>The caller lacks valid authentication credentials.</summary>
        Unauthenticated = 16,
    }

    /// <summary>
    /// Conversions between <see cref="ConnectCode"/>, the wire names, and HTTP status codes.
    /// </summary>
    public static class ConnectCodes
    {
        /// <summary>
        /// The name this code carries on the wire, as it appears in the JSON <c>code</c> field.
        /// </summary>
        public static string ToWireName(this ConnectCode code) => code switch
        {
            ConnectCode.Cancelled => "canceled", // note the spelling: the protocol uses the one-l form
            ConnectCode.Unknown => "unknown",
            ConnectCode.InvalidArgument => "invalid_argument",
            ConnectCode.DeadlineExceeded => "deadline_exceeded",
            ConnectCode.NotFound => "not_found",
            ConnectCode.AlreadyExists => "already_exists",
            ConnectCode.PermissionDenied => "permission_denied",
            ConnectCode.ResourceExhausted => "resource_exhausted",
            ConnectCode.FailedPrecondition => "failed_precondition",
            ConnectCode.Aborted => "aborted",
            ConnectCode.OutOfRange => "out_of_range",
            ConnectCode.Unimplemented => "unimplemented",
            ConnectCode.Internal => "internal",
            ConnectCode.Unavailable => "unavailable",
            ConnectCode.DataLoss => "data_loss",
            ConnectCode.Unauthenticated => "unauthenticated",
            _ => "unknown",
        };

        /// <summary>
        /// Parses a wire name; an unrecognised name is <see cref="ConnectCode.Unknown"/>, since a future
        /// revision of the protocol may add codes and a client should not fail to report the error at all.
        /// </summary>
        public static ConnectCode FromWireName(string? name) => name switch
        {
            "canceled" => ConnectCode.Cancelled,
            "unknown" => ConnectCode.Unknown,
            "invalid_argument" => ConnectCode.InvalidArgument,
            "deadline_exceeded" => ConnectCode.DeadlineExceeded,
            "not_found" => ConnectCode.NotFound,
            "already_exists" => ConnectCode.AlreadyExists,
            "permission_denied" => ConnectCode.PermissionDenied,
            "resource_exhausted" => ConnectCode.ResourceExhausted,
            "failed_precondition" => ConnectCode.FailedPrecondition,
            "aborted" => ConnectCode.Aborted,
            "out_of_range" => ConnectCode.OutOfRange,
            "unimplemented" => ConnectCode.Unimplemented,
            "internal" => ConnectCode.Internal,
            "unavailable" => ConnectCode.Unavailable,
            "data_loss" => ConnectCode.DataLoss,
            "unauthenticated" => ConnectCode.Unauthenticated,
            _ => ConnectCode.Unknown,
        };

        /// <summary>
        /// The HTTP status a server sends for this code.
        /// </summary>
        public static int ToHttpStatus(this ConnectCode code) => code switch
        {
            ConnectCode.Cancelled => 499,
            ConnectCode.Unknown => 500,
            ConnectCode.InvalidArgument => 400,
            ConnectCode.DeadlineExceeded => 504,
            ConnectCode.NotFound => 404,
            ConnectCode.AlreadyExists => 409,
            ConnectCode.PermissionDenied => 403,
            ConnectCode.ResourceExhausted => 429,
            ConnectCode.FailedPrecondition => 400,
            ConnectCode.Aborted => 409,
            ConnectCode.OutOfRange => 400,
            ConnectCode.Unimplemented => 501,
            ConnectCode.Internal => 500,
            ConnectCode.Unavailable => 503,
            ConnectCode.DataLoss => 500,
            ConnectCode.Unauthenticated => 401,
            _ => 500,
        };

        /// <summary>
        /// Infers a code from an HTTP status, for a non-200 response that carries no Connect error object.
        /// </summary>
        /// <remarks>
        /// This is not the inverse of <see cref="ToHttpStatus"/> and is not meant to be - the spec gives a
        /// separate table, because several codes share a status. Note 400 infers <see cref="ConnectCode.Internal"/>
        /// rather than <see cref="ConnectCode.InvalidArgument"/>, and 404 infers <see cref="ConnectCode.Unimplemented"/>
        /// rather than <see cref="ConnectCode.NotFound"/>: an unannotated status came from an intermediary
        /// or from the HTTP layer, not from the application, so it says nothing about the request's contents.
        /// </remarks>
        public static ConnectCode FromHttpStatus(int status) => status switch
        {
            400 => ConnectCode.Internal,
            401 => ConnectCode.Unauthenticated,
            403 => ConnectCode.PermissionDenied,
            404 => ConnectCode.Unimplemented,
            429 => ConnectCode.Unavailable,
            502 => ConnectCode.Unavailable,
            503 => ConnectCode.Unavailable,
            504 => ConnectCode.Unavailable,
            _ => ConnectCode.Unknown,
        };
    }
}
