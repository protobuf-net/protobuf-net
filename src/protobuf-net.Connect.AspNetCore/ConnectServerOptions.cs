using System.Collections.Generic;

namespace ProtoBuf.Connect.AspNetCore;

/// <summary>
/// Server-wide Connect configuration.
/// </summary>
public sealed class ConnectServerOptions
{
    /// <summary>
    /// The codecs this server accepts, in preference order.
    /// </summary>
    /// <remarks>
    /// A list rather than a single codec because the protocol negotiates: the request's content-type
    /// names the codec, and one this server does not have is answered <c>415</c>. Today that means a
    /// server registering only <see cref="ProtoConnectCodec"/> declines <c>application/json</c>, which
    /// is legal - and is what makes a binary-only implementation a real one rather than a fudge.
    /// </remarks>
    public IList<ConnectCodec> Codecs { get; } = new List<ConnectCodec>();

    /// <summary>
    /// Whether an exception's message is included in the error sent to the client. Off by default:
    /// an unhandled exception's message is not written for a caller to read.
    /// </summary>
    /// <remarks>
    /// A <see cref="ConnectException"/> is always reported in full, because throwing one is a
    /// deliberate statement to the caller. This only governs everything else.
    /// </remarks>
    public bool IncludeExceptionDetailInErrors { get; set; }

    /// <summary>
    /// Whether a request must carry <c>connect-protocol-version: 1</c>. Off by default, matching the
    /// protocol, which says clients <em>should</em> send it and servers <em>may</em> reject without it.
    /// </summary>
    public bool RequireProtocolVersionHeader { get; set; }
}
