using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ProtoBuf.Connect.Internal;

/// <summary>
/// A unary request body: the bare message, with <c>Content-Length</c> known in advance.
/// </summary>
/// <remarks>
/// This is one member of an intended family rather than "the request content". A unary body can state
/// its length because protobuf-net can measure a value before writing it; a client-streaming or duplex
/// body cannot, and will want a sibling that writes incrementally and lets the length be unknown. The
/// abstraction that makes that possible is simply that the request body is an <see cref="HttpContent"/>.
/// <para>
/// The payload is serialized eagerly into a pooled buffer rather than written on demand, so that the
/// content is re-sendable - a retrying <see cref="DelegatingHandler"/> may serialize it more than once,
/// and a measured write consumes its measurement.
/// </para>
/// </remarks>
internal sealed class MeasuredCodecContent<T> : HttpContent
{
    private byte[]? _buffer;
    private readonly int _length;

    public MeasuredCodecContent(ConnectCodec codec, T value, string contentType)
    {
        var measured = codec.Measure(value);
        if (measured is { } length && length <= int.MaxValue)
        {
            _length = (int)length;
            _buffer = ArrayPool<byte>.Shared.Rent(_length);
            using var ms = new MemoryStream(_buffer, 0, _length, writable: true);
            codec.Write(ms, value);
        }
        else
        {
            using var ms = new MemoryStream();
            codec.Write(ms, value);
            _buffer = ms.ToArray();
            _length = _buffer.Length;
        }

        Headers.ContentType = new MediaTypeHeaderValue(contentType);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _length;
        return true;
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => SerializeToStreamAsync(stream, context, CancellationToken.None);

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        var buffer = _buffer ?? throw new ObjectDisposedException(nameof(MeasuredCodecContent<T>));
        await stream.WriteAsync(buffer.AsMemory(0, _length), cancellationToken).ConfigureAwait(false);
    }

    protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        var buffer = _buffer ?? throw new ObjectDisposedException(nameof(MeasuredCodecContent<T>));
        stream.Write(buffer, 0, _length);
    }

    protected override void Dispose(bool disposing)
    {
        var buffer = Interlocked.Exchange(ref _buffer, null);
        if (buffer is not null) ArrayPool<byte>.Shared.Return(buffer);
        base.Dispose(disposing);
    }
}
