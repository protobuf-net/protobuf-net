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
        public static ConnectInvoker<TService> Create<TService, TRequest, TResponse>(
            ConnectMethod<TRequest, TResponse> method,
            ConnectServerStreamingHandler<TService, TRequest, TResponse> handler)
            where TService : class
            => new ConnectServerStreamingInvoker<TService, TRequest, TResponse>(method, handler);
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
    internal sealed class ConnectServerStreamingInvoker<TService, TRequest, TResponse> : ConnectInvoker<TService>
        where TService : class
    {
        private readonly ConnectMethod<TRequest, TResponse> _method;
        private readonly ConnectServerStreamingHandler<TService, TRequest, TResponse> _handler;

        public ConnectServerStreamingInvoker(
            ConnectMethod<TRequest, TResponse> method,
            ConnectServerStreamingHandler<TService, TRequest, TResponse> handler)
        {
            _method = method;
            _handler = handler;
        }

        public override async Task InvokeAsync(
            HttpContext http, TService service, ConnectCodec codec, ConnectServerCallContext context)
        {
            // read BEFORE committing the status: a request we cannot read never starts a stream, and so
            // is reportable the ordinary way, as a non-200 with a JSON error
            var request = await ReadRequestAsync(http.Request.BodyReader, codec, context.CancellationToken)
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
            var length = codec.Measure(message)
                ?? throw new ConnectException(
                    ConnectCode.Internal, $"The '{codec.Name}' codec cannot measure a message, which framing requires.");

            ConnectEnvelope.WriteHeader(writer, flags: 0, checked((int)length));
            codec.Write(writer, message, _method.ResponseSerializer);
        }

        private async Task<TRequest> ReadRequestAsync(PipeReader reader, ConnectCodec codec, CancellationToken cancellationToken)
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var buffer = result.Buffer;

                if (ConnectEnvelope.TryRead(ref buffer, out var flags, out var payload))
                {
                    try
                    {
                        return Decode(codec, flags, payload);
                    }
                    finally
                    {
                        // everything after the single request message is ignored; a server-streaming
                        // call declares one, and reading further would be inventing cardinality
                        reader.AdvanceTo(buffer.Start, buffer.End);
                    }
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    throw new ConnectException(
                        ConnectCode.InvalidArgument,
                        $"The request to '{_method}' ended before a complete enveloped message arrived.");
                }
            }
        }

        private TRequest Decode(ConnectCodec codec, byte flags, in ReadOnlySequence<byte> payload)
        {
            if ((flags & ConnectEnvelope.FlagCompressed) != 0)
            {
                throw new ConnectException(
                    ConnectCode.Unimplemented, "Compressed request messages are not implemented yet.");
            }

            if ((flags & (ConnectEnvelope.FlagEndOfStream | ConnectEnvelope.FlagReserved)) != 0)
            {
                throw new ConnectException(
                    ConnectCode.InvalidArgument, $"The request's enveloped message set unexpected flags (0x{flags:x2}).");
            }

            try
            {
                return codec.Read(payload, _method.RequestSerializer);
            }
            catch (Exception ex) when (ex is not ConnectException)
            {
                throw new ConnectException(
                    ConnectCode.InvalidArgument,
                    $"The request to '{_method}' could not be read as '{codec.Name}': {ex.Message}",
                    innerException: ex);
            }
        }
    }
}
