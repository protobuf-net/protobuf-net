using System.Threading;
using Microsoft.AspNetCore.Http;

namespace ProtoBuf.Connect.AspNetCore;

/// <summary>
/// What a service implementation is told about the call it is serving.
/// </summary>
/// <remarks>
/// Deliberately thin, and deliberately <em>not</em> an attempt at protobuf-net.Grpc's
/// <c>CallContext</c>: what the contract-facing vocabulary should be is an open decision recorded in
/// <c>notes/connect/findings.md</c>, and picking one here would pre-empt it. <see cref="HttpContext"/>
/// is exposed because a Connect endpoint really is an ordinary HTTP endpoint, and pretending otherwise
/// would throw away the reason to prefer it.
/// </remarks>
public sealed class ConnectServerCallContext
{
    internal ConnectServerCallContext(HttpContext httpContext, CancellationToken cancellationToken)
    {
        HttpContext = httpContext;
        CancellationToken = cancellationToken;
    }

    /// <summary>The underlying HTTP request.</summary>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// Cancelled when the client goes away or the call's <c>connect-timeout-ms</c> elapses.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Reads a piece of leading metadata, or <c>null</c> when it was not sent.</summary>
    public string? GetRequestHeader(string name)
        => HttpContext.Request.Headers.TryGetValue(name, out var values) ? values.ToString() : null;

    /// <summary>
    /// Adds a piece of trailing metadata.
    /// </summary>
    /// <remarks>
    /// For a unary call this is written as a <c>trailer-</c> prefixed response header, which is how the
    /// protocol avoids HTTP trailers and therefore HTTP/2. Callers should not apply the prefix; a
    /// streaming implementation will send the same value in the terminating message instead.
    /// </remarks>
    public void AddTrailer(string name, string value)
        => HttpContext.Response.Headers.Append("trailer-" + name, value);
}
