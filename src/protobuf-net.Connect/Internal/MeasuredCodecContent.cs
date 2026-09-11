using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf.Serializers;

namespace ProtoBuf.Connect.Internal
{
    /// <summary>
    /// A unary request body: the bare message, with <c>Content-Length</c> known in advance.
    /// </summary>
    /// <remarks>
    /// This is one member of an intended family rather than "the request content". A unary body can state
    /// its length because protobuf-net can measure a value before writing it; a client-streaming or duplex
    /// body cannot, and will want a sibling that writes incrementally and lets the length be unknown. The
    /// abstraction that makes that possible is simply that the request body is an <see cref="HttpContent"/>.
    /// <para>
    /// The payload is serialized eagerly rather than written on demand, so that the content is re-sendable:
    /// a retrying <see cref="DelegatingHandler"/> may serialize it more than once.
    /// </para>
    /// </remarks>
    internal sealed class MeasuredCodecContent<T> : HttpContent
    {
        private PooledBufferWriter? _payload;

        public MeasuredCodecContent(ConnectCodec codec, T value, string contentType, IConnectMessageCodec<T>? over = null)
        {
            // Measure first where the codec can: it sizes the buffer exactly, and it is the same call the
            // server uses to set Content-Length. A codec that cannot measure simply grows the writer.
            var hint = codec.Measure(value, over) is { } length && length <= int.MaxValue ? (int)length : 256;
            var payload = new PooledBufferWriter(hint);
            try
            {
                codec.Write(payload, value, over);
            }
            catch
            {
                payload.Dispose();
                throw;
            }

            _payload = payload;
            Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }

        private PooledBufferWriter Payload
            => _payload ?? throw new ObjectDisposedException(nameof(MeasuredCodecContent<T>));

        protected override bool TryComputeLength(out long length)
        {
            length = Payload.WrittenCount;
            return true;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            => await stream.WriteAsync(Payload.WrittenMemory, cancellationToken).ConfigureAwait(false);

        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            => stream.Write(Payload.WrittenMemory.Span);

        protected override void Dispose(bool disposing)
        {
            Interlocked.Exchange(ref _payload, null)?.Dispose();
            base.Dispose(disposing);
        }
    }
}
