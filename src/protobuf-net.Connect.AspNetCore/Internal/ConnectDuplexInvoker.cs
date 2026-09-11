using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

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
    /// shared. Nothing about duplex is new to the *protocol* - it is new only to the transport.
    /// <para>
    /// <b>This does not require HTTP/2, and must not demand it.</b> Only <em>full</em> duplex needs
    /// interleaving; a half-duplex bidi call - every request sent, then every response read - is
    /// ordinary over HTTP/1.1, is explicitly permitted, and is exercised by the conformance suite (which
    /// has a <c>supports_half_duplex_bidi_over_http1</c> feature flag precisely because it is a real
    /// distinction). This invoker used to refuse anything below HTTP/2 and failed those cases with 505.
    /// </para>
    /// <para>
    /// The check belongs on the <em>client</em>, and is still there: a caller about to interleave knows
    /// it is going to, and <c>ConnectChannel.DuplexAsync</c> pins HTTP/2 exactly so that such a call
    /// fails cleanly instead of deadlocking. The server has no way to tell the two apart - the request
    /// looks identical - so refusing here can only ever be a guess, and it was guessing wrong.
    /// </para>
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
            // Kestrel enforces a MINIMUM REQUEST BODY DATA RATE, and a full-duplex call violates it as a
            // matter of course: the caller holds the request stream open while it waits for responses, so
            // there are legitimately long gaps with no request bytes. Kestrel's answer is to abort the
            // body - "Reading the request body timed out due to data arriving too slowly" - which faults
            // the HTTP/2 connection and reaches the caller as a GOAWAY with INTERNAL_ERROR, losing the
            // status the service was in the middle of reporting.
            //
            // Relaxed per REQUEST rather than per server, so the protection stays on for every other
            // endpoint; this is the same accommodation long-running streaming needs generally, and is
            // exactly what the rate limit was not designed for.
            if (http.Features.Get<IHttpMinRequestBodyDataRateFeature>() is { } dataRate)
            {
                dataRate.MinDataRate = null;
            }

            var requests = EnvelopedRequestReader.ReadAllAsync(
                http.Request.BodyReader, codec, _method.RequestCodec, _method.ToString(), context.CancellationToken);

            var response = http.Response;
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = codec.ContentTypeFor(ConnectMethodType.DuplexStreaming);

            ConnectException? failure = null;
            try
            {
                await foreach (var message in _handler(service, requests, context)
                    .WithCancellation(context.CancellationToken).ConfigureAwait(false))
                {
                    ConnectEnvelope.WriteMessage(response.BodyWriter, codec, message, _method.ResponseCodec);
                    await response.BodyWriter.FlushAsync(context.CancellationToken).ConfigureAwait(false);
                }

                failure = context.GetReportedFailure();
            }
            catch (Grpc.Core.RpcException ex)
            {
                // a deliberate status from the service, not a fault; without this it would fall to
                // the catch-all below and be reported as `internal`
                failure = ConnectException.FromRpcException(ex);
            }
            catch (ConnectException ex)
            {
                failure = ex;
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
