using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProtoBuf.MessagePipes
{
    /// <summary>
    /// Represents a duplex messaging channel
    /// </summary>
    /// <typeparam name="TWrite"></typeparam>
    /// <typeparam name="TRead"></typeparam>
    public sealed class DuplexChannel<TWrite, TRead> : Channel<TWrite, TRead>, IAsyncDisposable
    {
        private readonly Channel<TWrite> _send;
        private readonly Channel<TRead> _receive;

#if DEBUG
        private readonly Action<string> _log;
#endif
        [Conditional("DEBUG")]
        private void OnLog(string message)
        {
#if DEBUG
            _log?.Invoke(message);
#endif
        }

        internal DuplexChannel(Channel<TWrite> send, Channel<TRead> receive
#if DEBUG
            , Action<string> log
#endif
            )
        {
            _send = send;
            _receive = receive;
            Writer = send.Writer;
            Reader = receive.Reader;
#if DEBUG
            _log = log;
#endif
        }

        /// <summary>
        /// Sends a single message and consumes a single response; note; this method should only
        /// be used if ALL operations are unary, as no additional tracking is enforced.
        /// </summary>
        public async ValueTask<TRead> UnaryAsync(TWrite value, CancellationToken cancellationToken = default)
        {
            OnLog("UnaryAsync sending request...");
            await Writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
            OnLog("UnaryAsync awaiting response...");
            var response = await Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            OnLog("UnaryAsync awaiting complete");
            return response;
        }

        /// <summary>
        /// Run a message pump as a server, invoking the callback for each request sequentially
        /// </summary>
        public async ValueTask AsServer(Func<TRead, TWrite> server, CancellationToken cancellationToken = default)
        {
            do
            {
                while (Reader.TryRead(out TRead request))
                {
                    OnLog("AsServer writing response...");
                    await Writer.WriteAsync(server(request), cancellationToken).ConfigureAwait(false);
                }
                OnLog("AsServer awaiting requests...");
            } while (await Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false));
            OnLog("AsServer complete");
        }

        /// <summary>
        /// Flushes any outstanding work
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            OnLog("DisposeAsync marking writers complete...");
            _receive.Writer.TryComplete();
            Writer.TryComplete();

            // wait for everything to be sent
            OnLog("DisposeAsync awaiting reader...");
            await _send.Reader.Completion.ConfigureAwait(false);

            OnLog("DisposeAsync complete");
        }

        /// <summary>
        /// Try to receive a single message from the reader
        /// </summary>
        public ValueTask<ReceiveResult> TryReceiveAsync(CancellationToken cancellationToken = default)
        {
            return Reader.TryRead(out var value)
                ? new ValueTask<ReceiveResult>(new ReceiveResult(value))
                : Awaited(this, cancellationToken);


            static async ValueTask<ReceiveResult> Awaited(
                DuplexChannel<TWrite, TRead> channel, CancellationToken cancellationToken)
            {
                if (await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)
                    && channel.Reader.TryRead(out var value))
                {
                    return new ReceiveResult(value);
                }
                return default;
            }
        }

        /// <summary>
        /// Indicates the result of TryReceiveAsync
        /// </summary>
        public readonly struct ReceiveResult
        {
            /// <summary>
            /// Indicates the outcome of TryReceiveAsync attempt
            /// </summary>
            public bool IsSuccess(out TRead value)
            {
                value = Value;
                return Success;
            }
            /// <summary>
            /// Was it possible to receive a message?
            /// </summary>
            public bool Success { get; }
            /// <summary>
            /// The message received, if one
            /// </summary>
            public TRead Value { get; }
            internal ReceiveResult(TRead value)
            {
                Success = true;
                Value = value;
            }
        }
    }
}
