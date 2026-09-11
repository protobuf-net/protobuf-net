using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProtoBuf.Connect.AspNetCore.Internal
{
    internal static class ConnectDuplexInvoker
    {
        public static ConnectInvoker<TImplementation> Create<TImplementation, TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            ConnectDuplexHandler<TImplementation, TRequest, TResponse> handler)
            where TImplementation : class
            => new ConnectDuplexInvoker<TImplementation, TRequest, TResponse>(method, handler);
    }

    /// <summary>
    /// Serves one bidirectional-streaming method.
    /// </summary>
    /// <remarks>
    /// Structurally the server-streaming invoker with a sequence in place of a single request, and that
    /// is the whole difference: the framing, the terminator, the trailers and the error handling are all
    /// shared. Nothing about duplex is new to the *protocol* - it is new only to the transport, which
    /// must interleave, and therefore must be HTTP/2.
    /// </remarks>
    internal sealed class ConnectDuplexInvoker<TImplementation, TRequest, TResponse> : ConnectInvoker<TImplementation>
        where TImplementation : class
    {
        private readonly ConnectMethod<TRequest, TResponse> _method;
        private readonly ConnectDuplexHandler<TImplementation, TRequest, TResponse> _handler;

        public ConnectDuplexInvoker(
            ConnectMethod<TRequest, TResponse> method,
            ConnectDuplexHandler<TImplementation, TRequest, TResponse> handler)
        {
            _method = method;
            _handler = handler;
        }

        public override async Task InvokeAsync(
            HttpContext http, TImplementation service, ConnectCodec codec, ConnectServerCallContext context)
        {
            if (!HttpProtocol.IsHttp2(http.Request.Protocol) && !HttpProtocol.IsHttp3(http.Request.Protocol))
            {
                // failing here beats deadlocking: over HTTP/1.1 the client cannot read a response until
                // it has finished sending, and the service is waiting for messages that will not come
                throw new ConnectException(
                    ConnectCode.Unimplemented,
                    $"'{_method}' is bidirectional and needs HTTP/2; this request arrived over {http.Request.Protocol}.",
                    StatusCodes.Status505HttpVersionNotsupported);
            }

            var requests = EnvelopedRequestReader.ReadAllAsync(
                http.Request.BodyReader, codec, _method.RequestSerializer, _method.ToString(), context.CancellationToken);

            var response = http.Response;
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = codec.ContentTypeFor(ConnectMethodType.DuplexStreaming);

            ConnectException? failure = null;
            try
            {
                await foreach (var message in _handler(service, requests, context)
                    .WithCancellation(context.CancellationToken).ConfigureAwait(false))
                {
                    var length = codec.Measure(message)
                        ?? throw new ConnectException(
                            ConnectCode.Internal, $"The '{codec.Name}' codec cannot measure a message, which framing requires.");

                    ConnectEnvelope.WriteHeader(response.BodyWriter, flags: 0, checked((int)length));
                    codec.Write(response.BodyWriter, message, _method.ResponseSerializer);
                    await response.BodyWriter.FlushAsync(context.CancellationToken).ConfigureAwait(false);
                }

                failure = context.GetReportedFailure();
            }
            catch (ConnectException ex)
            {
                failure = ex;
            }
            catch (OperationCanceledException) when (http.RequestAborted.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                failure = new ConnectException(ConnectCode.Internal, null, innerException: ex);
            }

            await EndStreamWriter
                .WriteAsync(response.BodyWriter, failure, context.ResponseTrailers, context.CancellationToken)
                .ConfigureAwait(false);
        }
    }
}
