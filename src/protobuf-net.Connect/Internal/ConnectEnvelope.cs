using System;
using System.Buffers;
using System.Buffers.Binary;

namespace ProtoBuf.Connect.Internal
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
    internal static class ConnectEnvelope
    {
        /// <summary>The flag byte plus the four length bytes.</summary>
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
