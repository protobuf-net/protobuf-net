using System.Buffers.Binary;
using Connectrpc.Conformance.V1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ProtoBuf.Connect;

namespace ProtoBuf.ConnectConformance;

/// <summary>
/// The client half of the conformance suite: each test case describes an RPC to make against the
/// runner's own <em>reference server</em>, and we report exactly what came back.
/// </summary>
/// <remarks>
/// The mirror of the server harness, and the more interesting direction of the two - the server mode
/// proves a real Connect client can talk to us, and this proves we can talk to a real Connect server.
/// Between them there is no step where both ends are ours.
/// <para>
/// The calls go through <c>protoc</c>'s generated <c>ConformanceServiceClient</c> over
/// <see cref="ConnectCallInvoker"/>, deliberately: that is the contract-first client path exactly as a
/// consumer would use it, so the suite is testing the thing we ship rather than a bespoke harness.
/// </para>
/// </remarks>
internal static class ConformanceClient
{
    public static async Task<int> RunAsync(string? logPath)
    {
        using var input = Console.OpenStandardInput();
        using var output = Console.OpenStandardOutput();

        while (TryReadRequest(input, out var request))
        {
            ClientCompatResponse response;
            try
            {
                response = await RunOneAsync(request).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // ClientErrorResult means "the harness could not run this case at all", which is a
                // different thing from "the RPC failed" - the latter is a perfectly good result
                Log(logPath, $"{request.TestName}: {ex}");
                response = new ClientCompatResponse
                {
                    TestName = request.TestName,
                    Error = new ClientErrorResult { Message = ex.Message },
                };
            }

            Write(output, response);
        }

        return 0;
    }

    private static async Task<ClientCompatResponse> RunOneAsync(ClientCompatRequest request)
    {
        // There is no ALPN on a plaintext endpoint, so the version has to be stated per request rather
        // than negotiated. HttpClient.DefaultRequestVersion does not reach the HttpRequestMessage the
        // channel builds, so it is pinned by a handler instead - which is also how a consumer would do
        // it, DelegatingHandler being Connect's interception model (there is no protocol-specific
        // interceptor type, and none is wanted).
        using var http = new HttpClient(new PinHttpVersion(request.HttpVersion == HTTPVersion._2
            ? System.Net.HttpVersion.Version20
            : System.Net.HttpVersion.Version11))
        {
            // the suite states the deadline; HttpClient's own timeout must not pre-empt it
            Timeout = Timeout.InfiniteTimeSpan,
        };

        var address = new Uri($"http://{request.Host}:{request.Port}");
        var client = new ConformanceService.ConformanceServiceClient(new ConnectCallInvoker(http, address));

        var result = new ClientResponseResult();
        using var cancellation = new CancellationTokenSource();

        var options = new CallOptions(
            headers: ToMetadata(request.RequestHeaders),
            deadline: request.HasTimeoutMs ? DateTime.UtcNow.AddMilliseconds(request.TimeoutMs) : null,
            cancellationToken: cancellation.Token);

        try
        {
            switch (request.Method)
            {
                case "Unary":
                    await UnaryAsync(client, request, options, result).ConfigureAwait(false);
                    break;
                case "IdempotentUnary":
                    await IdempotentUnaryAsync(client, request, options, result).ConfigureAwait(false);
                    break;
                case "Unimplemented":
                    await UnimplementedAsync(client, request, options, result).ConfigureAwait(false);
                    break;
                case "ServerStream":
                    await ServerStreamAsync(client, request, options, result, cancellation).ConfigureAwait(false);
                    break;
                case "ClientStream":
                    await ClientStreamAsync(client, request, options, result, cancellation).ConfigureAwait(false);
                    break;
                case "BidiStream":
                    await BidiStreamAsync(client, request, options, result, cancellation).ConfigureAwait(false);
                    break;
                default:
                    throw new NotSupportedException($"'{request.Method}' is not a ConformanceService method.");
            }
        }
        catch (RpcException ex)
        {
            Describe(ex, result);
        }
        catch (OperationCanceledException)
        {
            // a case that asked us to cancel; `canceled` is the result it is looking for
            result.Error = new Error { Code = Code.Canceled };
        }

        return new ClientCompatResponse { TestName = request.TestName, Response = result };
    }

