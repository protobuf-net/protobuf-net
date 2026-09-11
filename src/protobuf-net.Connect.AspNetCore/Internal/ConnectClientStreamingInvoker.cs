using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProtoBuf.Connect.AspNetCore.Internal
{
    internal static class ConnectClientStreamingInvoker
    {
        public static ConnectInvoker<TService> Create<TService, TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            ConnectClientStreamingHandler<TService, TRequest, TResponse> handler)
            where TService : class
            => new ConnectClientStreamingInvoker<TService, TRequest, TResponse>(method, handler);
    }

    /// <summary>
    /// Serves one client-streaming method: a sequence of enveloped requests in, one enveloped response
    /// out, then the terminating message.
    /// </summary>
    /// <remarks>
    /// The response side is a server-streaming response that happens to carry one message, so the error
    /// handling is the same: a failure before the first write is a non-200, and one after it travels in
    /// the terminator. Here the handler runs to completion before anything is written, so in practice
    /// almost every failure is reportable the ordinary way - but the terminator still has to be written,
    /// because that is what says the stream ended cleanly.
    /// </remarks>
    internal sealed class ConnectClientStreamingInvoker<TService, TRequest, TResponse> : ConnectInvoker<TService>
        where TService : class
    {
        private readonly ConnectMethod<TRequest, TResponse> _method;
        private readonly ConnectClientStreamingHandler<TService, TRequest, TResponse> _handler;

        public ConnectClientStreamingInvoker(
            ConnectMethod<TRequest, TResponse> method,
            ConnectClientStreamingHandler<TService, TRequest, TResponse> handler)
        {
            _method = method;
            _handler = handler;
        }

        public override async Task InvokeAsync(
            HttpContext http, TService service, ConnectCodec codec, ConnectServerCallContext context)
        {
            var requests = EnvelopedRequestReader.ReadAllAsync(
                http.Request.BodyReader, codec, _method.RequestSerializer, _method.ToString(), context.CancellationToken);

            // the handler consumes the request stream; nothing is written until it returns, so a failure
            // here still reaches the endpoint's ordinary error path as a non-200
            var response = await _handler(service, requests, context).ConfigureAwait(false);
            if (context.GetReportedFailure() is { } reported) throw reported;

            var http2 = http.Response;
            http2.StatusCode = StatusCodes.Status200OK;
            http2.ContentType = codec.ContentTypeFor(ConnectMethodType.ClientStreaming);

            var length = codec.Measure(response)
                ?? throw new ConnectException(
                    ConnectCode.Internal, $"The '{codec.Name}' codec cannot measure a message, which framing requires.");

            ConnectEnvelope.WriteHeader(http2.BodyWriter, flags: 0, checked((int)length));
            codec.Write(http2.BodyWriter, response, _method.ResponseSerializer);

            await EndStreamWriter
                .WriteAsync(http2.BodyWriter, error: null, context.ResponseTrailers, context.CancellationToken)
                .ConfigureAwait(false);
        }
    }
}
