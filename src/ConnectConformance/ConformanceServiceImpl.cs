using Connectrpc.Conformance.V1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace ProtoBuf.ConnectConformance;

/// <summary>
/// The conformance suite's service, implemented against <c>protoc</c>'s generated base.
/// </summary>
/// <remarks>
/// Every RPC is told what to do by a response definition in the request, so there is almost no policy
/// here: the work is echoing back exactly what was observed - headers, timeout, and every request
/// message - so the runner can check the wire against its own expectations.
/// </remarks>
public sealed class ConformanceServiceImpl : ConformanceService.ConformanceServiceBase
{
    public override Task<UnaryResponse> Unary(UnaryRequest request, ServerCallContext context)
        => UnaryCore(request.ResponseDefinition, context, new[] { (IMessage)request },
            data => new UnaryResponse { Payload = data });

    public override Task<IdempotentUnaryResponse> IdempotentUnary(IdempotentUnaryRequest request, ServerCallContext context)
        => UnaryCore(request.ResponseDefinition, context, new[] { (IMessage)request },
            data => new IdempotentUnaryResponse { Payload = data });

    /// <summary>Always fails; the suite checks that an unimplemented RPC reports as one.</summary>
    public override Task<UnimplementedResponse> Unimplemented(UnimplementedRequest request, ServerCallContext context)
        => throw new RpcException(new Status(StatusCode.Unimplemented, "connectrpc.conformance.v1.ConformanceService.Unimplemented is not implemented"));

    public override async Task<ClientStreamResponse> ClientStream(
        IAsyncStreamReader<ClientStreamRequest> requestStream, ServerCallContext context)
    {
        // the definition rides on the FIRST message only, and is to be ignored on the rest
        UnaryResponseDefinition? definition = null;
        var seen = new List<IMessage>();

        while (await requestStream.MoveNext(context.CancellationToken))
        {
            definition ??= requestStream.Current.ResponseDefinition;
            seen.Add(requestStream.Current);
        }

        return await UnaryCore(definition, context, seen, data => new ClientStreamResponse { Payload = data });
    }

    public override async Task ServerStream(
        ServerStreamRequest request, IServerStreamWriter<ServerStreamResponse> responseStream, ServerCallContext context)
    {
        var definition = request.ResponseDefinition;
        await WriteHeadersAsync(definition?.ResponseHeaders, definition?.ResponseTrailers, context);

        var requests = new List<IMessage> { request };
        var sent = 0;

        if (definition is not null)
        {
            foreach (var data in definition.ResponseData)
            {
                await DelayAsync(definition.ResponseDelayMs, context);

                // the request info rides on the FIRST response only; later ones carry payload alone
                var payload = new ConformancePayload { Data = data };
                if (sent++ == 0) payload.RequestInfo = DescribeRequest(context, requests);

                await responseStream.WriteAsync(new ServerStreamResponse { Payload = payload });
            }
        }

        // an error after the messages is the normal case; before any, it is a trailers-only response and
        // must carry the request info in its details instead, since no payload will
        if (definition?.Error is { } error) throw ToRpcException(error, sent == 0 ? DescribeRequest(context, requests) : null);
    }

    public override async Task BidiStream(
        IAsyncStreamReader<BidiStreamRequest> requestStream,
        IServerStreamWriter<BidiStreamResponse> responseStream,
        ServerCallContext context)
    {
        StreamResponseDefinition? definition = null;
        var fullDuplex = false;
        var headersSent = false;
        var sent = 0;

        // in full-duplex the request info covers everything seen SINCE THE LAST RESPONSE, so the buffer
        // is drained each time; in half-duplex it accumulates and is reported once
        var pending = new List<IMessage>();
        var responses = new Queue<ByteString>();

        while (await requestStream.MoveNext(context.CancellationToken))
        {
            var message = requestStream.Current;
            if (!headersSent)
            {
                definition = message.ResponseDefinition;
                fullDuplex = message.FullDuplex;
                if (definition is not null)
                {
                    foreach (var data in definition.ResponseData) responses.Enqueue(data);
                }

                await WriteHeadersAsync(definition?.ResponseHeaders, definition?.ResponseTrailers, context);
                headersSent = true;
            }

            pending.Add(message);

            // In FULL DUPLEX the caller sends a request and then waits for its response before sending
            // more - so a server that keeps reading until the request stream ends waits forever for
            // messages the caller will not send until it hears back. When there is nothing left to send
            // and a failure is defined, that failure IS the response, and it has to go out now rather
            // than after a stream end that never comes.
            if (fullDuplex && responses.Count == 0 && definition?.Error is not null) break;

            // interleaved: one response per request, as they arrive
            if (fullDuplex && responses.Count > 0)
            {
                await DelayAsync(definition?.ResponseDelayMs ?? 0, context);
                await responseStream.WriteAsync(new BidiStreamResponse
                {
                    Payload = Payload(responses.Dequeue(), sent++ == 0 || fullDuplex ? DescribeRequest(context, pending) : null),
                });
                pending.Clear();
            }
        }

        if (!headersSent)
        {
            // an empty request stream still needs the definition's headers, and there was none to read
            await WriteHeadersAsync(null, null, context);
        }

        // half-duplex, or full-duplex with responses left over: send the rest now
        while (responses.Count > 0)
        {
            await DelayAsync(definition?.ResponseDelayMs ?? 0, context);
            await responseStream.WriteAsync(new BidiStreamResponse
            {
                Payload = Payload(responses.Dequeue(), sent++ == 0 ? DescribeRequest(context, pending) : null),
            });
        }

        if (definition?.Error is { } error) throw ToRpcException(error, sent == 0 ? DescribeRequest(context, pending) : null);

        static ConformancePayload Payload(ByteString data, ConformancePayload.Types.RequestInfo? info)
        {
            var payload = new ConformancePayload { Data = data };
            if (info is not null) payload.RequestInfo = info;
            return payload;
        }
    }

