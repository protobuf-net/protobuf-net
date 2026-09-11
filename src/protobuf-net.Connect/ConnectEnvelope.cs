using System;
using System.Buffers;
using System.Buffers.Binary;

namespace ProtoBuf.Connect
{
    /// <summary>
    /// The five-byte frame every streaming message carries: one flag byte, then a big-endian
    /// <see cref="uint"/> length, then the payload.
    /// </summary>
    /// <remarks>
    /// Layout-identical to gRPC's framing, which is worth knowing: the flag byte differs in meaning
    /// (gRPC's whole byte is "compressed"; here bit 0 is compressed and bit 1 is end-of-stream) but a
    /// reader and writer are shareable between the two protocols, should we ever serve both from one
    /// handler.
    /// <para>
    /// All three streaming shapes use this in <em>both</em> directions and differ only in cardinality -
    /// confirmed against connect-go, see notes/connect/findings.md §21 - so there is exactly one
    /// framing implementation to get right.
    /// </para>
    /// </remarks>
    public static class ConnectEnvelope
    {
        /// <summary>The flag byte plus the four length bytes.</summary>
        /// <remarks>
        /// Public because the wire format is shared by both halves of this implementation and is fixed
        /// by the specification; a private copy on each side would be a duplicated wire format, which is
        /// the one thing not worth duplicating.
        /// </remarks>
        public const int HeaderLength = 5;

        /// <summary>Bit 0: the payload is compressed per <c>connect-content-encoding</c>.</summary>
        public const byte FlagCompressed = 0b0000_0001;

        /// <summary>Bit 1: this is the terminating message, and its payload is an EndStreamResponse.</summary>
        public const byte FlagEndOfStream = 0b0000_0010;

        /// <summary>Bits 2-7 are reserved and must be zero; a peer setting one is telling us something we do not understand.</summary>
        public const byte FlagReserved = 0b1111_1100;

        /// <summary>Writes a header for a payload of the given length.</summary>
        public static void WriteHeader(IBufferWriter<byte> destination, byte flags, int payloadLength)
        {
            var span = destination.GetSpan(HeaderLength);
            span[0] = flags;
            BinaryPrimitives.WriteUInt32BigEndian(span[1..], checked((uint)payloadLength));
            destination.Advance(HeaderLength);
        }

        /// <summary>
        /// Writes one whole enveloped message - header and payload - to <paramref name="destination"/>.
        /// </summary>
        /// <remarks>
        /// Every streaming shape on both sides goes through this, and that is the point: the header states
        /// the payload's length, so the length and the payload must come from <em>the same</em> encoding.
        /// Measuring with the channel codec and then writing with a per-method one is a length that
        /// describes different bytes than the ones that follow - which desynchronises the stream for every
        /// message after it, with no error at the point of the mistake. Five sites open-coded that pairing
        /// before this existed, and all five had the mismatch.
        /// <para>
        /// A codec that <em>cannot</em> measure is served by buffering the payload and reading its length
        /// off the buffer. That is not a hypothetical: <see cref="MarshallerMessageCodec{T}"/> wraps a
        /// <c>Grpc.Core</c> marshaller, which has no measure pass at all, so the contract-first path takes
        /// this arm for every message. protobuf-net's own codec takes the measured one and never buffers.
        /// </para>
        /// </remarks>
        /// <param name="destination">Where to write.</param>
        /// <param name="codec">The channel codec.</param>
        /// <param name="value">The message.</param>
        /// <param name="over">An optional per-method codec that replaces the channel's encoding.</param>
        /// <param name="flags">The envelope flags; zero for an ordinary message.</param>
        public static void WriteMessage<T>(IBufferWriter<byte> destination, ConnectCodec codec, T value,
            IConnectMessageCodec<T>? over = null, byte flags = 0)
        {
            if (destination is null) throw new ArgumentNullException(nameof(destination));
            if (codec is null) throw new ArgumentNullException(nameof(codec));

            if (codec.Measure(value, over) is { } measured)
            {
                WriteHeader(destination, flags, checked((int)measured));
                codec.Write(destination, value, over);
                return;
            }

            // no measure pass: encode into a scratch buffer, then state what it came to. The copy is the
            // price of not knowing, and is paid only by codecs that cannot tell us.
            using var scratch = new Internal.PooledBufferWriter();
            codec.Write(scratch, value, over);

            WriteHeader(destination, flags, scratch.WrittenCount);
            destination.Write(scratch.WrittenMemory.Span);
        }

        /// <summary>
        /// Takes one whole envelope off the front of <paramref name="buffer"/>, or leaves it untouched
        /// and returns <c>false</c> when less than a whole one has arrived.
        /// </summary>
        public static bool TryRead(ref ReadOnlySequence<byte> buffer, out byte flags, out ReadOnlySequence<byte> payload)
        {
            flags = 0;
            payload = default;
            if (buffer.Length < HeaderLength) return false;

            Span<byte> header = stackalloc byte[HeaderLength];
            buffer.Slice(0, HeaderLength).CopyTo(header);

            flags = header[0];
            var length = BinaryPrimitives.ReadUInt32BigEndian(header[1..]);

            // a length we cannot address is a protocol violation rather than a "wait for more" - saying
            // so here beats waiting forever for bytes that will never arrive
            if (length > int.MaxValue)
            {
                throw new ConnectException(
                    ConnectCode.ResourceExhausted, $"An enveloped message declares {length} bytes, which is too large to read.");
            }

            if (buffer.Length < HeaderLength + length) return false;

            payload = buffer.Slice(HeaderLength, (int)length);
            buffer = buffer.Slice(HeaderLength + (int)length);
            return true;
        }
    }
}
