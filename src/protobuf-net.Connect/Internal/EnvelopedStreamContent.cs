using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf.Serializers;

namespace ProtoBuf.Connect.Internal
{
    /// <summary>
    /// A request body carrying a sequence of enveloped messages, written as they are produced.
    /// </summary>
    /// <remarks>
    /// The third member of the family <see cref="MeasuredCodecContent{T}"/> predicted, and the one that
    /// tests the prediction: it **cannot** state a <c>Content-Length</c>, because the messages do not
    /// exist yet when the headers go out. <see cref="TryComputeLength"/> returning <c>false</c> is what
    /// makes the request chunked.
    /// <para>
    /// Unlike its siblings this content is <b>not re-sendable</b>: it is backed by an
    /// <see cref="IAsyncEnumerable{T}"/>, which a caller may not be able to replay. A retrying
    /// <see cref="DelegatingHandler"/> in front of a client-streaming call will therefore fail on the
    /// second attempt rather than silently sending a partial body - which is the better of the two.
    /// </para>
    /// </remarks>
    internal sealed class EnvelopedStreamContent<T> : HttpContent
    {
        private readonly ConnectCodec _codec;
        private readonly IAsyncEnumerable<T> _messages;
        private readonly IConnectMessageCodec<T>? _serializer;
        private readonly CancellationToken _cancellationToken;
        private int _sent;

        public EnvelopedStreamContent(
            ConnectCodec codec,
            IAsyncEnumerable<T> messages,
            string contentType,
            IConnectMessageCodec<T>? serializer,
            CancellationToken cancellationToken)
        {
            _codec = codec;
            _messages = messages;
            _serializer = serializer;
            _cancellationToken = cancellationToken;
            Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }

        /// <summary>Always <c>false</c>: the body is produced as it is sent.</summary>
        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref _sent, 1) != 0)
            {
                throw new InvalidOperationException(
                    "A client-streaming request body cannot be sent twice; its messages come from a sequence that may not be replayable.");
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationToken);
            var token = linked.Token;
            var writer = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));

            await foreach (var message in _messages.WithCancellation(token).ConfigureAwait(false))
            {
                ConnectEnvelope.WriteMessage(writer, _codec, message, _serializer);

                // flush per message: the server is entitled to act on each as it arrives
                await writer.FlushAsync(token).ConfigureAwait(false);
            }

            // the REQUEST stream has no terminating message - only responses do - so the end of the
            // body is the end of the sequence, and nothing else needs saying
            await writer.CompleteAsync().ConfigureAwait(false);
        }
    }
}
