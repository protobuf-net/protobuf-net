using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProtoBuf.Connect.AspNetCore.Internal
{
    internal static class ConnectServerStreamingInvoker
    {
        public static ConnectInvoker<TImplementation> Create<TImplementation, TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            ConnectServerStreamingHandler<TImplementation, TRequest, TResponse> handler)
            where TImplementation : class
            => new ConnectServerStreamingInvoker<TImplementation, TRequest, TResponse>(method, handler);
    }

    /// <summary>
    /// Serves one server-streaming method: one enveloped request in, a sequence of enveloped responses
    /// out, then the terminating message.
    /// </summary>
    /// <remarks>
    /// The error handling is the interesting part, and it is where §14.1's "one error path" constraint
    /// is actually tested. Once the first message has gone out the status is committed to <c>200</c>, so
    /// a later failure cannot be reported as a failed response - it becomes a successful response whose
    /// terminating message describes a failed call. This invoker therefore catches its own failures
    /// rather than letting them reach the endpoint's error path, which would have nowhere to put them.
    /// </remarks>
    internal sealed class ConnectServerStreamingInvoker<TImplementation, TRequest, TResponse> : ConnectInvoker<TImplementation>
        where TImplementation : class
    {
        private readonly ConnectMethod<TRequest, TResponse> _method;
        private readonly ConnectServerStreamingHandler<TImplementation, TRequest, TResponse> _handler;

        public ConnectServerStreamingInvoker(
            ConnectMethod<TRequest, TResponse> method,
            ConnectServerStreamingHandler<TImplementation, TRequest, TResponse> handler)
        {
            _method = method;
            _handler = handler;
        }

        public override async Task InvokeAsync(
            HttpContext http, TImplementation service, ConnectCodec codec, ConnectServerCallContext context)
        {
            // read BEFORE committing the status: a request we cannot read never starts a stream, and so
            // is reportable the ordinary way, as a non-200 with a JSON error
            var request = await EnvelopedRequestReader
                .ReadOneAsync(http.Request.BodyReader, codec, _method.RequestCodec, _method.ToString(), context.CancellationToken)
                .ConfigureAwait(false);

            var response = http.Response;
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = codec.ContentTypeFor(ConnectMethodType.ServerStreaming);
            // no Content-Length: a stream cannot state one, which is the sibling case MeasuredCodecContent
            // exists to contrast with

            ConnectException? failure = null;
            try
            {
                await foreach (var message in _handler(service, request, context)
                    .WithCancellation(context.CancellationToken).ConfigureAwait(false))
                {
                    WriteMessage(response.BodyWriter, codec, message);
                    // flush per message: a stream the client cannot see until the end is not a stream
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
                // the client went away; there is nobody to send a terminator to
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

        private void WriteMessage(PipeWriter writer, ConnectCodec codec, TResponse message)
        {
            ConnectEnvelope.WriteMessage(writer, codec, message, _method.ResponseCodec);
        }

    }
}
