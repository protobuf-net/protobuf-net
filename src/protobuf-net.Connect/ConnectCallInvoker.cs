using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using ProtoBuf.Connect.Internal;

namespace ProtoBuf.Connect
{
    /// <summary>
    /// Lets a <c>protoc</c>-generated gRPC client speak Connect, unchanged.
    /// </summary>
    /// <remarks>
    /// The client half of what <c>ContractFirstConnectExtensions</c> does for servers, and the consumer's
    /// whole opt-in is the constructor argument:
    /// <code>
    /// var client = new Greeter.GreeterClient(new ConnectCallInvoker(http, baseAddress));
    /// </code>
    /// <para>
    /// <see cref="CallInvoker"/> is the only way in, and that is by design rather than by luck: a generated
    /// client holds its <c>Method&lt;,&gt;</c> descriptors in <c>private static readonly</c> fields and
    /// routes every call through its invoker, so the invoker is the documented seam for exactly this - a
    /// different transport under an unchanged client.
    /// </para>
    /// <para>
    /// The headline consequence is that such a client no longer needs HTTP/2: Connect carries unary and
    /// the two single-ended streaming shapes over HTTP/1.1, because it puts trailing metadata in the body
    /// rather than in HTTP trailers. Only bidirectional streaming still requires HTTP/2, and for the same
    /// reason it always did - interleaving two bodies is not something HTTP/1.1 can express.
    /// </para>
    /// </remarks>
    public sealed class ConnectCallInvoker : CallInvoker
    {
        private readonly ConnectChannel _channel;

        /// <summary>Creates an invoker over an existing <see cref="ConnectChannel"/>.</summary>
        public ConnectCallInvoker(ConnectChannel channel)
            => _channel = channel ?? throw new ArgumentNullException(nameof(channel));

        /// <summary>
        /// Creates an invoker over the given client and address, marshalling through the descriptors the
        /// generated client already carries.
        /// </summary>
        /// <remarks>
        /// The codec is <see cref="MarshallerConnectCodec"/> because there is nothing for a channel-level
        /// codec to do here: every method supplies its own, built from its own marshaller.
        /// </remarks>
        public ConnectCallInvoker(System.Net.Http.HttpClient http, Uri? baseAddress = null)
            : this(new ConnectChannel(http, MarshallerConnectCodec.Instance, baseAddress)) { }

        /// <inheritdoc/>
        public override TResponse BlockingUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
            // there is no synchronous transport underneath - HttpClient is async to the bottom - so this
            // blocks on the async call rather than pretending to a sync path. Generated clients expose it,
            // so refusing would make an ordinary client partly unusable for no gain.
            => Invoke(method, host, options, request).GetAwaiter().GetResult();

