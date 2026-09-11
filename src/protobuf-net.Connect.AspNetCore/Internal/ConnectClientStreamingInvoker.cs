using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProtoBuf.Connect.AspNetCore.Internal
{
    internal static class ConnectClientStreamingInvoker
    {
        public static ConnectInvoker<TImplementation> Create<TImplementation, TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            ConnectClientStreamingHandler<TImplementation, TRequest, TResponse> handler)
            where TImplementation : class
            => new ConnectClientStreamingInvoker<TImplementation, TRequest, TResponse>(method, handler);
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
    internal sealed class ConnectClientStreamingInvoker<TImplementation, TRequest, TResponse> : ConnectInvoker<TImplementation>
        where TImplementation : class
    {
        private readonly ConnectMethod<TRequest, TResponse> _method;
        private readonly ConnectClientStreamingHandler<TImplementation, TRequest, TResponse> _handler;

        public ConnectClientStreamingInvoker(
            ConnectMethod<TRequest, TResponse> method,
            ConnectClientStreamingHandler<TImplementation, TRequest, TResponse> handler)
        {
            _method = method;
            _handler = handler;
        }

        public override async Task InvokeAsync(
            HttpContext http, TImplementation service, ConnectCodec codec, ConnectServerCallContext context)
        {
            var requests = EnvelopedRequestReader.ReadAllAsync(
                http.Request.BodyReader, codec, _method.RequestCodec, _method.ToString(), context.RequestCompression, context.CancellationToken);

            // Status and content-type are set BEFORE the handler runs, and that ordering is load-bearing
            // rather than tidy: a handler may send leading metadata, which commits the response, after
            // which neither can be assigned - "StatusCode cannot be set because the response has already
            // started", thrown after the response was under way and reaching the client as a reset
            // connection. Setting them early is free, because assigning them does not itself commit; and
            // there is nothing to decide later, since a streaming call answers 200 whatever happens.
            var response = http.Response;
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = codec.ContentTypeFor(ConnectMethodType.ClientStreaming);
            context.ApplyResponseEncoding();

            // A STREAMING call answers 200 whatever happens, and reports failure in its terminating
            // envelope - even when it fails before producing anything. It is tempting to let the
            // exception reach the endpoint's error path, since nothing has been written yet and that path
            // would produce a tidy non-200; the protocol says otherwise, and the conformance suite is
            // explicit about it ("error-returns-success-http-code"). A client reading a streaming
            // response is looking for envelopes, and a bare JSON body is not one.
            ConnectException? failure = null;
            TResponse? reply = default;
            try
            {
                reply = await _handler(service, requests, context).ConfigureAwait(false);
                failure = context.GetReportedFailure();
            }
            catch (ConnectException ex)
            {
                failure = ex;
            }
            catch (Grpc.Core.RpcException ex)
            {
                failure = ConnectException.FromRpcException(ex);
            }
            catch (OperationCanceledException) when (http.RequestAborted.IsCancellationRequested)
            {
                return;
            }
            catch (OperationCanceledException ex)
            {
                // ...whereas this one is our own connect-timeout-ms firing, which the caller asked for
                failure = new ConnectException(
                    ConnectCode.DeadlineExceeded, "The call exceeded its deadline.", innerException: ex);
            }
            catch (Exception ex)
            {
                failure = new ConnectException(ConnectCode.Internal, null, innerException: ex);
            }

            // the message only if the call succeeded; a failed one carries no payload at all
            if (failure is null)
            {
                ConnectEnvelope.WriteMessage(response.BodyWriter, codec, reply!, _method.ResponseCodec, compression: context.ResponseCompression);
            }

            // http.RequestAborted, NOT the call's token. The call's token carries the deadline, so on a
            // timeout it is already cancelled - and the terminating envelope is precisely how the
            // deadline gets reported. Writing it with that token throws, the endpoint's catch writes a
            // SECOND terminator, and the caller sees "corrupt response: N extra bytes after end of
            // stream" instead of deadline_exceeded. RequestAborted fires only when the client has gone,
            // at which point there is nobody left to tell.
            await EndStreamWriter
                .WriteAsync(response.BodyWriter, failure, context.ResponseTrailers, http.RequestAborted)
                .ConfigureAwait(false);
        }
    }
}
