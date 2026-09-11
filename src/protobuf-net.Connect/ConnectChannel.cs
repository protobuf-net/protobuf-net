using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Globalization;
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
        public ConnectChannel(HttpClient http, ConnectCodec codec, Uri? baseAddress = null)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            Codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _baseAddress = baseAddress ?? http.BaseAddress;
            if (_baseAddress is null)
            {
                throw new ArgumentException(
                    "A base address is required, either here or on the HttpClient.", nameof(baseAddress));
            }
        }

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
            if (method is null) throw new ArgumentNullException(nameof(method));
            if (method.Type != ConnectMethodType.Unary)
            {
                throw new NotSupportedException(
                    $"'{method}' is {method.Type}; only unary calls are implemented so far.");
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseAddress!, method.Path))
            {
                // the bare message, no envelope - unary framing is the absence of framing
                Content = new MeasuredCodecContent<TRequest>(
                    Codec, request, Codec.ContentTypeFor(method.Type), method.RequestSerializer),
            };
            ApplyOptions(httpRequest, options);

            // ResponseHeadersRead throughout, not just where it is needed: it is what a streaming shape
            // requires, and using it for unary too keeps one path rather than two
            using var httpResponse = await _http
                .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                throw await ReadErrorAsync(httpResponse, cancellationToken).ConfigureAwait(false);
            }

            // unary is small by construction and the codec reads a whole message, so the body is taken in
            // one piece; a streaming response reads from the PipeReader incrementally instead
            var body = await httpResponse.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            TResponse value;
            try
            {
                value = Codec.Read(new ReadOnlySequence<byte>(body), method.ResponseSerializer);
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
            if (method is null) throw new ArgumentNullException(nameof(method));
            if (method.Type != ConnectMethodType.ServerStreaming)
            {
                throw new NotSupportedException(
                    $"'{method}' is {method.Type}; this call shape is for {nameof(ConnectMethodType.ServerStreaming)}.");
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseAddress!, method.Path))
            {
                // one enveloped message: a streaming RPC frames both directions, whatever the cardinality
                Content = new EnvelopedCodecContent<TRequest>(
                    Codec, request, Codec.ContentTypeFor(method.Type), method.RequestSerializer),
            };
            ApplyOptions(httpRequest, options);

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

            var metadata = ReadMetadata(httpResponse);
            return new ConnectServerStream<TResponse>(
                httpResponse, Codec, method.ResponseSerializer, method.ToString(), metadata.Headers);
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
            if (method is null) throw new ArgumentNullException(nameof(method));
            if (requests is null) throw new ArgumentNullException(nameof(requests));
            if (method.Type != ConnectMethodType.ClientStreaming)
            {
                throw new NotSupportedException(
                    $"'{method}' is {method.Type}; this call shape is for {nameof(ConnectMethodType.ClientStreaming)}.");
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseAddress!, method.Path))
            {
                Content = new EnvelopedStreamContent<TRequest>(
                    Codec, requests, Codec.ContentTypeFor(method.Type), method.RequestSerializer, cancellationToken),
            };
            ApplyOptions(httpRequest, options);

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

            var metadata = ReadMetadata(httpResponse);
            var stream = new ConnectServerStream<TResponse>(
                httpResponse, Codec, method.ResponseSerializer, method.ToString(), metadata.Headers);

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
                throw new ConnectException(
                    ConnectCode.Internal,
                    $"'{method}' is client-streaming and must answer with exactly one message; {count} arrived.");
            }

            return response!;
        }

        private static void ApplyOptions(HttpRequestMessage request, ConnectCallOptions? options)
        {
            request.Headers.TryAddWithoutValidation(ProtocolVersionHeader, ProtocolVersion);

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
                    codeWasInferred: true, innerException: ex);
            }

            var isJson = string.Equals(
                response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase);

            if (isJson && ConnectErrorReader.TryParse(payload, out var code, out var message, out var details))
            {
                return new ConnectException(code, message, status, details);
            }

            return new ConnectException(
                ConnectCodes.FromHttpStatus(status),
                DescribeOpaqueBody(payload, response.ReasonPhrase),
                status,
                codeWasInferred: true);
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
