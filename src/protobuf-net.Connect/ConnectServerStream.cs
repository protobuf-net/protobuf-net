using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using ProtoBuf.Connect.Internal;
using ProtoBuf.Serializers;

namespace ProtoBuf.Connect
{
    /// <summary>
    /// A response stream, enumerable exactly once.
    /// </summary>
    /// <remarks>
    /// Returned as <see cref="IAsyncEnumerable{T}"/> to a contract, which is what protobuf-net.Grpc's
    /// shape expects; the concrete type additionally exposes the metadata, since an
    /// <see cref="IAsyncEnumerable{T}"/> has nowhere to put it.
    /// <para>
    /// <see cref="Trailers"/> is empty until enumeration completes, and that is the point rather than a
    /// caveat: for a streaming call trailing metadata arrives in the terminating message, not in
    /// response headers as it does for unary. Anything reading trailers must go through the accessor
    /// for exactly this reason.
    /// </para>
    /// </remarks>
    public sealed class ConnectServerStream<TResponse> : IAsyncEnumerable<TResponse>
    {
        private readonly HttpResponseMessage _response;
        private readonly ConnectCodec _codec;
        private readonly IConnectMessageCodec<TResponse>? _serializer;
        private readonly string _method;
        private int _enumerated;
        private ConnectException? _failure;

        internal ConnectServerStream(
            HttpResponseMessage response,
            ConnectCodec codec,
            IConnectMessageCodec<TResponse>? serializer,
            string method,
            IReadOnlyList<KeyValuePair<string, string>> headers)
        {
            _response = response;
            _codec = codec;
            _serializer = serializer;
            _method = method;
            Headers = headers;
            Trailers = Array.Empty<KeyValuePair<string, string>>();
        }

        /// <summary>Leading metadata, available before enumeration.</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Headers { get; }

        /// <summary>Trailing metadata; empty until enumeration completes.</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Trailers { get; private set; }

        /// <inheritdoc/>
        public async IAsyncEnumerator<TResponse> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _enumerated, 1) != 0)
            {
                throw new InvalidOperationException(
                    $"The response stream for '{_method}' has already been enumerated; it reads a network connection and cannot be replayed.");
            }

            using var response = _response;
            var body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var reader = PipeReader.Create(body);

            var sawTerminator = false;
            try
            {
                while (!sawTerminator)
                {
                    var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    var buffer = result.Buffer;

                    while (!sawTerminator && ConnectEnvelope.TryRead(ref buffer, out var flags, out var payload))
                    {
                        if ((flags & ConnectEnvelope.FlagEndOfStream) != 0)
                        {
                            // a span cannot cross a yield, so the terminator is parsed out of line
                            ReadTerminator(payload);
                            sawTerminator = true;
                            break;
                        }

                        yield return ReadMessage(flags, payload);
                    }

                    reader.AdvanceTo(buffer.Start, buffer.End);

                    if (!sawTerminator && result.IsCompleted)
                    {
                        // the connection ended without the message that says how the call went, so we
                        // cannot report success: silently treating this as a clean end would turn a
                        // dropped connection into a short-but-valid stream
                        throw new ConnectException(
                            ConnectCode.Internal,
                            $"The response stream for '{_method}' ended without a terminating message.");
                    }
                }
            }
            finally
            {
                await reader.CompleteAsync().ConfigureAwait(false);
            }

            if (_failure is not null) throw _failure;
        }

        private TResponse ReadMessage(byte flags, in ReadOnlySequence<byte> payload)
        {
            RejectUnsupportedFlags(flags);
            try
            {
                return _codec.Read(payload, _serializer);
            }
            catch (Exception ex) when (ex is not ConnectException)
            {
                throw new ConnectException(
                    ConnectCode.Internal,
                    $"A message in the response stream for '{_method}' could not be read as '{_codec.Name}': {ex.Message}",
                    innerException: ex);
            }
        }

        private void ReadTerminator(in ReadOnlySequence<byte> payload)
        {
            byte[]? rented = null;
            ReadOnlySpan<byte> span;
            if (payload.IsSingleSegment)
            {
                span = payload.FirstSpan;
            }
            else
            {
                rented = ArrayPool<byte>.Shared.Rent(checked((int)payload.Length));
                payload.CopyTo(rented);
                span = rented.AsSpan(0, (int)payload.Length);
            }

            try
            {
                EndStreamReader.Parse(span, out _failure, out var trailers);
                Trailers = trailers;
            }
            finally
            {
                if (rented is not null) ArrayPool<byte>.Shared.Return(rented);
            }
        }

        private void RejectUnsupportedFlags(byte flags)
        {
            if ((flags & ConnectEnvelope.FlagCompressed) != 0)
            {
                throw new ConnectException(
                    ConnectCode.Unimplemented,
                    "The response stream is compressed; compression negotiation is not implemented yet.");
            }

            if ((flags & ConnectEnvelope.FlagReserved) != 0)
            {
                // the peer is using an extension we do not know about, so we cannot know that ignoring
                // it is safe
                throw new ConnectException(
                    ConnectCode.Internal,
                    $"An enveloped message set reserved flag bits (0x{flags:x2}).");
            }
        }
    }
}