        /// <inheritdoc/>
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
        {
            var call = new CallState();
            var response = InvokeWithMetadata(method, host, options, request, call);
            return new AsyncUnaryCall<TResponse>(
                response, call.HeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
        }

        /// <inheritdoc/>
        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
        {
            var connect = Describe(method, host);
            var call = new CallState();
            var responses = Track(
                _channel.ServerStreamingAsync(connect, request, ConnectCallOptions.From(options), options.CancellationToken),
                call, options.CancellationToken);

            return new AsyncServerStreamingCall<TResponse>(
                GrpcStreamAdapters.ToStreamReader(responses, options.CancellationToken),
                call.HeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
        }

        /// <inheritdoc/>
        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options)
        {
            var connect = Describe(method, host);
            var call = new CallState();
            var (writer, requests) = GrpcStreamAdapters.CreateRequestStream<TRequest>();

            // the call starts now and completes when the caller calls CompleteAsync, which ends the
            // sequence and so ends the request body
            var response = Capture(
                _channel.ClientStreamingWithMetadataAsync(connect, requests, ConnectCallOptions.From(options), options.CancellationToken),
                call);

            return new AsyncClientStreamingCall<TRequest, TResponse>(
                writer, response, call.HeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
        }

        /// <inheritdoc/>
        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options)
        {
            var connect = Describe(method, host);
            var call = new CallState();
            var (writer, requests) = GrpcStreamAdapters.CreateRequestStream<TRequest>();
            var responses = Track(
                _channel.DuplexAsync(connect, requests, ConnectCallOptions.From(options), options.CancellationToken),
                call, options.CancellationToken);

            return new AsyncDuplexStreamingCall<TRequest, TResponse>(
                writer,
                GrpcStreamAdapters.ToStreamReader(responses, options.CancellationToken),
                call.HeadersAsync, call.GetStatus, call.GetTrailers, call.Dispose);
        }

        private async Task<TResponse> Invoke<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
        {
            try
            {
                return await _channel.UnaryAsync(
                    Describe(method, host), request, ConnectCallOptions.From(options), options.CancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ConnectException ex)
            {
                throw ToRpcException(ex);
            }
        }

        private async Task<TResponse> InvokeWithMetadata<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request, CallState call)
        {
            try
            {
                var (response, result) = await _channel.UnaryWithMetadataAsync(
                    Describe(method, host), request, ConnectCallOptions.From(options), options.CancellationToken)
                    .ConfigureAwait(false);

                call.SetHeaders(result.Headers);
                call.Complete(result.Trailers, Status.DefaultSuccess);
                return response;
            }
            catch (ConnectException ex)
            {
                call.Fail(ex);
                throw ToRpcException(ex);
            }
            catch (Exception ex)
            {
                call.Fail(new ConnectException(ConnectCode.Unknown, ex.Message, innerException: ex));
                throw;
            }
        }

        /// <summary>Runs a task that produces the single response of a client-streaming call.</summary>
        private static async Task<TResponse> Capture<TResponse>(Task<(TResponse Response, ConnectCallResult Call)> pending, CallState call)
        {
            try
            {
                var (response, result) = await pending.ConfigureAwait(false);
                call.SetHeaders(result.Headers);
                call.Complete(result.Trailers, Status.DefaultSuccess);
                return response;
            }
            catch (ConnectException ex)
            {
                call.Fail(ex);
                throw ToRpcException(ex);
            }
        }

        /// <summary>
        /// Wraps a response stream so the call's metadata and status are settled as they become known.
        /// </summary>
        /// <remarks>
        /// Deliberately driven from <c>ConnectServerStream</c> rather than from a bare sequence: gRPC
        /// callers may await <c>ResponseHeadersAsync</c> <em>before</em> reading any message, and Connect
        /// delivers trailing metadata in the terminating envelope - so a bare sequence would resolve
        /// headers only at the end of the call and report no trailers at all.
        /// </remarks>
        private static async IAsyncEnumerable<TResponse> Track<TResponse>(
            Task<ConnectServerStream<TResponse>> pending,
            CallState call,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ConnectServerStream<TResponse> stream;
            try
            {
                stream = await pending.ConfigureAwait(false);
            }
            catch (ConnectException ex)
            {
                call.Fail(ex);
                throw ToRpcException(ex);
            }

            // the response headers have arrived by now; the body may not have started
            call.SetHeaders(stream.Headers);

            var enumerator = stream.GetAsyncEnumerator(cancellationToken);
            try
            {
                while (true)
                {
                    TResponse current;
                    try
                    {
                        if (!await enumerator.MoveNextAsync().ConfigureAwait(false)) break;
                        current = enumerator.Current;
                    }
                    catch (ConnectException ex)
                    {
                        // a streaming failure arrives in the terminating envelope under HTTP 200, so it
                        // surfaces here rather than at the call's start - which is exactly where gRPC
                        // callers expect a mid-stream status
                        call.Fail(ex);
                        throw ToRpcException(ex);
                    }

                    yield return current;
                }

                // read after the terminator, which is where Connect puts trailing metadata
                call.Complete(stream.Trailers, Status.DefaultSuccess);
            }
            finally
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <remarks>
        /// <paramref name="host"/> is refused rather than ignored. gRPC's per-call host overrides the
        /// channel's authority; a Connect call is an ordinary HTTP request to a resolved
        /// <see cref="Uri"/>, so honouring it would mean rewriting the address per call, and *ignoring*
        /// it would silently send the call somewhere the caller did not ask for. Generated clients always
        /// pass <c>null</c>.
        /// </remarks>
        private static ConnectMethod<TRequest, TResponse> Describe<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host)
        {
            if (!string.IsNullOrEmpty(host))
            {
                throw new NotSupportedException(
                    $"A per-call host ('{host}') is not supported; set the address on the {nameof(ConnectChannel)} instead.");
            }

            return ConnectMethod.FromGrpc(method);
        }

        private static RpcException ToRpcException(ConnectException error)
        {
            var status = new Status(ConnectException.ToStatusCode(error.Code), error.RawMessage ?? error.Message, error);

            // trailing metadata rides on the exception, which is where a gRPC caller looks for it
            if (error.Trailers.Count == 0) return new RpcException(status);

            var trailers = new Metadata();
            foreach (var pair in error.Trailers) trailers.Add(pair.Key, pair.Value);
            return new RpcException(status, trailers);
        }

        /// <summary>
        /// Holds the headers, trailers and status a <c>Grpc.Core</c> call object hands back.
        /// </summary>
        /// <remarks>
        /// gRPC's call objects want these as a task and two callbacks that may be read <em>before</em> the
        /// call finishes - <c>GetStatus()</c> throws if asked too early - so they cannot simply be
        /// returned alongside the response.
        /// </remarks>
        private sealed class CallState
        {
            private readonly TaskCompletionSource<Metadata> _headers =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            private Metadata _trailers = new();
            private Status? _status;

            public Task<Metadata> HeadersAsync => _headers.Task;

            public Status GetStatus() => _status
                ?? throw new InvalidOperationException("The call is not finished; its status is not yet known.");

            public Metadata GetTrailers() => _status is null
                ? throw new InvalidOperationException("The call is not finished; its trailers are not yet known.")
                : _trailers;

            public void SetHeaders(IReadOnlyList<KeyValuePair<string, string>> headers)
                => _headers.TrySetResult(ToMetadata(headers));

            public void Complete(IReadOnlyList<KeyValuePair<string, string>> trailers, Status status)
            {
                _headers.TrySetResult(new Metadata());
                _trailers = ToMetadata(trailers);
                _status ??= status;
            }

            public void Fail(ConnectException error)
            {
                // a failed call still has metadata, and callers read it: gRPC puts it on
                // RpcException.Trailers, and the suite checks both sides of it
                _headers.TrySetResult(ToMetadata(error.Headers));
                _trailers = ToMetadata(error.Trailers);
                _status ??= new Status(ConnectException.ToStatusCode(error.Code), error.RawMessage ?? error.Message, error);
            }

            public void Dispose() => _headers.TrySetCanceled();

            private static Metadata ToMetadata(IReadOnlyList<KeyValuePair<string, string>> source)
            {
                var metadata = new Metadata();
                foreach (var pair in source) metadata.Add(pair.Key, pair.Value);
                return metadata;
            }
        }
    }
}
