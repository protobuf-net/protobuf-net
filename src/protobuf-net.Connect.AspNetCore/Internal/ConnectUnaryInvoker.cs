using System;
using System.Buffers;
using System.IO.Pipelines;
using ProtoBuf.Connect.Internal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProtoBuf.Connect.AspNetCore.Internal
{
    /// <summary>
    /// Serves one method. The runtime owns reading and writing; the generated code supplies only a typed
    /// delegate that calls the service.
    /// </summary>
    internal abstract class ConnectInvoker<TImplementation> where TImplementation : class
    {
        public abstract Task InvokeAsync(HttpContext http, TImplementation service, ConnectCodec codec, ConnectServerCallContext context);
    }

    internal static class ConnectUnaryInvoker
    {
        public static ConnectInvoker<TImplementation> Create<TImplementation, TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            ConnectUnaryHandler<TImplementation, TRequest, TResponse> handler)
            where TImplementation : class
            => new ConnectUnaryInvoker<TImplementation, TRequest, TResponse>(method, handler);
    }

    internal sealed class ConnectUnaryInvoker<TImplementation, TRequest, TResponse> : ConnectInvoker<TImplementation>
        where TImplementation : class
    {
        private readonly ConnectMethod<TRequest, TResponse> _method;
        private readonly ConnectUnaryHandler<TImplementation, TRequest, TResponse> _handler;

        public ConnectUnaryInvoker(ConnectMethod<TRequest, TResponse> method, ConnectUnaryHandler<TImplementation, TRequest, TResponse> handler)
        {
            _method = method;
            _handler = handler;
        }

        public override async Task InvokeAsync(HttpContext http, TImplementation service, ConnectCodec codec, ConnectServerCallContext context)
        {
            var request = await ReadAsync(http.Request.BodyReader, codec, context).ConfigureAwait(false);
            var response = await _handler(service, request, context).ConfigureAwait(false);

            // a handler may report failure by setting ServerCallContext.Status rather than by throwing,
            // which is ordinary gRPC practice and therefore ordinary practice in a shared contract
            if (context.GetReportedFailure() is { } reported) throw reported;

            // before the body: the response commits on first write, and for a unary call trailing metadata
            // is a `trailer-` prefixed header - which is how the protocol avoids HTTP trailers, and so HTTP/2
            context.FlushTrailers();

            await WriteAsync(http.Response, codec, response, _method.ResponseCodec, context).ConfigureAwait(false);
        }

        private async Task<TRequest> ReadAsync(PipeReader reader, ConnectCodec codec, ConnectServerCallContext context)
        {
            var cancellationToken = context.CancellationToken;
            // A unary body is the bare message with no framing, so there is nothing to parse incrementally:
            // read until the client is done, then decode once. A streaming shape reads envelope by envelope
            // from this same reader, which is why the reader rather than a Stream is the primitive.
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                if (result.IsCanceled) throw new ConnectException(ConnectCode.Cancelled, "The request was cancelled.");

                if (result.IsCompleted)
                {
                    var buffer = result.Buffer;
                    try
                    {
                        var compression = context.RequestCompression;
                        if (!ConnectCompression.IsIdentity(compression.Name))
                        {
                            return Decode(codec, new ReadOnlySequence<byte>(compression.Decompress(buffer)));
                        }

                        return Decode(codec, buffer);
                    }
                    finally
                    {
                        reader.AdvanceTo(buffer.End);
                    }
                }

                // consumed nothing, examined everything: ask for more
                reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
            }
        }

        private TRequest Decode(ConnectCodec codec, in ReadOnlySequence<byte> buffer)
        {
            try
            {
                return codec.Read(buffer, _method.RequestCodec);
            }
            catch (Exception ex) when (ex is not ConnectException)
            {
                // the caller sent something we cannot read: that is invalid_argument, and saying which
                // method and codec were involved is the useful half of the message
                throw new ConnectException(
                    ConnectCode.InvalidArgument,
                    $"The request to '{_method}' could not be read as '{codec.Name}': {ex.Message}",
                    innerException: ex);
            }
        }

        private static async Task WriteAsync(HttpResponse response, ConnectCodec codec, TResponse value,
            global::ProtoBuf.Connect.IConnectMessageCodec<TResponse>? serializer, ConnectServerCallContext context)
        {
            // headers first: the response is committed on the first write, so anything the handler added -
            // including trailing metadata, which for unary is a `trailer-` prefixed header - is already set
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = codec.ContentTypeFor(ConnectMethodType.Unary);

            var compression = context.ResponseCompression;
            if (ConnectCompression.IsIdentity(compression.Name))
            {
                context.ApplyResponseEncoding();

                // measured with the same codec that writes, or not stated at all - a marshaller cannot measure
                if (codec.Measure(value, serializer) is { } length) response.ContentLength = length;

                codec.Write(response.BodyWriter, value, serializer);
                await response.BodyWriter.FlushAsync(context.CancellationToken).ConfigureAwait(false);
                return;
            }

            // Compressing means encoding first: the Content-Length is the length of the COMPRESSED body,
            // which nothing can predict, so the measure pass buys nothing here.
            using var scratch = new PooledBufferWriter();
            codec.Write(scratch, value, serializer);

            // ...and below the threshold it goes out plain, in which case the encoding must NOT be
            // announced - a stated content-encoding that does not describe the body is worse than none
            if (scratch.WrittenCount < compression.MinimumSize)
            {
                context.ResponseCompression = ConnectCompression.Identity;
                response.ContentLength = scratch.WrittenCount;
                await response.BodyWriter.WriteAsync(scratch.WrittenMemory, context.CancellationToken).ConfigureAwait(false);
                return;
            }

            var compressed = compression.Compress(new ReadOnlySequence<byte>(scratch.WrittenMemory));
            context.ApplyResponseEncoding();
            response.ContentLength = compressed.Length;
            await response.BodyWriter.WriteAsync(compressed, context.CancellationToken).ConfigureAwait(false);
        }
    }
}
