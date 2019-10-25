using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProtoBuf.MessagePipes
{
    /// <summary>
    /// Additional options that control messaging operations
    /// </summary>
    [Flags]
    public enum MessagePipeFlags
    {
        /// <summary>
        /// No additional flags
        /// </summary>
        None = 0,
        /// <summary>
        /// The system should not mark pipes or streams as complete
        /// </summary>
        LeaveOpen = 1,
    }

    /// <summary>
    /// Configuration options for messaging
    /// </summary>
    public readonly struct MessagePipeOptions
    {
        /// <summary>
        /// Create a new instance
        /// </summary>
        public MessagePipeOptions(TypeModel typeModel = default, CancellationToken cancellationToken = default,
            MessagePipeFlags flags = MessagePipeFlags.None, PipeOptions pipeOptions = default,
            object userState = default, long maxMessageSize = default
#if DEBUG
            , Action<string> log = default
#endif
            )
        {
            TypeModel = typeModel;
            CancellationToken = cancellationToken;
            PipeOptions = pipeOptions;
            UserState = userState;
            MaxMessageSize = maxMessageSize;
            Flags = flags;
#if DEBUG
            Log = log;
#endif
        }

        /// <summary>
        /// Additional options that control messaging operations
        /// </summary>
        public MessagePipeFlags Flags { get; }

        internal bool HasFlag(MessagePipeFlags flag) => (Flags & flag) != 0;
        internal bool OmitsFlag(MessagePipeFlags flag) => (Flags & flag) == 0;

        internal bool MarkComplete => OmitsFlag(MessagePipeFlags.LeaveOpen);

        /// <summary>
        /// The protobuf-net model to use for serialization
        /// </summary>
        public TypeModel TypeModel { get; }
        /// <summary>
        /// Allows cancellation of operations
        /// </summary>
        public CancellationToken CancellationToken { get; }
        /// <summary>
        /// PipeOptions to use if a Pipe is needed
        /// </summary>
        public PipeOptions PipeOptions { get; }
        /// <summary>
        /// The user-state to use with serialization operations
        /// </summary>
        public object UserState { get; }
        /// <summary>
        /// The maximum expected size of messages; larger messages will be rejected
        /// </summary>
        public long MaxMessageSize { get; }

#if DEBUG
        public Action<string> Log { get; }
#endif
        internal MessagePipeOptions Normalize()
            => new MessagePipeOptions(TypeModel ?? TypeModel.DefaultModel, CancellationToken, Flags,
                PipeOptions ?? PipeOptions.Default, UserState,
                MaxMessageSize <= 0 ? DefaultMaxMessageSize : MaxMessageSize
#if DEBUG
                , Log
#endif
                );

        internal MessagePipeOptions Without(MessagePipeFlags flags)
            => new MessagePipeOptions(TypeModel ?? TypeModel.DefaultModel, CancellationToken,
                Flags & ~flags,
                PipeOptions, UserState,
                MaxMessageSize
#if DEBUG
                , Log
#endif
                );

        const long DefaultMaxMessageSize = 2 * 1024 * 1024; // 2 MiB by default, a fair limit

        internal void Write<T>(IBufferWriter<byte> destination, T message)
        {
            using var measure = TypeModel.Measure<T>(message, UserState, MaxMessageSize);
            var span = destination.GetSpan(10);
            int bytes = ProtoWriter.State.WriteVarint64((ulong)measure.Length, span, 0);
            destination.Advance(bytes);
            OnLog($"Serializing message of {measure.Length} bytes (header: {bytes} bytes)");
            measure.Serialize(destination);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowOversize(long length)
            => throw new InvalidOperationException($"The incoming message size of {length} bytes exceeds the configured quota of {MaxMessageSize} bytes");

        internal bool TryRead<T>(ref ReadOnlySequence<byte> payload, out T item)
        {
            OnLog($"Processing {payload.Length} available bytes");
            var bodyLength = ReadMessageLength(payload, out var headerLength);
            if (MaxMessageSize > 0 && bodyLength > MaxMessageSize) ThrowOversize(bodyLength);

            if (headerLength == 0 || payload.Length < (bodyLength + headerLength))
            {
                OnLog($"No message available");
                item = default;
                return false;
            }

            OnLog($"Deserializing message of {bodyLength} bytes (header: {headerLength} bytes)");
            var messageBody = payload.Slice(headerLength, bodyLength);
            item = TypeModel.Deserialize<T>(messageBody);
            payload = payload.Slice(messageBody.End);
            return true;
        }

        [Conditional("DEBUG")]
        internal void OnLog(string message)
        {
#if DEBUG
            Log?.Invoke(message);
#endif
        }

        static long ReadMessageLength(ReadOnlySequence<byte> payload, out int headerLength)
        {
            if (payload.IsEmpty)
            {
                headerLength = 0;
                return 0;
            }

            // check the first 10 bytes, if we have that many, for a header
            if (payload.IsSingleSegment || payload.First.Length >= 10)
                return ReadMessageLength(payload.First.Span, out headerLength);

            var payloadLength = payload.Length;
            if (payloadLength >= 10)
            {
                Span<byte> span = stackalloc byte[10];
                payload.Slice(0, 10).CopyTo(span);
                return ReadMessageLength(span, out headerLength);
            }
            else
            {
                Span<byte> span = stackalloc byte[(int)payloadLength];
                payload.CopyTo(span);
                return ReadMessageLength(span, out headerLength);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ThrowInvalidHeader()
            => throw new InvalidOperationException("An unexpected message header was encountered");
        static long ReadMessageLength(ReadOnlySpan<byte> header, out int headerLength)
        {
            int bytes = ProtoReader.State.TryParseUInt64Varint(header, 0, out var bodyLength);
            if (bytes == 0)
            {
                headerLength = 0;
                return 0;
            }
            else
            {
                headerLength = bytes;
                return (long)bodyLength;
            }
        }
    }
}