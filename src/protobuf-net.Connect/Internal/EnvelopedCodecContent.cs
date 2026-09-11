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
    /// A request body carrying exactly one enveloped message: the server-streaming shape.
    /// </summary>
    /// <remarks>
    /// The sibling <see cref="MeasuredCodecContent{T}"/> predicted - same family, different framing, and
    /// still able to state <c>Content-Length</c> because there is only one message and protobuf-net can
    /// measure it. Client-streaming and duplex want a third member that writes incrementally and lets
    /// the length be unknown; nothing here blocks that.
    /// </remarks>
    internal sealed class EnvelopedCodecContent<T> : HttpContent
    {
        private PooledBufferWriter? _payload;

        public EnvelopedCodecContent(ConnectCodec codec, T value, string contentType, ISerializer<T>? serializer = null)
        {
            var hint = codec.Measure(value) is { } length && length <= int.MaxValue ? (int)length : 256;
            var payload = new PooledBufferWriter(hint + ConnectEnvelope.HeaderLength);
            try
            {
                // the header states the payload length, so the payload is written first and the header
                // patched in - except protobuf-net can measure, so there is nothing to patch
                var measured = codec.Measure(value);
                if (measured is null)
                {
                    throw new NotSupportedException(
                        $"The '{codec.Name}' codec cannot measure a message, which an enveloped request requires.");
                }

                ConnectEnvelope.WriteHeader(payload, flags: 0, checked((int)measured.Value));
                codec.Write(payload, value, serializer);
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
            => _payload ?? throw new ObjectDisposedException(nameof(EnvelopedCodecContent<T>));

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
