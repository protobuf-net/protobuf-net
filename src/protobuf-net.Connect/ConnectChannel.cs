using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf.Connect.Internal;

namespace ProtoBuf.Connect
{
    /// <summary>
    /// Invokes Connect RPCs over an <see cref="HttpClient"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="HttpClient"/> is the whole point: Connect is ordinary HTTP, so a client that did not use
    /// the ordinary HTTP stack would be throwing away HTTP/1.1, HTTP/2 and HTTP/3, proxies, authentication
    /// handlers, connection pooling, <c>IHttpClientFactory</c> and every diagnostic anyone has, in exchange
    /// for nothing.
    /// <para>
    /// Only unary is implemented. The streaming shapes need enveloped framing and a terminating message;
    /// the seams they need - a codec that is independent of framing, a request body that is an
    /// <see cref="HttpContent"/>, trailers behind an accessor, and one error path - are in place.
    /// </para>
    /// </remarks>
    public sealed class ConnectChannel
    {
        /// <summary>The protocol version header, which servers and proxies may require.</summary>
        public const string ProtocolVersionHeader = "connect-protocol-version";

        /// <summary>The value of <see cref="ProtocolVersionHeader"/> for this revision of the protocol.</summary>
        public const string ProtocolVersion = "1";

        /// <summary>The per-call timeout header.</summary>
        public const string TimeoutHeader = "connect-timeout-ms";

        private const string TrailerPrefix = "trailer-";

        private readonly HttpClient _http;
        private readonly Uri? _baseAddress;

        /// <summary>Creates a channel.</summary>
        /// <param name="http">The client to send on. Its lifetime is the caller's.</param>
        /// <param name="codec">The codec for request and response payloads.</param>
        /// <param name="baseAddress">
        /// The server's base address. May be omitted when <paramref name="http"/> already has one.
        /// </param>
        /// <param name="httpVersion">
        /// Pins the HTTP version for every call on this channel, for callers that cannot state it per
        /// call.
        /// </param>
        /// <remarks>
        /// The version is worth pinning because a plaintext endpoint has no ALPN, so it is a decision
        /// rather than an outcome. The case that needs it is <c>HttpVersion.Version11</c> with
        /// <b>half-duplex</b> bidi: <c>Duplex</c> defaults to HTTP/2 because a <em>full</em>-duplex call
        /// deadlocks without it, and half-duplex neither needs nor wants that. A generated code-first
        /// client goes through <c>CallContext</c>, which has no HTTP-version concept to carry - so for
        /// that consumer the channel is the only place to say it.
        /// </remarks>
        public ConnectChannel(HttpClient http, ConnectCodec codec, Uri? baseAddress = null, Version? httpVersion = null,
            ConnectCompression? compression = null)
        {
            _httpVersion = httpVersion;
            _compression = compression;
            _http = http ?? throw new ArgumentNullException(nameof(http));
            Codec = codec ?? throw new ArgumentNullException(nameof(codec));
            var resolved = baseAddress ?? http.BaseAddress;
            if (resolved is null)
            {
                throw new ArgumentException(
                    "A base address is required, either here or on the HttpClient.", nameof(baseAddress));
            }

            // a base address carrying a routing prefix must end in "/" or Uri treats the last segment as
            // a file name and replaces it - so "http://host/connect" would silently become "http://host/"
            _baseAddress = resolved.AbsolutePath.EndsWith("/", StringComparison.Ordinal)
                ? resolved
                : new Uri(resolved.AbsoluteUri + "/");
        }

        private readonly Version? _httpVersion;
        private readonly ConnectCompression? _compression;

        /// <summary>
        /// What this client can decode, advertised on every request.
        /// </summary>
        /// <remarks>
        /// Advertised unconditionally rather than configured, because it is a statement of capability
        /// rather than of preference: these three are in-box on .NET, so a response in any of them can
        /// be read. Whether the server uses one is its choice, and is read back off the response.
        /// </remarks>
        private const string AcceptedEncodings = "gzip, br, deflate";

        /// <summary>The codec in use.</summary>
        public ConnectCodec Codec { get; }

        /// <summary>Invokes a unary RPC and returns the response message.</summary>
        public async Task<TResponse> UnaryAsync<TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            TRequest request,
            ConnectCallOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var (response, _) = await UnaryWithMetadataAsync(method, request, options, cancellationToken).ConfigureAwait(false);
            return response;
        }

