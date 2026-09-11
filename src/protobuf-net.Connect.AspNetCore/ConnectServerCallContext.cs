using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace ProtoBuf.Connect.AspNetCore
{
    /// <summary>
    /// A <see cref="ServerCallContext"/> over an ASP.NET Core request, so that a contract written for
    /// protobuf-net.Grpc can be served over Connect unchanged.
    /// </summary>
    /// <remarks>
    /// This exists because protobuf-net.Grpc's <c>CallContext</c> has exactly two constructors, and the
    /// server-side one takes a <see cref="ServerCallContext"/>. Sharing the contract vocabulary therefore
    /// means implementing this - it is not merely a package reference. grpc-dotnet does the same thing
    /// (<c>HttpContextServerCallContext</c>), so the shape is proven; twelve abstract members, all of which
    /// an <see cref="Microsoft.AspNetCore.Http.HttpContext"/> can answer.
    /// <para>
    /// Two of them cannot be answered honestly and say so rather than inventing an answer:
    /// <see cref="CreatePropagationTokenCore"/> is a Grpc.Core-native concept with no Connect equivalent,
    /// and <see cref="AuthContextCore"/> describes transport-level peer identity that we do not collect -
    /// ASP.NET Core authentication lands on <c>HttpContext.User</c>, which is reachable through
    /// <see cref="HttpContext"/>.
    /// </para>
    /// </remarks>
    public sealed class ConnectServerCallContext : ServerCallContext
    {
        private readonly CancellationToken _cancellationToken;
        private readonly string _method;
        private readonly DateTime _deadline;
        private Metadata? _requestHeaders;
        private Metadata? _responseTrailers;
        private Dictionary<object, object>? _userState;

        internal ConnectServerCallContext(HttpContext httpContext, string method, DateTime deadline, CancellationToken cancellationToken)
        {
            HttpContext = httpContext;
            _method = method;
            _deadline = deadline;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// The underlying HTTP request. Exposed because a Connect endpoint really is an ordinary HTTP
        /// endpoint, and hiding that would throw away the reason to prefer it.
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <inheritdoc/>
        protected override string MethodCore => _method;

        /// <inheritdoc/>
        protected override string HostCore => HttpContext.Request.Host.Value ?? string.Empty;

        /// <inheritdoc/>
        protected override string PeerCore
        {
            get
            {
                var address = HttpContext.Connection.RemoteIpAddress;
                if (address is null) return string.Empty;
                // the shape grpc-dotnet uses, so anything parsing it keeps working
                return $"ipv{(address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? "6" : "4")}:{address}:{HttpContext.Connection.RemotePort}";
            }
        }

        /// <inheritdoc/>
        protected override DateTime DeadlineCore => _deadline;

        /// <inheritdoc/>
        protected override Metadata RequestHeadersCore => _requestHeaders ??= BuildRequestHeaders();

        /// <inheritdoc/>
        protected override CancellationToken CancellationTokenCore => _cancellationToken;

        /// <inheritdoc/>
        protected override Metadata ResponseTrailersCore => _responseTrailers ??= new Metadata();

        /// <summary>
        /// The status the handler wishes to report. A non-OK status is turned into a Connect error, which
        /// is possible without a translation table because <see cref="ConnectCode"/> deliberately uses
        /// gRPC's ordinals.
        /// </summary>
        protected override Status StatusCore { get; set; } = Status.DefaultSuccess;

        /// <inheritdoc/>
        protected override WriteOptions? WriteOptionsCore { get; set; }

        /// <inheritdoc/>
        protected override AuthContext AuthContextCore
            // transport-level peer identity, which we do not collect; ASP.NET Core authentication is on
            // HttpContext.User, reachable through HttpContext
            => new(null, new Dictionary<string, List<AuthProperty>>());

        /// <inheritdoc/>
        protected override IDictionary<object, object> UserStateCore => _userState ??= new Dictionary<object, object>();

        /// <inheritdoc/>
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
            => throw new NotSupportedException(
                "Context propagation is a Grpc.Core concept with no Connect equivalent; propagate "
                + nameof(CancellationToken) + " explicitly instead.");

        /// <inheritdoc/>
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
        {
            if (HttpContext.Response.HasStarted)
            {
                throw new InvalidOperationException("The response has already started; leading metadata cannot be sent now.");
            }

            if (responseHeaders is not null)
            {
                foreach (var entry in responseHeaders)
                {
                    HttpContext.Response.Headers.Append(
                        entry.Key, entry.IsBinary ? Convert.ToBase64String(entry.ValueBytes) : entry.Value);
                }
            }

            return HttpContext.Response.StartAsync(_cancellationToken);
        }

        private Metadata BuildRequestHeaders()
        {
            var metadata = new Metadata();
            foreach (var header in HttpContext.Request.Headers)
            {
                var key = header.Key;
                // the protocol reserves the connect- prefix, and the HTTP/2 pseudo-headers are not metadata
                if (key.StartsWith(":", StringComparison.Ordinal)) continue;
                if (key.StartsWith("connect-", StringComparison.OrdinalIgnoreCase)) continue;

                var value = header.Value.ToString();
                if (key.EndsWith(Metadata.BinaryHeaderSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    // binary metadata is unpadded base64 on the wire; Metadata wants the bytes
                    if (TryDecodeBase64(value, out var bytes)) metadata.Add(key, bytes);
                }
                else
                {
                    metadata.Add(key, value);
                }
            }
            return metadata;
        }

        private static bool TryDecodeBase64(string text, out byte[] bytes)
        {
            var padding = (4 - (text.Length % 4)) % 4;
            if (padding != 3)
            {
                if (padding != 0) text += new string('=', padding);
                try
                {
                    bytes = Convert.FromBase64String(text);
                    return true;
                }
                catch (FormatException) { }
            }
            bytes = Array.Empty<byte>();
            return false;
        }

        /// <summary>
        /// Writes any trailing metadata the handler added, as <c>trailer-</c> prefixed response headers.
        /// </summary>
        /// <remarks>
        /// This is where the protocol's avoidance of HTTP trailers - and therefore of HTTP/2 - actually
        /// happens for a unary call. It must run before the body, because the response commits on first
        /// write. A streaming call will instead carry the same values in the terminating message.
        /// </remarks>
        internal void FlushTrailers()
        {
            if (_responseTrailers is null) return;
            foreach (var entry in _responseTrailers)
            {
                HttpContext.Response.Headers.Append(
                    "trailer-" + entry.Key, entry.IsBinary ? Convert.ToBase64String(entry.ValueBytes) : entry.Value);
            }
        }

        /// <summary>The status the handler set, as a Connect error, or <c>null</c> when it reported success.</summary>
        internal ConnectException? GetReportedFailure()
        {
            var status = StatusCore;
            if (status.StatusCode == StatusCode.OK) return null;

            // ConnectCode's ordinals are gRPC's, deliberately, so this is a cast rather than a table
            return new ConnectException((ConnectCode)(int)status.StatusCode, status.Detail);
        }

        internal static DateTime DeadlineFrom(TimeSpan? timeout)
            => timeout is { } span ? DateTime.UtcNow.Add(span) : DateTime.MaxValue;

        internal static string FormatDeadline(DateTime deadline)
            => deadline == DateTime.MaxValue ? "none" : deadline.ToString("O", CultureInfo.InvariantCulture);
    }
}
