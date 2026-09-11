using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;

namespace ProtoBuf.Connect
{
    /// <summary>
    /// One compression algorithm, named as it appears in <c>content-encoding</c> and
    /// <c>connect-content-encoding</c>.
    /// </summary>
    /// <remarks>
    /// Connect negotiates compression in two places, and they are deliberately separate:
    /// <list type="bullet">
    /// <item>a <b>unary</b> body is an ordinary HTTP body, so it uses ordinary <c>content-encoding</c>
    /// and <c>accept-encoding</c>;</item>
    /// <item>an <b>enveloped</b> stream compresses each message individually, under Connect's own
    /// <c>connect-content-encoding</c> and <c>connect-accept-encoding</c>, with the envelope's flag bit
    /// 0 saying whether <em>that</em> message is compressed.</item>
    /// </list>
    /// The second exists because the HTTP body of a streaming call is a frame sequence, not a message -
    /// compressing it as a whole would defeat the framing, and an intermediary that re-encoded it would
    /// corrupt the stream.
    /// <para>
    /// The per-message flag is what makes "compressed" a property of a message rather than of a stream,
    /// so a sender may leave a small message uncompressed even when compression is negotiated. This
    /// implementation does exactly that - see <see cref="MinimumSize"/>.
    /// </para>
    /// </remarks>
    public abstract class ConnectCompression
    {
        /// <summary>The encoding name, as it appears on the wire.</summary>
        public abstract string Name { get; }

        /// <summary>Compresses a payload.</summary>
        public abstract byte[] Compress(in ReadOnlySequence<byte> payload);

        /// <summary>Decompresses a payload.</summary>
        public abstract byte[] Decompress(in ReadOnlySequence<byte> payload);

        /// <summary>
        /// Below this many bytes, a message is sent uncompressed even when compression is negotiated.
        /// </summary>
        /// <remarks>
        /// Compressing a very small message reliably makes it bigger - gzip alone carries an 18-byte
        /// header and trailer - so there is nothing to gain and a header's worth to lose. The protocol
        /// allows this explicitly: the envelope flag is per message, and a reader must cope with either.
        /// </remarks>
        public virtual int MinimumSize => 64;

        /// <summary>The identity "compression": no compression at all.</summary>
        /// <remarks>
        /// Named rather than represented by <c>null</c> because the wire has a name for it, and a peer
        /// may state <c>identity</c> explicitly - which must be accepted rather than rejected as unknown.
        /// </remarks>
        public static ConnectCompression Identity { get; } = new IdentityCompression();

        /// <summary>gzip, which every Connect implementation supports.</summary>
        public static ConnectCompression Gzip { get; } = new GzipCompression();

        /// <summary>Brotli.</summary>
        public static ConnectCompression Brotli { get; } = new BrotliCompression();

        /// <summary>
        /// <c>deflate</c>, which on the wire means <b>zlib</b> (RFC 1950) rather than raw DEFLATE.
        /// </summary>
        /// <remarks>
        /// The name is a long-standing misnomer and the distinction is two bytes of header plus a
        /// checksum, so getting it wrong produces a stream that looks almost right and decodes nowhere.
        /// .NET's <see cref="DeflateStream"/> is the <em>raw</em> form and is the wrong one here;
        /// <c>ZLibStream</c> is the right one. The conformance suite found this immediately - gzip and
        /// brotli passed, and every single deflate case failed.
        /// </remarks>
        public static ConnectCompression Deflate { get; } = new DeflateCompression();

        /// <summary>Whether a name means "no compression" - absent, empty, or <c>identity</c>.</summary>
        public static bool IsIdentity(string? name)
            => string.IsNullOrEmpty(name) || string.Equals(name, "identity", StringComparison.OrdinalIgnoreCase);

        private static byte[] Apply(in ReadOnlySequence<byte> payload, Func<Stream, Stream> wrap, bool compressing)
        {
            var output = new MemoryStream();
            if (compressing)
            {
                using (var compressor = wrap(output))
                {
                    foreach (var segment in payload) compressor.Write(segment.Span);
                }

                return output.ToArray();
            }

            var input = new MemoryStream(payload.ToArray(), writable: false);
            using (var decompressor = wrap(input))
            {
                decompressor.CopyTo(output);
            }

            return output.ToArray();
        }

        private sealed class IdentityCompression : ConnectCompression
        {
            public override string Name => "identity";
            public override int MinimumSize => int.MaxValue;   // never worth "compressing"
            public override byte[] Compress(in ReadOnlySequence<byte> payload) => payload.ToArray();
            public override byte[] Decompress(in ReadOnlySequence<byte> payload) => payload.ToArray();
        }

        private sealed class GzipCompression : ConnectCompression
        {
            public override string Name => "gzip";

            public override byte[] Compress(in ReadOnlySequence<byte> payload)
                => Apply(payload, static s => new GZipStream(s, CompressionLevel.Fastest, leaveOpen: true), compressing: true);

            public override byte[] Decompress(in ReadOnlySequence<byte> payload)
                => Apply(payload, static s => new GZipStream(s, CompressionMode.Decompress), compressing: false);
        }

        private sealed class BrotliCompression : ConnectCompression
        {
            public override string Name => "br";

            public override byte[] Compress(in ReadOnlySequence<byte> payload)
                => Apply(payload, static s => new BrotliStream(s, CompressionLevel.Fastest, leaveOpen: true), compressing: true);

            public override byte[] Decompress(in ReadOnlySequence<byte> payload)
                => Apply(payload, static s => new BrotliStream(s, CompressionMode.Decompress), compressing: false);
        }

        private sealed class DeflateCompression : ConnectCompression
        {
            public override string Name => "deflate";

            public override byte[] Compress(in ReadOnlySequence<byte> payload)
                => Apply(payload, static s => new ZLibStream(s, CompressionLevel.Fastest, leaveOpen: true), compressing: true);

            public override byte[] Decompress(in ReadOnlySequence<byte> payload)
                => Apply(payload, static s => new ZLibStream(s, CompressionMode.Decompress), compressing: false);
        }
    }
}
