using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly CancellationTokenSource? _deadline;
        private readonly ConnectCompression? _compression;

        internal ConnectServerStream(
            HttpResponseMessage response,
            ConnectCodec codec,
            IConnectMessageCodec<TResponse>? serializer,
            string method,
            IReadOnlyList<KeyValuePair<string, string>> headers,
            CancellationTokenSource? deadline = null,
            ConnectCompression? compression = null)
        {
            _deadline = deadline;
            _compression = compression;
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

            // the call's deadline outlives the method that started it, so the timer is owned here and
            // stops when enumeration does
            using var deadline = _deadline;

            // ...and it has to be JOINED to the enumeration token, which comes from whoever enumerates
            // and knows nothing about it. Without this the deadline is linked to the caller's token and
            // fires into a void: the reads below observe only what the enumerator was handed, so a
            // streaming call would run past its deadline and deliver the whole response late.
            using var linked = deadline is null
                ? null
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
            if (linked is not null) cancellationToken = linked.Token;

            var body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var reader = PipeReader.Create(body);

            var sawTerminator = false;
            try
            {
                while (!sawTerminator)
                {
                    var result = await ReadAsync(reader, cancellationToken).ConfigureAwait(false);
                    var buffer = result.Buffer;

                    while (!sawTerminator && ConnectEnvelope.TryRead(ref buffer, out var flags, out var payload))
                    {
                        if ((flags & ConnectEnvelope.FlagEndOfStream) != 0)
                        {
                            // a span cannot cross a yield, so the terminator is parsed out of line
                            ReadTerminator(flags, payload);
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

        /// <summary>
        /// Reads, reporting a lapsed deadline as one.
        /// </summary>
        /// <remarks>
        /// Its own method because the enumerator cannot catch around its loop - a <c>yield return</c>
        /// may not appear inside a <c>try</c> that has a <c>catch</c> - so the translation has to wrap
        /// the individual await instead.
        /// <para>
        /// Without it a deadline surfaces as a bare <see cref="OperationCanceledException"/>, which is
        /// indistinguishable from the caller cancelling and is not what a caller that set a deadline
        /// asked to be told.
        /// </para>
        /// </remarks>
        private async ValueTask<ReadResult> ReadAsync(PipeReader reader, CancellationToken cancellationToken)
        {
            try
            {
                return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (_deadline?.IsCancellationRequested == true)
            {
                throw new ConnectException(
                    ConnectCode.DeadlineExceeded, $"The call to '{_method}' exceeded its deadline.", innerException: ex);
            }
        }

        private TResponse ReadMessage(byte flags, in ReadOnlySequence<byte> payload)
        {
            RejectUnsupportedFlags(flags);
            var body = Decompress(flags, payload);
            try
            {
                return _codec.Read(body, _serializer);
            }
            catch (Exception ex) when (ex is not ConnectException)
            {
                throw new ConnectException(
                    ConnectCode.Internal,
                    $"A message in the response stream for '{_method}' could not be read as '{_codec.Name}': {ex.Message}",
                    innerException: ex);
            }
        }

        private void ReadTerminator(byte flags, in ReadOnlySequence<byte> source)
        {
            // the end-of-stream envelope is an envelope: it carries the compressed flag like any other
            var payload = Decompress(flags, source);

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

                // The terminator carries the error AND the trailing metadata, and a caller that catches
                // the error never sees this stream object - a gRPC caller reads them off
                // RpcException.Trailers. So the metadata has to travel ON the exception, or it is simply
                // lost for every failed streaming call.
                if (_failure is { } failure)
                {
                    _failure = new ConnectException(
                        failure.Code, failure.RawMessage, failure.HttpStatus, failure.Details,
                        failure.CodeWasInferred, failure.InnerException)
                    { Headers = Headers, Trailers = trailers };
                }
            }
            finally
            {
                if (rented is not null) ArrayPool<byte>.Shared.Return(rented);
            }
        }

        /// <summary>
        /// Decompresses a payload where the envelope says it is compressed.
        /// </summary>
        /// <remarks>
        /// Per <em>message</em>, not per stream: the flag is on each envelope precisely so that a sender
        /// may leave a small message uncompressed, so a reader must take the flag's word for it rather
        /// than the negotiation's.
        /// </remarks>
        private ReadOnlySequence<byte> Decompress(byte flags, in ReadOnlySequence<byte> payload)
        {
            if ((flags & ConnectEnvelope.FlagCompressed) == 0) return payload;

            if (_compression is null || ConnectCompression.IsIdentity(_compression.Name))
            {
                throw new ConnectException(
                    ConnectCode.Internal,
                    $"A response message for '{_method}' is flagged compressed, but no compression was negotiated.");
            }

            try
            {
                return new ReadOnlySequence<byte>(_compression.Decompress(payload));
            }
            catch (Exception ex) when (ex is not ConnectException)
            {
                throw new ConnectException(
                    ConnectCode.Internal,
                    $"A message in the response stream for '{_method}' could not be decompressed as '{_compression.Name}': {ex.Message}",
                    innerException: ex);
            }
        }

        private void RejectUnsupportedFlags(byte flags)
        {
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