    /// <summary>
    /// The single-response shape, shared by unary, idempotent-unary and client-streaming: they differ
    /// only in what they wrap the payload in.
    /// </summary>
    private static async Task<TResponse> UnaryCore<TResponse>(
        UnaryResponseDefinition? definition,
        ServerCallContext context,
        IReadOnlyList<IMessage> requests,
        Func<ConformancePayload, TResponse> wrap)
    {
        await WriteHeadersAsync(definition?.ResponseHeaders, definition?.ResponseTrailers, context);
        await DelayAsync(definition?.ResponseDelayMs ?? 0, context);

        var info = DescribeRequest(context, requests);

        // an error instead of a response: the request info goes into the error's details, since there is
        // no payload to carry it
        if (definition?.Error is { } error) throw ToRpcException(error, info);

        return wrap(new ConformancePayload
        {
            Data = definition?.ResponseData ?? ByteString.Empty,
            RequestInfo = info,
        });
    }

    /// <summary>Echoes back what was observed on the wire.</summary>
    private static ConformancePayload.Types.RequestInfo DescribeRequest(
        ServerCallContext context, IReadOnlyList<IMessage> requests)
    {
        var info = new ConformancePayload.Types.RequestInfo();

        foreach (var entry in context.RequestHeaders)
        {
            // binary metadata is echoed under its own -bin suffix, which the Metadata entry keeps in Key
            info.RequestHeaders.Add(new Header
            {
                Name = entry.Key,
                Value = { entry.IsBinary ? Convert.ToBase64String(entry.ValueBytes) : entry.Value },
            });
        }

        if (context.Deadline != DateTime.MaxValue)
        {
            // reported as the remaining budget, which is the only thing a server can observe; the suite
            // is lenient here deliberately, hence the int64
            info.TimeoutMs = (long)Math.Round((context.Deadline - DateTime.UtcNow).TotalMilliseconds);
        }

        foreach (var request in requests) info.Requests.Add(Any.Pack(request));

        return info;
    }

    /// <summary>
    /// Sends leading metadata, and registers trailing metadata for the end of the call.
    /// </summary>
    /// <remarks>
    /// Trailers are set here rather than at the end deliberately: they must be in place before anything
    /// commits the response, and for a unary call they travel as <c>trailer-</c> prefixed headers.
    /// </remarks>
    private static async Task WriteHeadersAsync(
        IEnumerable<Header>? headers, IEnumerable<Header>? trailers, ServerCallContext context)
    {
        if (trailers is not null)
        {
            foreach (var header in trailers)
            {
                foreach (var value in header.Value) context.ResponseTrailers.Add(header.Name, value);
            }
        }

        if (headers is null) return;

        var metadata = new Metadata();
        foreach (var header in headers)
        {
            foreach (var value in header.Value) metadata.Add(header.Name, value);
        }

        if (metadata.Count != 0) await context.WriteResponseHeadersAsync(metadata);
    }

    private static Task DelayAsync(uint milliseconds, ServerCallContext context)
        => milliseconds == 0 ? Task.CompletedTask : Task.Delay((int)milliseconds, context.CancellationToken);

    /// <summary>
    /// Builds the failure the definition asked for, carrying its details.
    /// </summary>
    /// <remarks>
    /// Details travel the way a gRPC service already sends them: a <c>google.rpc.Status</c> in a
    /// <c>grpc-status-details-bin</c> trailer. That is deliberate rather than convenient - it is the
    /// existing convention, and using it here is what proves a contract-first service keeps its rich
    /// errors when it moves onto Connect, rather than proving only that our own error type round-trips.
    /// </remarks>
    private static RpcException ToRpcException(Error error, ConformancePayload.Types.RequestInfo? info)
    {
        var status = new Status((StatusCode)(int)error.Code, error.Message ?? string.Empty);

        // "Servers should build a RequestInfo and append it to the details of the requested error."
        var details = new List<Any>(error.Details);
        if (info is not null) details.Add(Any.Pack(info));

        var rpc = new Google.Rpc.Status { Code = (int)error.Code, Message = error.Message ?? string.Empty };
        rpc.Details.AddRange(details);

        var trailers = new Metadata { { GrpcStatusDetailsTrailer, rpc.ToByteArray() } };
        return new RpcException(status, trailers);
    }

    /// <summary>The conventional key; <c>-bin</c> makes it binary metadata.</summary>
    private const string GrpcStatusDetailsTrailer = "grpc-status-details-bin";
}
