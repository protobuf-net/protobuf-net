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
            PipeReader reader, ConnectCodec codec, IConnectMessageCodec<T>? serializer, string method, CancellationToken cancellationToken)
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var buffer = result.Buffer;

                if (ConnectEnvelope.TryRead(ref buffer, out var flags, out var payload))
                {
                    try
                    {
                        return Decode(codec, serializer, method, flags, payload);
                    }
                    finally
                    {
                        // anything after the single message is ignored: this shape declares one, and
                        // reading further would be inventing cardinality the contract did not state
                        reader.AdvanceTo(buffer.Start, buffer.End);
                    }
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
                if (result.IsCompleted) throw Truncated(method);
            }
        }

        /// <summary>Reads messages until the body ends.</summary>
        public static async IAsyncEnumerable<T> ReadAllAsync<T>(
            PipeReader reader,
            ConnectCodec codec,
            IConnectMessageCodec<T>? serializer,
            string method,
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
                    produced.Add(Decode(codec, serializer, method, flags, payload));
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
            ConnectCodec codec, IConnectMessageCodec<T>? serializer, string method, byte flags, in ReadOnlySequence<byte> payload)
        {
            if ((flags & ConnectEnvelope.FlagCompressed) != 0)
            {
                throw new ConnectException(
                    ConnectCode.Unimplemented, "Compressed request messages are not implemented yet.");
            }

            if ((flags & (ConnectEnvelope.FlagEndOfStream | ConnectEnvelope.FlagReserved)) != 0)
            {
                throw new ConnectException(
                    ConnectCode.InvalidArgument,
                    $"A request message for '{method}' set unexpected flags (0x{flags:x2}); end-of-stream is a response-only flag.");
            }

            try
            {
                return codec.Read(payload, serializer);
            }
            catch (Exception ex) when (ex is not ConnectException)
            {
                throw new ConnectException(
                    ConnectCode.InvalidArgument,
                    $"A request message for '{method}' could not be read as '{codec.Name}': {ex.Message}",
                    innerException: ex);
            }
        }

        private static ConnectException Truncated(string method)
            => new(ConnectCode.InvalidArgument,
                $"The request to '{method}' ended part-way through an enveloped message.");
    }
}