    // ---------------------------------------------------------------------------- the six methods

    private static async Task UnaryAsync(
        ConformanceService.ConformanceServiceClient client, ClientCompatRequest request,
        CallOptions options, ClientResponseResult result)
    {
        using var call = client.UnaryAsync(Unpack<UnaryRequest>(request.RequestMessages[0]), options);
        await CaptureAsync(call.ResponseHeadersAsync, () => call.GetStatus(), () => call.GetTrailers(), result,
            async () => Record(result, (await call.ResponseAsync.ConfigureAwait(false)).Payload)).ConfigureAwait(false);
    }

    private static async Task IdempotentUnaryAsync(
        ConformanceService.ConformanceServiceClient client, ClientCompatRequest request,
        CallOptions options, ClientResponseResult result)
    {
        using var call = client.IdempotentUnaryAsync(Unpack<IdempotentUnaryRequest>(request.RequestMessages[0]), options);
        await CaptureAsync(call.ResponseHeadersAsync, () => call.GetStatus(), () => call.GetTrailers(), result,
            async () => Record(result, (await call.ResponseAsync.ConfigureAwait(false)).Payload)).ConfigureAwait(false);
    }

    private static async Task UnimplementedAsync(
        ConformanceService.ConformanceServiceClient client, ClientCompatRequest request,
        CallOptions options, ClientResponseResult result)
    {
        using var call = client.UnimplementedAsync(Unpack<UnimplementedRequest>(request.RequestMessages[0]), options);
        await CaptureAsync(call.ResponseHeadersAsync, () => call.GetStatus(), () => call.GetTrailers(), result,
            async () => await call.ResponseAsync.ConfigureAwait(false)).ConfigureAwait(false);
    }

    private static async Task ServerStreamAsync(
        ConformanceService.ConformanceServiceClient client, ClientCompatRequest request,
        CallOptions options, ClientResponseResult result, CancellationTokenSource cancellation)
    {
        using var call = client.ServerStream(Unpack<ServerStreamRequest>(request.RequestMessages[0]), options);
        await CaptureAsync(call.ResponseHeadersAsync, () => call.GetStatus(), () => call.GetTrailers(), result,
            async () =>
            {
                while (await call.ResponseStream.MoveNext(CancellationToken.None).ConfigureAwait(false))
                {
                    Record(result, call.ResponseStream.Current.Payload);
                    if (ShouldCancelAfter(request, result.Payloads.Count)) { cancellation.Cancel(); break; }
                }
            }).ConfigureAwait(false);
    }

    private static async Task ClientStreamAsync(
        ConformanceService.ConformanceServiceClient client, ClientCompatRequest request,
        CallOptions options, ClientResponseResult result, CancellationTokenSource cancellation)
    {
        using var call = client.ClientStream(options);
        await CaptureAsync(call.ResponseHeadersAsync, () => call.GetStatus(), () => call.GetTrailers(), result,
            async () =>
            {
                var sent = await SendAllAsync(call.RequestStream, request, result, cancellation).ConfigureAwait(false);
                if (sent) Record(result, (await call.ResponseAsync.ConfigureAwait(false)).Payload);
            }).ConfigureAwait(false);
    }

