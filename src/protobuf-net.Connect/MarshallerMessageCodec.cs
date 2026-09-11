using System;
using System.Buffers;
using Grpc.Core;

namespace ProtoBuf.Connect
{
    /// <summary>
    /// An <see cref="IConnectMessageCodec{T}"/> over a <c>Grpc.Core</c> <see cref="Marshaller{T}"/>.
    /// </summary>
    /// <remarks>
    /// This is the contract-first path: <c>protoc</c>'s generated descriptors carry their own
    /// marshallers, built from Google.Protobuf's <c>Parser</c>, and a Google.Protobuf message is not a
    /// protobuf-net contract - so no <c>TypeModel</c> can serialize it and the marshalling has to come
    /// from the method rather than from the channel.
    /// <para>
    /// Nothing is re-encoded: for <c>application/proto</c> a Connect body and a gRPC body are the same
    /// bytes, so this is the <em>same</em> marshalling the gRPC path would have done, reached through a
    /// different framing.
    /// </para>
    /// </remarks>
    public sealed class MarshallerMessageCodec<T> : IConnectMessageCodec<T>
    {
        private readonly Marshaller<T> _marshaller;

        /// <summary>Creates a codec over a marshaller, normally one from a generated <c>Method&lt;,&gt;</c>.</summary>
        public MarshallerMessageCodec(Marshaller<T> marshaller)
            => _marshaller = marshaller ?? throw new ArgumentNullException(nameof(marshaller));

        /// <inheritdoc/>
        public string CodecName => "proto";

        /// <summary>
        /// The marshaller this wraps.
        /// </summary>
        /// <remarks>
        /// Exposed because a codec that cannot delegate to this one can still make use of it: the JSON
        /// codec needs a <c>MessageDescriptor</c>, and the only route to one that does not reflect is to
        /// materialise an empty message through this marshaller and ask the instance. See
        /// notes/connect/findings.md §46.
        /// </remarks>
        public Marshaller<T> Marshaller => _marshaller;

        /// <summary>
        /// Always <c>null</c>: a marshaller cannot report a length without encoding.
        /// </summary>
        /// <remarks>
        /// Which is the interesting difference from protobuf-net, whose measure pass is cheap and is why
        /// unary requests can state <c>Content-Length</c> and envelopes can write their header first.
        /// Callers must cope - see <c>ConnectFraming</c>, which buffers when a codec cannot measure.
        /// </remarks>
        public long? Measure(T value) => null;

        /// <inheritdoc/>
        public void Write(IBufferWriter<byte> destination, T value)
        {
            if (_marshaller.ContextualSerializer is { } contextual)
            {
                // Prime the writer before handing it over. Google.Protobuf's WriteContext opens with
                // `Advance(0)` - measured, not assumed: writing a message to a logging IBufferWriter gives
                // exactly `Advance(0), GetSpan(0), Advance(n)` - and Kestrel's response writer rejects an
                // Advance that no GetSpan/GetMemory preceded, with "Invalid ordering of calling StartAsync
                // or CompleteAsync and Advance". An unused GetSpan is free and unambiguously legal, where
                // a leading bare Advance(0) is not; buffering instead would cost a copy on every message.
                destination.GetSpan(1);

                var context = new BufferWriterSerializationContext(destination);
                contextual(value, context);
                return;
            }

            // the byte[] form; generated marshallers use the contextual one, but a hand-written
            // Marshaller<T> may not
            destination.Write(_marshaller.Serializer(value));
        }

        /// <inheritdoc/>
        public T Read(in ReadOnlySequence<byte> source)
        {
            if (_marshaller.ContextualDeserializer is { } contextual)
            {
                return contextual(new SequenceDeserializationContext(source));
            }

            return _marshaller.Deserializer(source.ToArray());
        }

        /// <summary>A <see cref="Grpc.Core.SerializationContext"/> that writes straight to an <see cref="IBufferWriter{T}"/>.</summary>
        /// <remarks>
        /// Note the qualification: <c>ProtoBuf.SerializationContext</c> exists too, is <c>sealed</c>, and wins
        /// the name here because this namespace is inside <c>ProtoBuf</c>. They are unrelated types.
        /// </remarks>
        private sealed class BufferWriterSerializationContext : Grpc.Core.SerializationContext
        {
            private readonly IBufferWriter<byte> _destination;

            public BufferWriterSerializationContext(IBufferWriter<byte> destination) => _destination = destination;

            public override IBufferWriter<byte> GetBufferWriter() => _destination;

            // the length is announced before the bytes; we do not need it, because the caller frames
            // around us, but a marshaller is entitled to call this and must not fault
            public override void SetPayloadLength(int payloadLength) { }

            public override void Complete() { }

            public override void Complete(byte[] payload) => _destination.Write(payload);
        }

        /// <summary>A <see cref="Grpc.Core.DeserializationContext"/> over the bytes already in hand.</summary>
        private sealed class SequenceDeserializationContext : Grpc.Core.DeserializationContext
        {
            private readonly ReadOnlySequence<byte> _payload;

            public SequenceDeserializationContext(in ReadOnlySequence<byte> payload) => _payload = payload;

            public override int PayloadLength => checked((int)_payload.Length);

            public override ReadOnlySequence<byte> PayloadAsReadOnlySequence() => _payload;

            public override byte[] PayloadAsNewBuffer() => _payload.ToArray();
        }
    }
}
