using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf.Serializers;

namespace ProtoBuf.Connect.AspNetCore.Internal
{
    /// <summary>
    /// Reads enveloped messages off a request body.
    /// </summary>
    /// <remarks>
    /// Shared by every streaming shape, because they differ only in how many messages they expect - the
    /// framing is identical, which was established by probing connect-go rather than assumed
    /// (notes/connect/findings.md §21).
    /// <para>
    /// Note a <b>request</b> stream has no terminating message: end-of-stream is a response-only flag,
    /// so the body simply ending is what says "no more". That asymmetry is the protocol's, not ours.
    /// </para>
    /// </remarks>
    internal static class EnvelopedRequestReader
    {
        /// <summary>Reads exactly one message; anything less is a truncated request.</summary>
        public static async Task<T> ReadOneAsync<T>(
            PipeReader reader, ConnectCodec codec, IConnectMessageCodec<T>? serializer, string method,
            ConnectCompression? compression, CancellationToken cancellationToken)
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var buffer = result.Buffer;

                if (ConnectEnvelope.TryRead(ref buffer, out var flags, out var payload))
                {
                    T message;
                    try
                    {
                        message = Decode(codec, serializer, method, flags, payload, compression);
                    }
                    finally
                    {
                        reader.AdvanceTo(buffer.Start, buffer.End);
                    }

                    // ...and then confirm there is nothing after it. This shape declares exactly one
                    // message, so a second is a protocol violation rather than something to ignore -
                    // the suite tests for it by name ("server-stream/multiple-requests").
                    await EnsureEndOfBodyAsync(reader, method, cancellationToken).ConfigureAwait(false);
                    return message;
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                // an empty body is not a truncated message; it is a request with no message at all, which
                // this shape also requires exactly one of ("server-stream/no-request")
                if (result.IsCompleted) throw buffer.IsEmpty ? Missing(method) : Truncated(method);
            }
        }

        /// <summary>Reads messages until the body ends.</summary>
        public static async IAsyncEnumerable<T> ReadAllAsync<T>(
            PipeReader reader,
            ConnectCodec codec,
            IConnectMessageCodec<T>? serializer,
            string method,
            ConnectCompression? compression,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var buffer = result.Buffer;
                var produced = new List<T>();

                // decode into a list before advancing: a ReadOnlySequence cannot cross a yield, and the
                // reader must not be advanced while payloads still reference its buffer
                while (ConnectEnvelope.TryRead(ref buffer, out var flags, out var payload))
                {
                    produced.Add(Decode(codec, serializer, method, flags, payload, compression));
                }

                var completed = result.IsCompleted;
                var remaining = buffer.Length;
                reader.AdvanceTo(buffer.Start, buffer.End);

                foreach (var message in produced) yield return message;

                if (completed)
                {
                    // a partial envelope at the end means the client stopped mid-message
                    if (remaining != 0) throw Truncated(method);
                    yield break;
                }
            }
        }

        private static T Decode<T>(
            ConnectCodec codec, IConnectMessageCodec<T>? serializer, string method, byte flags,
            in ReadOnlySequence<byte> payload, ConnectCompression? compression)
        {
            ReadOnlySequence<byte> body = payload;
            if ((flags & ConnectEnvelope.FlagCompressed) != 0)
            {
                if (compression is null || ConnectCompression.IsIdentity(compression.Name))
                {
                    // `internal`, not `unimplemented`: the envelope claims compression that no
                    // connect-content-encoding negotiated, so the peer has contradicted itself rather
                    // than asked for something we lack. The conformance suite pins the distinction.
                    throw new ConnectException(
                        ConnectCode.Internal,
                        $"A request message for '{method}' is flagged compressed, but no compression was negotiated.");
                }

                try
                {
                    body = new ReadOnlySequence<byte>(compression.Decompress(payload));
                }
                catch (Exception ex) when (ex is not ConnectException)
                {
                    throw new ConnectException(
                        ConnectCode.InvalidArgument,
                        $"A request message for '{method}' could not be decompressed as '{compression.Name}': {ex.Message}",
                        innerException: ex);
                }
            }

            if ((flags & (ConnectEnvelope.FlagEndOfStream | ConnectEnvelope.FlagReserved)) != 0)
            {
                throw new ConnectException(
                    ConnectCode.InvalidArgument,
                    $"A request message for '{method}' set unexpected flags (0x{flags:x2}); end-of-stream is a response-only flag.");
            }

            try
            {
                return codec.Read(body, serializer);
            }
            catch (Exception ex) when (ex is not ConnectException)
            {
                throw new ConnectException(
                    ConnectCode.InvalidArgument,
                    $"A request message for '{method}' could not be read as '{codec.Name}': {ex.Message}",
                    innerException: ex);
            }
        }

        /// <summary>Verifies the request body holds nothing beyond the message already read.</summary>
        private static async Task EnsureEndOfBodyAsync(PipeReader reader, string method, CancellationToken cancellationToken)
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var buffer = result.Buffer;

                if (ConnectEnvelope.TryRead(ref buffer, out _, out _))
                {
                    reader.AdvanceTo(buffer.Start, buffer.End);
                    throw TooMany(method);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
                if (result.IsCompleted)
                {
                    // trailing bytes that are not a whole envelope: still more than the one message
                    if (!buffer.IsEmpty) throw TooMany(method);
                    return;
                }
            }
        }

        private static ConnectException Missing(string method)
            => new(ConnectCode.Unimplemented,
                $"The request to '{method}' carried no message; this method requires exactly one.");

        private static ConnectException TooMany(string method)
            => new(ConnectCode.Unimplemented,
                $"The request to '{method}' carried more than one message; this method requires exactly one.");

        private static ConnectException Truncated(string method)
            => new(ConnectCode.InvalidArgument,
                $"The request to '{method}' ended part-way through an enveloped message.");
    }
}