    private static async Task BidiStreamAsync(
        ConformanceService.ConformanceServiceClient client, ClientCompatRequest request,
        CallOptions options, ClientResponseResult result, CancellationTokenSource cancellation)
    {
        using var call = client.BidiStream(options);
        var fullDuplex = request.RequestMessages.Count != 0
            && Unpack<BidiStreamRequest>(request.RequestMessages[0]).FullDuplex;

        await CaptureAsync(call.ResponseHeadersAsync, () => call.GetStatus(), () => call.GetTrailers(), result,
            async () =>
            {
                if (fullDuplex)
                {
                    // interleaved: one response is read for each request sent, which is the shape the
                    // suite means by full duplex and the only one that needs HTTP/2
                    foreach (var message in request.RequestMessages)
                    {
                        await DelayAsync(request.RequestDelayMs).ConfigureAwait(false);
                        await call.RequestStream.WriteAsync(Unpack<BidiStreamRequest>(message)).ConfigureAwait(false);

                        if (!await call.ResponseStream.MoveNext(CancellationToken.None).ConfigureAwait(false)) break;
                        Record(result, call.ResponseStream.Current.Payload);
                        if (ShouldCancelAfter(request, result.Payloads.Count)) { cancellation.Cancel(); return; }
                    }

                    await call.RequestStream.CompleteAsync().ConfigureAwait(false);
                }
                else if (!await SendAllAsync(call.RequestStream, request, result, cancellation).ConfigureAwait(false))
                {
                    return;
                }

                while (await call.ResponseStream.MoveNext(CancellationToken.None).ConfigureAwait(false))
                {
                    Record(result, call.ResponseStream.Current.Payload);
                    if (ShouldCancelAfter(request, result.Payloads.Count)) { cancellation.Cancel(); return; }
                }
            }).ConfigureAwait(false);
    }

    // ------------------------------------------------------------------------------------ helpers

    /// <summary>
    /// Sends every request message, honouring the case's cancellation timing.
    /// </summary>
    /// <returns><c>false</c> when the case asked to be cancelled before the stream was closed.</returns>
    private static async Task<bool> SendAllAsync<T>(
        IClientStreamWriter<T> stream, ClientCompatRequest request, ClientResponseResult result,
        CancellationTokenSource cancellation) where T : IMessage, new()
    {
        for (var i = 0; i < request.RequestMessages.Count; i++)
        {
            await DelayAsync(request.RequestDelayMs).ConfigureAwait(false);
            await stream.WriteAsync(Unpack<T>(request.RequestMessages[i])).ConfigureAwait(false);
        }

        if (request.Cancel?.CancelTimingCase == ClientCompatRequest.Types.Cancel.CancelTimingOneofCase.BeforeCloseSend)
        {
            cancellation.Cancel();
            return false;
        }

        await stream.CompleteAsync().ConfigureAwait(false);

        if (request.Cancel?.CancelTimingCase == ClientCompatRequest.Types.Cancel.CancelTimingOneofCase.AfterCloseSendMs)
        {
            await Task.Delay((int)request.Cancel.AfterCloseSendMs).ConfigureAwait(false);
            cancellation.Cancel();
        }

        return true;
    }

    private static bool ShouldCancelAfter(ClientCompatRequest request, int received)
        => request.Cancel?.CancelTimingCase == ClientCompatRequest.Types.Cancel.CancelTimingOneofCase.AfterNumResponses
            && received >= request.Cancel.AfterNumResponses;

    /// <summary>
    /// Runs the call and records its metadata whichever way it ends.
    /// </summary>
    /// <remarks>
    /// The headers and trailers matter on the failure path as much as the success one - the suite checks
    /// them on errors too - so they are collected in a <c>finally</c> rather than after the body.
    /// </remarks>
    private static async Task CaptureAsync(
        Task<Metadata> headers, Func<Status> status, Func<Metadata> trailers,
        ClientResponseResult result, Func<Task> body)
    {
        try
        {
            await body().ConfigureAwait(false);
            result.ResponseTrailers.AddRange(ToHeaders(Safe(trailers)));
        }
        finally
        {
            if (headers.IsCompletedSuccessfully) result.ResponseHeaders.AddRange(ToHeaders(headers.Result));
        }

        _ = status;
    }

    private static Metadata? Safe(Func<Metadata> get)
    {
        try
        {
            return get();
        }
        catch (InvalidOperationException)
        {
            // not finished, so there is nothing to report; better than failing the case over it
            return null;
        }
    }