        /// <summary>Invokes a unary RPC and returns the response message together with its metadata.</summary>
        public async Task<(TResponse Response, ConnectCallResult Call)> UnaryWithMetadataAsync<TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            TRequest request,
            ConnectCallOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // the deadline is owned here and torn down with the call, which for a unary shape is the
            // whole of it - a streaming shape hands the source to its stream instead
            using var deadline = StartDeadlineSource(options, cancellationToken);
            try
            {
                return await UnaryCoreAsync(
                    method, request, options, deadline?.Token ?? cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                throw Translate(ex, deadline, cancellationToken, method!);
            }
        }

        private async Task<(TResponse Response, ConnectCallResult Call)> UnaryCoreAsync<TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            TRequest request,
            ConnectCallOptions? options,
            CancellationToken cancellationToken)
        {
            if (method is null) throw new ArgumentNullException(nameof(method));
            if (method.Type != ConnectMethodType.Unary)
            {
                throw new NotSupportedException(
                    $"'{method}' is {method.Type}; only unary calls are implemented so far.");
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ResolveUri(method))
            {
                // the bare message, no envelope - unary framing is the absence of framing
                Content = new MeasuredCodecContent<TRequest>(
                    Codec, request, Codec.ContentTypeFor(method.Type), method.RequestCodec,
                    options?.Compression ?? _compression),
            };
            ApplyOptions(httpRequest, options, unary: true);

            // ResponseHeadersRead throughout, not just where it is needed: it is what a streaming shape
            // requires, and using it for unary too keeps one path rather than two
            using var httpResponse = await _http
                .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                throw await ReadErrorAsync(httpResponse, cancellationToken).ConfigureAwait(false);
            }

            // a 200 with the wrong framing is not a payload we can read
            ValidateContentType(httpResponse, method.Type, method);

            // unary is small by construction and the codec reads a whole message, so the body is taken in
            // one piece; a streaming response reads from the PipeReader incrementally instead
            var body = await httpResponse.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            var payload = new ReadOnlySequence<byte>(body);

            if (ResponseCompression(httpResponse, unary: true, method) is { } decompressor
                && !ConnectCompression.IsIdentity(decompressor.Name))
            {
                payload = new ReadOnlySequence<byte>(decompressor.Decompress(payload));
            }

            TResponse value;
            try
            {
                value = Codec.Read(payload, method.ResponseCodec);
            }
            catch (Exception ex) when (ex is not ConnectException)
            {
                throw new ConnectException(
                    ConnectCode.Internal,
                    $"The response to '{method}' could not be read as '{Codec.Name}': {ex.Message}",
                    (int)httpResponse.StatusCode,
                    innerException: ex);
            }

            return (value, ReadMetadata(httpResponse));
        }

        /// <summary>
        /// Invokes a server-streaming RPC. Nothing is sent until the result is enumerated.
        /// </summary>
        /// <remarks>
        /// Deferred, which is what an <see cref="IAsyncEnumerable{T}"/> implies and what a contract's
        /// signature needs, since it returns synchronously. Use
        /// <see cref="ServerStreamingAsync{TRequest, TResponse}"/> where the response metadata matters.
        /// </remarks>
        public async IAsyncEnumerable<TResponse> ServerStreaming<TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            TRequest request,
            ConnectCallOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var stream = await ServerStreamingAsync(method, request, options, cancellationToken).ConfigureAwait(false);
            await foreach (var response in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return response;
            }
        }

        /// <summary>
        /// Starts a server-streaming RPC, returning once the response headers have arrived.
        /// </summary>
        public async Task<ConnectServerStream<TResponse>> ServerStreamingAsync<TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            TRequest request,
            ConnectCallOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var deadline = StartDeadlineSource(options, cancellationToken);
            if (deadline is not null) cancellationToken = deadline.Token;

            if (method is null) throw new ArgumentNullException(nameof(method));
            if (method.Type != ConnectMethodType.ServerStreaming)
            {
                throw new NotSupportedException(
                    $"'{method}' is {method.Type}; this call shape is for {nameof(ConnectMethodType.ServerStreaming)}.");
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ResolveUri(method))
            {
                // one enveloped message: a streaming RPC frames both directions, whatever the cardinality
                Content = new EnvelopedCodecContent<TRequest>(
                    Codec, request, Codec.ContentTypeFor(method.Type), method.RequestCodec,
                    options?.Compression ?? _compression),
            };
            ApplyOptions(httpRequest, options, unary: false);

            var httpResponse = await _http
                .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            // a stream that fails *after* it starts carries 200 and says so in its terminating message;
            // a non-200 here means it never started, and reads exactly like a failed unary call
            if (!httpResponse.IsSuccessStatusCode)
            {
                try
                {
                    throw await ReadErrorAsync(httpResponse, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    httpResponse.Dispose();
                }
            }

            // a 200 with the wrong framing is not a payload we can read
            ValidateContentType(httpResponse, method.Type, method);

            var metadata = ReadMetadata(httpResponse);
            return new ConnectServerStream<TResponse>(
                httpResponse, Codec, method.ResponseCodec, method.ToString(), metadata.Headers, deadline,
                ResponseCompression(httpResponse, unary: false, method));
        }

        /// <summary>
        /// Invokes a client-streaming RPC: a sequence of requests, one response.
        /// </summary>
        /// <remarks>
        /// The response side is exactly a server-streaming response that happens to carry one message,
        /// so it reuses <see cref="ConnectServerStream{TResponse}"/> rather than repeating the
        /// envelope-and-terminator handling. What is new is the request: it is produced as it is sent,
        /// so it cannot state a <c>Content-Length</c> and goes out chunked.
        /// </remarks>
        public async Task<TResponse> ClientStreamingAsync<TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            IAsyncEnumerable<TRequest> requests,
            ConnectCallOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var (response, _) = await ClientStreamingWithMetadataAsync(method, requests, options, cancellationToken)
                .ConfigureAwait(false);
            return response;
        }

        /// <summary>
        /// Invokes a client-streaming RPC and returns the response together with its metadata.
        /// </summary>
        /// <remarks>
        /// The metadata form matters more here than it looks: trailing metadata on a client-streaming call
        /// arrives in the <em>terminating envelope</em>, not in the response headers, so it is unreachable
        /// to a caller holding only the response message. A <c>CallInvoker</c> must surface it.
        /// </remarks>
        public async Task<(TResponse Response, ConnectCallResult Call)> ClientStreamingWithMetadataAsync<TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            IAsyncEnumerable<TRequest> requests,
            ConnectCallOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            using var deadline = StartDeadlineSource(options, cancellationToken);
            try
            {
                return await ClientStreamingCoreAsync(
                    method, requests, options, deadline?.Token ?? cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                throw Translate(ex, deadline, cancellationToken, method!);
            }
        }

        private async Task<(TResponse Response, ConnectCallResult Call)> ClientStreamingCoreAsync<TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            IAsyncEnumerable<TRequest> requests,
            ConnectCallOptions? options,
            CancellationToken cancellationToken)
        {
            if (method is null) throw new ArgumentNullException(nameof(method));
            if (requests is null) throw new ArgumentNullException(nameof(requests));
            if (method.Type != ConnectMethodType.ClientStreaming)
            {
                throw new NotSupportedException(
                    $"'{method}' is {method.Type}; this call shape is for {nameof(ConnectMethodType.ClientStreaming)}.");
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ResolveUri(method))
            {
                Content = new EnvelopedStreamContent<TRequest>(
                    Codec, requests, Codec.ContentTypeFor(method.Type), method.RequestCodec, cancellationToken,
                    options?.Compression ?? _compression),
            };
            ApplyOptions(httpRequest, options, unary: false);

            var httpResponse = await _http
                .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                try
                {
                    throw await ReadErrorAsync(httpResponse, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    httpResponse.Dispose();
                }
            }

            // a 200 with the wrong framing is not a payload we can read
            ValidateContentType(httpResponse, method.Type, method);

            var metadata = ReadMetadata(httpResponse);
            var stream = new ConnectServerStream<TResponse>(
                httpResponse, Codec, method.ResponseCodec, method.ToString(), metadata.Headers,
                compression: ResponseCompression(httpResponse, unary: false, method));

            TResponse? response = default;
            var count = 0;
            await foreach (var message in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                response = message;
                count++;
            }

            // exactly one, by the shape's definition; anything else is the peer disagreeing with us
            // about what this method is
            if (count != 1)
            {
                // `unimplemented`, not `internal`: the peer answered a shape it does not implement the way
                // this method is declared, which is a disagreement about the contract rather than a fault
                // on either side. The conformance suite pins it.
                throw new ConnectException(
                    ConnectCode.Unimplemented,
                    $"'{method}' is client-streaming and must answer with exactly one message; {count} arrived.");
            }

            return (response!, new ConnectCallResult(stream.Headers, stream.Trailers));
        }

        /// <summary>
        /// Invokes a bidirectional-streaming RPC. Nothing is sent until the result is enumerated.
        /// </summary>
        public async IAsyncEnumerable<TResponse> Duplex<TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            IAsyncEnumerable<TRequest> requests,
            ConnectCallOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var stream = await DuplexAsync(method, requests, options, cancellationToken).ConfigureAwait(false);
            await foreach (var response in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return response;
            }
        }

        /// <summary>
        /// Starts a bidirectional-streaming RPC, returning once the response headers have arrived.
        /// </summary>
        /// <remarks>
        /// <b>HTTP/2 only</b>, and the one shape for which that is true: interleaving a request body
        /// with a response body is something HTTP/1.1 cannot express. The request is pinned to
        /// <see cref="HttpVersionPolicy.RequestVersionExact"/> rather than left to negotiate, because a
        /// plaintext endpoint has no ALPN and would otherwise silently settle on HTTP/1.1 - at which
        /// point the call deadlocks rather than failing, since the client waits for a response the
        /// server cannot send until the request completes.
        /// <para>
        /// Everything else is composition: the request body is the same
        /// <see cref="Internal.EnvelopedStreamContent{T}"/> client-streaming uses, and the response is
        /// the same <see cref="ConnectServerStream{TResponse}"/> server-streaming returns. Duplex adds
        /// no framing of its own, which is exactly what probing the protocol predicted.
        /// </para>
        /// </remarks>
        public async Task<ConnectServerStream<TResponse>> DuplexAsync<TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            IAsyncEnumerable<TRequest> requests,
            ConnectCallOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var deadline = StartDeadlineSource(options, cancellationToken);
            if (deadline is not null) cancellationToken = deadline.Token;

            if (method is null) throw new ArgumentNullException(nameof(method));
            if (requests is null) throw new ArgumentNullException(nameof(requests));
            if (method.Type != ConnectMethodType.DuplexStreaming)
            {
                throw new NotSupportedException(
                    $"'{method}' is {method.Type}; this call shape is for {nameof(ConnectMethodType.DuplexStreaming)}.");
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ResolveUri(method))
            {
                Content = new EnvelopedStreamContent<TRequest>(
                    Codec, requests, Codec.ContentTypeFor(method.Type), method.RequestCodec, cancellationToken,
                    options?.Compression ?? _compression),

                // the DEFAULT, not the rule: full duplex needs HTTP/2, half duplex does not, and only the
                // caller knows which this is. ApplyOptions overrides it when the caller said so.
                Version = System.Net.HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            };
            ApplyOptions(httpRequest, options, unary: false);

            var httpResponse = await _http
                .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                try
                {
                    throw await ReadErrorAsync(httpResponse, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    httpResponse.Dispose();
                }
            }

            // a 200 with the wrong framing is not a payload we can read
            ValidateContentType(httpResponse, method.Type, method);

            var metadata = ReadMetadata(httpResponse);
            return new ConnectServerStream<TResponse>(
                httpResponse, Codec, method.ResponseCodec, method.ToString(), metadata.Headers, deadline,
                ResponseCompression(httpResponse, unary: false, method));
        }

        /// <summary>
        /// Combines the method with the base address, preserving any routing prefix the base carries.
        /// </summary>
        private Uri ResolveUri<TRequest, TResponse>(ConnectMethod<TRequest, TResponse> method)
            => new(_baseAddress!, method.RelativePath);

        /// <summary>
        /// Starts the call's deadline, returning the token every step of it should observe.
        /// </summary>
        /// <remarks>
        /// <b>A deadline has to be enforced, not merely advertised.</b> <c>connect-timeout-ms</c> tells the
        /// server what we are willing to wait for, and a well-behaved one honours it - but a caller that
        /// sets a deadline is asking for the call to end by then, whatever the server does. gRPC clients
        /// enforce it locally for exactly this reason, and the conformance suite's reference server tests
        /// the point directly: it delays past the deadline and expects the client to give up on its own.
        /// <para>
        /// The source is returned so the caller can dispose it - or, for a streaming shape, hand it to the
        /// stream, whose reads outlive the method that started them.
        /// </para>
        /// </remarks>
        private static CancellationTokenSource? StartDeadlineSource(
            ConnectCallOptions? options, CancellationToken cancellationToken)
        {
            if (options?.Timeout is not { } timeout) return null;

            var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(timeout);
            return deadline;
        }

        /// <summary>
        /// Reports a cancellation as a lapsed deadline where that is what it was.
        /// </summary>
        /// <remarks>
        /// The caller's own cancellation is checked first and left alone: it is not a deadline, and
        /// saying so would be wrong in the one direction a caller would notice.
        /// </remarks>
        private static Exception Translate(
            OperationCanceledException exception, CancellationTokenSource? deadline,
            CancellationToken cancellationToken, object method)
        {
            if (cancellationToken.IsCancellationRequested || deadline?.IsCancellationRequested != true) return exception;

            return new ConnectException(
                ConnectCode.DeadlineExceeded, $"The call to '{method}' exceeded its deadline.", innerException: exception);
        }

        private void ApplyOptions(HttpRequestMessage request, ConnectCallOptions? options, bool unary)
        {
            request.Headers.TryAddWithoutValidation(ProtocolVersionHeader, ProtocolVersion);

            var compression = options?.Compression ?? _compression;
            if (compression is not null && !ConnectCompression.IsIdentity(compression.Name))
            {
                if (unary)
                {
                    // Content-Encoding is a CONTENT header, not a request header - HttpRequestMessage.Headers
                    // drops it silently, which looks exactly like not compressing at all. The symptom is the
                    // peer reporting a parse error on bytes beginning 0x1f 0x8b, which is gzip's magic:
                    // the body was compressed and nothing said so.
                    request.Content?.Headers.TryAddWithoutValidation("content-encoding", compression.Name);
                }
                else
                {
                    // ...whereas connect-content-encoding is Connect's own, and belongs on the request
                    request.Headers.TryAddWithoutValidation("connect-content-encoding", compression.Name);
                }
            }

            request.Headers.TryAddWithoutValidation(
                unary ? "accept-encoding" : "connect-accept-encoding", AcceptedEncodings);

            // a stated version wins over anything the call shape chose for itself, and is pinned exactly:
            // a plaintext endpoint has no ALPN, so a version left to negotiate silently becomes HTTP/1.1.
            // Per call first, then the channel's default.
            if ((options?.HttpVersion ?? _httpVersion) is { } version)
            {
                request.Version = version;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
            }

            if (options?.Timeout is { } timeout)
            {
                var ms = (long)Math.Ceiling(timeout.TotalMilliseconds);
                if (ms < 0) throw new ArgumentOutOfRangeException(nameof(options), "The timeout cannot be negative.");
                request.Headers.TryAddWithoutValidation(TimeoutHeader, ms.ToString(CultureInfo.InvariantCulture));
            }

            if (options?.Headers is { } headers)
            {
                foreach (var header in headers) request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        /// <summary>
        /// Checks that a successful response is framed the way this call asked for.
        /// </summary>
        /// <remarks>
        /// A 200 with the wrong content-type is not a payload we can read, and reading it anyway means
        /// handing arbitrary bytes to a codec and reporting whatever comes out. The two failures are
        /// told apart deliberately, because they mean different things to a caller:
        /// <list type="bullet">
        /// <item><description>not a Connect content-type at all (<c>image/jpeg</c>) - the peer is not
        /// speaking this protocol, which is <see cref="ConnectCode.Unknown"/>;</description></item>
        /// <item><description>a Connect content-type naming a <em>different codec</em>
        /// (<c>application/json</c> where proto was asked for) - it is speaking the protocol and got the
        /// negotiation wrong, which is <see cref="ConnectCode.Internal"/>.</description></item>
        /// </list>
        /// </remarks>
        private void ValidateContentType(HttpResponseMessage response, ConnectMethodType type, object method)
        {
            var expected = Codec.ContentTypeFor(type);
            var actual = response.Content.Headers.ContentType?.MediaType;

            if (string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)) return;

            // the framing prefix the shape requires, with any codec after it
            var prefix = type == ConnectMethodType.Unary ? "application/" : "application/connect+";
            var isConnect = actual is not null
                && actual.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && actual.Length > prefix.Length;

            throw new ConnectException(
                isConnect ? ConnectCode.Internal : ConnectCode.Unknown,
                $"The response to '{method}' is '{actual ?? "<none>"}', but this call asked for '{expected}'.");
        }

        /// <summary>
        /// The compression a response actually used, from what it says rather than what we asked for.
        /// </summary>
        /// <remarks>
        /// Read off the response deliberately: a server is free to answer uncompressed whatever the
        /// accept header offered - and will, for a message too small to be worth it - so assuming our own
        /// preference would decode garbage. An encoding we do not have is the server's mistake rather
        /// than the caller's, hence <c>internal</c>.
        /// </remarks>
        private static ConnectCompression ResponseCompression(HttpResponseMessage response, bool unary, object method)
        {
            var header = unary
                ? response.Content.Headers.ContentEncoding.FirstOrDefault()
                : (response.Headers.TryGetValues("connect-content-encoding", out var values)
                    ? values.FirstOrDefault() : null);

            if (ConnectCompression.IsIdentity(header)) return ConnectCompression.Identity;

            foreach (var known in new[] { ConnectCompression.Gzip, ConnectCompression.Brotli, ConnectCompression.Deflate })
            {
                if (string.Equals(known.Name, header, StringComparison.OrdinalIgnoreCase)) return known;
            }

            throw new ConnectException(
                ConnectCode.Internal,
                $"The response to '{method}' is encoded as '{header}', which this client cannot decode.");
        }

        private static ConnectCallResult ReadMetadata(HttpResponseMessage response)
        {
            List<KeyValuePair<string, string>>? headers = null;
            List<KeyValuePair<string, string>>? trailers = null;

            foreach (var header in response.Headers)
            {
                var value = string.Join(",", header.Value);
                if (header.Key.StartsWith(TrailerPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // the prefix is a wire detail and is stripped on the way in
                    (trailers ??= new()).Add(new(header.Key[TrailerPrefix.Length..], value));
                }
                else
                {
                    (headers ??= new()).Add(new(header.Key, value));
                }
            }

            return new ConnectCallResult(
                (IReadOnlyList<KeyValuePair<string, string>>?)headers ?? Array.Empty<KeyValuePair<string, string>>(),
                (IReadOnlyList<KeyValuePair<string, string>>?)trailers ?? Array.Empty<KeyValuePair<string, string>>());
        }

        private static async Task<ConnectException> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var status = (int)response.StatusCode;

            // the metadata belongs to the error as much as to a success; see ConnectException.Headers
            var metadata = ReadMetadata(response);

            // A non-200 does NOT imply a Connect error object. An unrouted path is answered by the HTTP
            // layer - measured: text/plain, no body of ours at all - so the status has to be enough on its
            // own, and the protocol supplies an inference table for exactly that.
            byte[] payload;
            try
            {
                payload = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new ConnectException(
                    ConnectCodes.FromHttpStatus(status), response.ReasonPhrase, status,
                    codeWasInferred: true, innerException: ex)
                    { Headers = metadata.Headers, Trailers = metadata.Trailers };
            }

            // The ERROR BODY is compressed too, and forgetting that produces the most confusing possible
            // failure: the JSON parse fails, the body is reported as an opaque message, and the caller
            // sees an error whose text is gzip. Errors are always the unary shape - a non-200 has no
            // envelopes - so it is content-encoding either way.
            try
            {
                var encoding = ResponseCompression(response, unary: true, "the error response");
                if (!ConnectCompression.IsIdentity(encoding.Name))
                {
                    payload = encoding.Decompress(new ReadOnlySequence<byte>(payload));
                }
            }
            catch (ConnectException)
            {
                // an encoding we cannot decode leaves the bytes alone; the status still says what
                // happened, and inventing a decode failure on top of a failure helps nobody
            }

            var isJson = string.Equals(
                response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase);

            if (isJson && ConnectErrorReader.TryParse(payload, out var code, out var message, out var details, out var hasCode))
            {
                // an error object that did not name a usable code still owns its message and details;
                // only the code comes from the status instead
                return new ConnectException(
                    hasCode ? code : ConnectCodes.FromHttpStatus(status),
                    message, status, details, codeWasInferred: !hasCode)
                    { Headers = metadata.Headers, Trailers = metadata.Trailers };
            }

            return new ConnectException(
                ConnectCodes.FromHttpStatus(status),
                DescribeOpaqueBody(payload, response.ReasonPhrase),
                status,
                codeWasInferred: true)
                { Headers = metadata.Headers, Trailers = metadata.Trailers };
        }

        private static string? DescribeOpaqueBody(byte[] payload, string? reasonPhrase)
        {
            if (payload.Length == 0) return reasonPhrase;
            // a short text body is nearly always the useful part of an intermediary's error page
            const int Max = 512;
            var text = System.Text.Encoding.UTF8.GetString(payload, 0, Math.Min(payload.Length, Max)).Trim();
            if (text.Length == 0) return reasonPhrase;
            return payload.Length > Max ? text + "..." : text;
        }
    }
}
