using Pipelines.Sockets.Unofficial;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProtoBuf.MessagePipes
{
    /// <summary>
    /// Provides basic message-pipe functionality over a stream or pipeline; the caller is responsible
    /// for all security etc aspects of securing the transport if necessary
    /// </summary>
    public static class MessagePipe
    {
        private static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(
            ChannelReader<T> reader,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (reader.TryRead(out T item))
                {
                    yield return item;
                }
            }
        }

        private static async ValueTask WriteToChannelAsync<T>(
            IAsyncEnumerable<T> source, ChannelWriter<T> destination, CancellationToken cancellationToken, bool markComplete)
        {
            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await destination.WriteAsync(item, cancellationToken).ConfigureAwait(false);
            }
            if (markComplete) destination.TryComplete();
        }

        static readonly Action<Exception, object> s_ReaderCompleted = (ex, state) =>
        {
            var typed = (TaskCompletionSource<object>)state;
            if (ex != null) typed.TrySetException(ex);
            else typed.TrySetResult(null);
        };

        /// <summary>
        /// Send multiple messages over a stream
        /// </summary>
        public static async ValueTask SendAsync<T>(Stream destination, IAsyncEnumerable<T> source, MessagePipeOptions options = default)
        {
            var writer = StreamConnection.GetWriter(destination, options.PipeOptions);
            var tcs = new TaskCompletionSource<object>();

#pragma warning disable CS0618 // this *is* a Pipe; this is fine
            writer.OnReaderCompleted(s_ReaderCompleted, tcs); // attach to detect when the pipe is drained
#pragma warning restore CS0618

            await SendAsync<T>(writer, source, options.Without(MessagePipeFlags.LeaveOpen)).ConfigureAwait(false); // send the data
            await tcs.Task.ConfigureAwait(false); // wait for the pipe to drain
            await destination.FlushAsync().ConfigureAwait(false); // flush the stream
        }

        /// <summary>
        /// Send multiple messages over a pipe
        /// </summary>
        public static async ValueTask SendAsync<T>(PipeWriter destination, IAsyncEnumerable<T> source, MessagePipeOptions options = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            options = options.Normalize();
            await foreach (var message in source.WithCancellation(options.CancellationToken).ConfigureAwait(false))
            {
                options.OnLog("SendAsync writing...");
                options.Write(destination, message);
                options.OnLog("SendAsync flushing...");
                CheckFlushResult(await destination.FlushAsync(options.CancellationToken).ConfigureAwait(false));
                options.OnLog("SendAsync flushed");
            }
            if (options.MarkComplete) await destination.CompleteAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Send multiple messages over a stream
        /// </summary>
        public static ValueTask SendAsync<T>(Stream destination, ChannelReader<T> source, MessagePipeOptions options = default)
            => SendAsync<T>(destination, AsAsyncEnumerable<T>(source, options.CancellationToken), options);

        /// <summary>
        /// Send multiple messages over a pipe
        /// </summary>
        public static ValueTask SendAsync<T>(PipeWriter destination, ChannelReader<T> source, MessagePipeOptions options = default)
            => SendAsync<T>(destination, AsAsyncEnumerable<T>(source, options.CancellationToken), options);

        static void CheckFlushResult(FlushResult flush)
        {
            if (flush.IsCompleted) ThrowOutputCompletedWithOutstandingMessages();
            if (flush.IsCanceled) ThrowCancelled();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowOutputCompletedWithOutstandingMessages() => throw new EndOfStreamException("The output writer was completed, but there were outstanding messages to send");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCancelled() => throw new TaskCanceledException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInputCompletedWithPartialPayload() => throw new EndOfStreamException("The input reader was completed, but there was an incomplete message in the buffer");

        /// <summary>
        /// Receive multiple messages from a stream
        /// </summary>
        public static IAsyncEnumerable<T> ReceiveAsync<T>(Stream source, MessagePipeOptions options = default)
            => ReceiveAsync<T>(StreamConnection.GetReader(source, options.PipeOptions), options);

        /// <summary>
        /// Receive multiple messages from a stream
        /// </summary>
        public static ValueTask ReceiveAsync<T>(Stream source, ChannelWriter<T> destination, MessagePipeOptions options = default)
            => WriteToChannelAsync(ReceiveAsync<T>(source, options), destination, options.CancellationToken, options.MarkComplete);

        /// <summary>
        /// Receive multiple messages from a pipe
        /// </summary>
        public static ValueTask ReceiveAsync<T>(PipeReader source, ChannelWriter<T> destination, MessagePipeOptions options = default)
            => WriteToChannelAsync(ReceiveAsync<T>(source, options), destination, options.CancellationToken, options.MarkComplete);

        /// <summary>
        /// Receive multiple messages from a pipe
        /// </summary>
        public static async IAsyncEnumerable<T> ReceiveAsync<T>(PipeReader source, MessagePipeOptions options = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            options = options.Normalize();

            while (true)
            {
                // obtain the next buffer
                options.OnLog("ReceiveAsync obtaining buffer...");
                if (!source.TryRead(out var result)) result = await source.ReadAsync(options.CancellationToken).ConfigureAwait(false);
                if (result.IsCanceled) ThrowCancelled();

                // consume the buffer
                var buffer = result.Buffer;
                options.OnLog($"ReceiveAsync got {buffer.Length} bytes");
                int processed = 0;
                while (options.TryRead<T>(ref buffer, out var item))
                {
                    processed++;
                    yield return item;
                }

                if (processed == 0 && result.IsCompleted) // check for termination
                {
                    // we have everything, and there will never be any more; was it clean?
                    if (!buffer.IsEmpty) ThrowInputCompletedWithPartialPayload();
                    break;
                }

                // mark what we consumed (the "ref buffer" means we've sliced it already)
                source.AdvanceTo(buffer.Start, buffer.End);
            }
            if (options.MarkComplete) await source.CompleteAsync().ConfigureAwait(false);
            options.OnLog("ReceiveAsync completed successfully");
        }

        static Channel<T> CreateChannel<T>(object options)
            => options switch
            {
                BoundedChannelOptions bounded => Channel.CreateBounded<T>(bounded),
                UnboundedChannelOptions unbounded => Channel.CreateUnbounded<T>(unbounded),
                _ => Channel.CreateBounded<T>(16), // reasonable default
            };

        /// <summary>
        /// Create a duplex channel over a pipe
        /// </summary>
        /// <remarks><paramref name="receiveChannelOptions"/> and <paramref name="receiveChannelOptions"/> can be <see cref="BoundedChannelOptions"/> or <see cref="UnboundedChannelOptions"/>, otherwise a default bounded channel is used</remarks>
        public static DuplexChannel<TWrite, TRead> DuplexAsync<TWrite, TRead>(IDuplexPipe transport, MessagePipeOptions options = default,
            object sendChannelOptions = null, object receiveChannelOptions = null)
        {
            var send = CreateChannel<TWrite>(sendChannelOptions);
            var receive = CreateChannel<TRead>(receiveChannelOptions);

            // start the read and write flows
            _ = Task.Run(() => SendAsync(transport.Output, send.Reader, options), options.CancellationToken);
            _ = Task.Run(() => ReceiveAsync(transport.Input, receive.Writer, options), options.CancellationToken);

            return new DuplexChannel<TWrite, TRead>(send, receive
#if DEBUG
                , options.Log
#endif
                );
        }

        /// <summary>
        /// Create a duplex channel over a pipe
        /// </summary>
        /// <remarks><paramref name="receiveChannelOptions"/> and <paramref name="receiveChannelOptions"/> can be <see cref="BoundedChannelOptions"/> or <see cref="UnboundedChannelOptions"/>, otherwise a default bounded channel is used</remarks>
        public static DuplexChannel<TWrite, TRead> DuplexAsync<TWrite, TRead>(Stream transport, MessagePipeOptions options = default,
            object sendChannelOptions = null, object receiveChannelOptions = null)
            => DuplexAsync<TWrite, TRead>(StreamConnection.GetDuplex(transport, options.PipeOptions), options, sendChannelOptions, receiveChannelOptions);
    }
}