    /// <summary>Records an RPC failure as the suite's own error shape.</summary>
    private static void Describe(RpcException exception, ClientResponseResult result)
    {
        var error = new Error { Code = (Code)(int)exception.StatusCode };
        if (!string.IsNullOrEmpty(exception.Status.Detail)) error.Message = exception.Status.Detail;

        if (exception.Status.DebugException is ConnectException connect)
        {
            if (connect.HttpStatus is { } httpStatus) result.HttpStatusCode = httpStatus;

            foreach (var detail in connect.Details)
            {
                // Connect names the type by its message name; Any wants a URL, and the prefix is not
                // significant - only the segment after the last slash is
                error.Details.Add(new Any
                {
                    TypeUrl = "type.googleapis.com/" + detail.TypeName,
                    Value = ByteString.CopyFrom(detail.Value),
                });
            }
        }

        result.Error = error;
        result.ResponseTrailers.AddRange(ToHeaders(exception.Trailers));
    }

    /// <summary>
    /// Records a response payload, tolerating one that is absent.
    /// </summary>
    /// <remarks>
    /// A response message with no payload is legal - an empty <c>ConformancePayload</c> is simply not
    /// written on the wire - and <c>Payloads.Add(null)</c> throws, which would turn a perfectly good
    /// result into a harness error.
    /// </remarks>
    private static void Record(ClientResponseResult result, ConformancePayload? payload)
        => result.Payloads.Add(payload ?? new ConformancePayload());

    private static Task DelayAsync(uint milliseconds)
        => milliseconds == 0 ? Task.CompletedTask : Task.Delay((int)milliseconds);

    private static T Unpack<T>(Any message) where T : IMessage, new()
        => message.Unpack<T>();

    private static Metadata ToMetadata(IEnumerable<Header> headers)
    {
        var metadata = new Metadata();
        foreach (var header in headers)
        {
            foreach (var value in header.Value) metadata.Add(header.Name, value);
        }

        return metadata;
    }

    private static IEnumerable<Header> ToHeaders(Metadata? metadata)
    {
        if (metadata is null) yield break;

        // grouped by name, since metadata may legitimately repeat and the suite compares sets
        var grouped = new Dictionary<string, Header>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in metadata)
        {
            if (!grouped.TryGetValue(entry.Key, out var header))
            {
                grouped[entry.Key] = header = new Header { Name = entry.Key };
            }

            header.Value.Add(entry.IsBinary ? Convert.ToBase64String(entry.ValueBytes) : entry.Value);
        }

        foreach (var header in grouped.Values) yield return header;
    }

    // ------------------------------------------------------------------- the stdin/stdout protocol

    private static bool TryReadRequest(Stream input, out ClientCompatRequest request)
    {
        request = null!;

        Span<byte> header = stackalloc byte[4];
        if (!TryReadExactly(input, header)) return false;

        var payload = new byte[BinaryPrimitives.ReadUInt32BigEndian(header)];
        if (!TryReadExactly(input, payload)) return false;

        request = ClientCompatRequest.Parser.ParseFrom(payload);
        return true;
    }

    private static bool TryReadExactly(Stream input, Span<byte> buffer)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var got = input.Read(buffer[read..]);
            if (got <= 0) return false;   // a clean EOF between cases is how the run ends
            read += got;
        }

        return true;
    }

    private static void Write(Stream output, ClientCompatResponse response)
    {
        var payload = response.ToByteArray();
        Span<byte> header = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(header, (uint)payload.Length);

        output.Write(header);
        output.Write(payload, 0, payload.Length);
        output.Flush();
    }

    /// <summary>Pins the HTTP version of every request, since a plaintext endpoint cannot negotiate.</summary>
    private sealed class PinHttpVersion(Version version) : DelegatingHandler(new SocketsHttpHandler())
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // UNCONDITIONALLY, including over the channel's own duplex pin. ConnectChannel.DuplexAsync
            // pins HTTP/2 because a FULL-duplex call deadlocks without it - but half-duplex bidi over
            // HTTP/1.1 is legal, and the suite tests it, so the pin cannot be the last word. The suite
            // never asks for full duplex over HTTP/1.1, so overriding here is safe.
            request.Version = version;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
            return base.SendAsync(request, cancellationToken);
        }
    }

    private static void Log(string? path, string message)
    {
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            lock (path) File.AppendAllText(path, message + Environment.NewLine + new string('-', 60) + Environment.NewLine);
        }
        catch
        {
            // logging must never be the reason a conformance run fails
        }
    }
}
