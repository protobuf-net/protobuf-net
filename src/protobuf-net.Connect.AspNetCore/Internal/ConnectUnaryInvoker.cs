using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProtoBuf.Connect.AspNetCore.Internal;

/// <summary>
/// Serves one method. The runtime owns reading and writing; the generated code supplies only a typed
/// delegate that calls the service.
/// </summary>
internal abstract class ConnectInvoker<TService> where TService : class
{
    public abstract Task InvokeAsync(HttpContext http, TService service, ConnectCodec codec, ConnectServerCallContext context);
}

internal static class ConnectUnaryInvoker
{
    public static ConnectInvoker<TService> Create<TService, TRequest, TResponse>(
        ConnectMethod<TRequest, TResponse> method,
        ConnectUnaryHandler<TService, TRequest, TResponse> handler)
        where TService : class
        => new ConnectUnaryInvoker<TService, TRequest, TResponse>(method, handler);
}

internal sealed class ConnectUnaryInvoker<TService, TRequest, TResponse> : ConnectInvoker<TService>
    where TService : class
{
    private readonly ConnectMethod<TRequest, TResponse> _method;
    private readonly ConnectUnaryHandler<TService, TRequest, TResponse> _handler;

    public ConnectUnaryInvoker(ConnectMethod<TRequest, TResponse> method, ConnectUnaryHandler<TService, TRequest, TResponse> handler)
    {
        _method = method;
        _handler = handler;
    }

    public override async Task InvokeAsync(HttpContext http, TService service, ConnectCodec codec, ConnectServerCallContext context)
    {
        var request = await ReadAsync(http.Request.BodyReader, codec, context.CancellationToken).ConfigureAwait(false);
        var response = await _handler(service, request, context).ConfigureAwait(false);
        await WriteAsync(http.Response, codec, response, context.CancellationToken).ConfigureAwait(false);
    }

    private async Task<TRequest> ReadAsync(PipeReader reader, ConnectCodec codec, CancellationToken cancellationToken)
    {
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
            return codec.Read<TRequest>(buffer);
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

    private static async Task WriteAsync(HttpResponse response, ConnectCodec codec, TResponse value, CancellationToken cancellationToken)
    {
        // headers first: the response is committed on the first write, so anything the handler added -
        // including trailing metadata, which for unary is a `trailer-` prefixed header - is already set
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = codec.ContentTypeFor(ConnectMethodType.Unary);
        if (codec.Measure(value) is { } length) response.ContentLength = length;

        codec.Write(response.BodyWriter, value);
        await response.BodyWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
