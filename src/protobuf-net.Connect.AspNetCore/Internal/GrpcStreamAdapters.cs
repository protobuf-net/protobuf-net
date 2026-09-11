using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;

namespace ProtoBuf.Connect.AspNetCore.Internal
{
    /// <summary>
    /// Bridges between <c>Grpc.Core</c>'s reader/writer-shaped service methods and the
    /// <see cref="IAsyncEnumerable{T}"/> shape this runtime's invokers are built on.
    /// </summary>
    /// <remarks>
    /// The two describe the same streams from opposite ends. A <c>protoc</c>-generated service base is
    /// <em>pushed</em> a writer and <em>pulls</em> from a reader; our invokers <em>pull</em> responses from
    /// a sequence and <em>push</em> requests into one. Pulling from a push is the only direction that needs
    /// a buffer, which is why only the writer side has a channel.
    /// </remarks>
    internal static class GrpcStreamAdapters
    {
        /// <summary>
        /// Presents a handler that writes to an <see cref="IServerStreamWriter{T}"/> as a sequence.
        /// </summary>
        /// <remarks>
        /// The channel is bounded at one, deliberately: the handler's <c>WriteAsync</c> then completes only
        /// once the previous message has been taken by the invoker - i.e. written to the wire - which is
        /// the backpressure a gRPC handler already expects. An unbounded channel would let a fast producer
        /// buffer the whole stream in memory while a slow client read none of it.
        /// </remarks>
        public static async IAsyncEnumerable<TResponse> ToAsyncEnumerable<TResponse>(
            Func<IServerStreamWriter<TResponse>, Task> invoke,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateBounded<TResponse>(new BoundedChannelOptions(1)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait,
            });

            var pump = PumpAsync(invoke, channel.Writer);
            try
            {
                // ReadAllAsync rethrows whatever completed the writer, so a handler's exception surfaces
                // here rather than being left on an unobserved task
                await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    yield return message;
                }
            }
            finally
            {
                // if the consumer walked away - a disconnect, or a `break` - the handler may be parked in
                // WriteAsync on a channel nobody will drain. Completing it from this side makes that write
                // throw, which is how the handler learns the stream is gone; without it the call leaks a
                // task that never finishes.
                channel.Writer.TryComplete();
                await pump.ConfigureAwait(false);
            }
        }

        private static async Task PumpAsync<TResponse>(
            Func<IServerStreamWriter<TResponse>, Task> invoke, ChannelWriter<TResponse> writer)
        {
            // never faults: the failure is handed to the reader through the channel's completion instead,
            // so there is exactly one place it can surface
            try
            {
                await invoke(new ChannelServerStreamWriter<TResponse>(writer)).ConfigureAwait(false);
                writer.TryComplete();
            }
            catch (ChannelClosedException)
            {
                // the reader gave up first and closed the channel under us; it is already unwinding
            }
            catch (Exception ex)
            {
                writer.TryComplete(ex);
            }
        }

        /// <summary>Presents a sequence as an <see cref="IAsyncStreamReader{T}"/>.</summary>
        public static IAsyncStreamReader<TRequest> ToStreamReader<TRequest>(
            IAsyncEnumerable<TRequest> source, CancellationToken cancellationToken)
            => new EnumerableStreamReader<TRequest>(source.GetAsyncEnumerator(cancellationToken));

        private sealed class ChannelServerStreamWriter<T> : IServerStreamWriter<T>
        {
            private readonly ChannelWriter<T> _writer;

            public ChannelServerStreamWriter(ChannelWriter<T> writer) => _writer = writer;

            /// <summary>
            /// Accepted and ignored: every option it carries is a gRPC transport concern.
            /// </summary>
            /// <remarks>
            /// <c>WriteFlags.NoCompress</c> and <c>BufferHint</c> describe gRPC's own per-message
            /// compression and batching, neither of which Connect frames the same way. Throwing on a
            /// property a generated base class may set for its own reasons would be worse than ignoring it.
            /// </remarks>
            public WriteOptions? WriteOptions { get; set; }

            public Task WriteAsync(T message) => WriteAsync(message, CancellationToken.None);

            public Task WriteAsync(T message, CancellationToken cancellationToken)
                => _writer.WriteAsync(message, cancellationToken).AsTask();
        }

        private sealed class EnumerableStreamReader<T> : IAsyncStreamReader<T>
        {
            private readonly IAsyncEnumerator<T> _source;
            private T _current = default!;

            public EnumerableStreamReader(IAsyncEnumerator<T> source) => _source = source;

            /// <remarks>
            /// Held rather than forwarded to the enumerator: once the sequence has ended, an enumerator's
            /// own <c>Current</c> is undefined and may throw, whereas gRPC callers routinely read
            /// <c>Current</c> after a <c>false</c>.
            /// </remarks>
            public T Current => _current;

            /// <param name="cancellationToken">
            /// Ignored. The sequence was created with the call's own token - see
            /// <see cref="ToStreamReader{TRequest}"/> - so cancelling is already wired up; a second token
            /// here could only ever be narrower, and no generated base class passes one.
            /// </param>
            public async Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                if (!await _source.MoveNextAsync().ConfigureAwait(false)) return false;
                _current = _source.Current;
                return true;
            }
        }
    }
}
