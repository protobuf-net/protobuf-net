using System.Collections.Generic;

namespace ProtoBuf.Connect.AspNetCore
{
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
        /// The compressions this server accepts and may use, most preferred first.
        /// </summary>
        /// <remarks>
        /// Empty by default: compression is opt-in, because it is a CPU cost the caller did not
        /// necessarily ask anyone to pay and because a server that advertises an encoding must be able to
        /// produce it. <c>identity</c> is always accepted and never needs listing - a peer that states it
        /// explicitly is asking for no compression, not for something unknown.
        /// </remarks>
        public IList<ConnectCompression> Compressions { get; } = new List<ConnectCompression>();

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
}
