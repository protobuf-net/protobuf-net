using System;
using System.Collections.Generic;
using System.Buffers.Binary;

namespace ProtoBuf.Connect.Internal
{
    /// <summary>
    /// Reads the rich error details a gRPC service attaches, so they survive onto a Connect error.
    /// </summary>
    /// <remarks>
    /// <para>
    /// gRPC has no vocabulary for error details in its status object; the convention - and it is only a
    /// convention, implemented by <c>Grpc.StatusProto</c> and every other language's equivalent - is a
    /// <c>grpc-status-details-bin</c> trailer holding a serialized <c>google.rpc.Status</c>, whose
    /// <c>details</c> field is a list of <c>Any</c>. Connect has a first-class <c>details</c> array
    /// instead, so the two say the same thing in different places.
    /// </para>
    /// <para>
    /// Honouring the trailer is what lets an <em>existing</em> contract-first service keep its rich
    /// errors when it moves onto Connect, which is the whole premise of that path. Without it the code
    /// and message survive and the details are silently dropped.
    /// </para>
    /// <para>
    /// Hand-parsed rather than taking a dependency on Google.Protobuf: the shape is two nested messages
    /// with three fields between them, it is fixed by a published contract, and this assembly
    /// deliberately depends on no protobuf implementation but protobuf-net's own.
    /// </para>
    /// </remarks>
    internal static class GrpcStatusDetails
    {
        /// <summary>The metadata key the convention uses.</summary>
        public const string TrailerName = "grpc-status-details-bin";

        /// <summary>
        /// Extracts the <c>details</c> of a serialized <c>google.rpc.Status</c>.
        /// </summary>
        /// <remarks>
        /// Lenient by design: this is decorative metadata on a path that is already reporting a failure,
        /// so a payload we cannot read yields no details rather than replacing the caller's error with a
        /// parse error of our own.
        /// </remarks>
        public static IReadOnlyList<ConnectErrorDetail> Read(ReadOnlySpan<byte> status)
        {
            List<ConnectErrorDetail>? details = null;

            // google.rpc.Status { 1: int32 code, 2: string message, 3: repeated Any details }
            while (TryReadField(ref status, out var field, out var wireType, out var payload))
            {
                if (field != 3 || wireType != 2) continue;
                if (TryReadAny(payload, out var detail)) (details ??= new()).Add(detail);
            }

            return (IReadOnlyList<ConnectErrorDetail>?)details ?? Array.Empty<ConnectErrorDetail>();
        }

        /// <summary>google.protobuf.Any { 1: string type_url, 2: bytes value }</summary>
        private static bool TryReadAny(ReadOnlySpan<byte> any, out ConnectErrorDetail detail)
        {
            string? typeUrl = null;
            byte[]? value = null;

            while (TryReadField(ref any, out var field, out var wireType, out var payload))
            {
                if (wireType != 2) continue;
                if (field == 1) typeUrl = System.Text.Encoding.UTF8.GetString(payload);
                else if (field == 2) value = payload.ToArray();
            }

            if (typeUrl is null)
            {
                detail = default;
                return false;
            }

            // Connect names the type by its fully-qualified message name, where Any carries a URL
            // ("type.googleapis.com/google.protobuf.Duration"). The prefix is explicitly not significant
            // in Any either - only the segment after the last slash is - so dropping it loses nothing.
            var slash = typeUrl.LastIndexOf('/');
            if (slash >= 0) typeUrl = typeUrl.Substring(slash + 1);

            detail = new ConnectErrorDetail(typeUrl, value ?? Array.Empty<byte>());
            return true;
        }

        /// <summary>
        /// Reads one field header and its payload, advancing <paramref name="buffer"/> past both.
        /// </summary>
        private static bool TryReadField(
            ref ReadOnlySpan<byte> buffer, out int fieldNumber, out int wireType, out ReadOnlySpan<byte> payload)
        {
            fieldNumber = 0;
            wireType = 0;
            payload = default;

            if (!TryReadVarint(ref buffer, out var tag)) return false;

            fieldNumber = (int)(tag >> 3);
            wireType = (int)(tag & 7);

            switch (wireType)
            {
                case 0: // varint
                    return TryReadVarint(ref buffer, out _);
                case 1: // fixed64
                    if (buffer.Length < 8) return false;
                    buffer = buffer.Slice(8);
                    return true;
                case 2: // length-delimited
                    if (!TryReadVarint(ref buffer, out var length) || length > (ulong)buffer.Length) return false;
                    payload = buffer.Slice(0, (int)length);
                    buffer = buffer.Slice((int)length);
                    return true;
                case 5: // fixed32
                    if (buffer.Length < 4) return false;
                    buffer = buffer.Slice(4);
                    return true;
                default:
                    // groups, or something invalid: stop rather than guess
                    return false;
            }
        }

        private static bool TryReadVarint(ref ReadOnlySpan<byte> buffer, out ulong value)
        {
            value = 0;
            for (var shift = 0; shift < 64; shift += 7)
            {
                if (buffer.IsEmpty) return false;

                var b = buffer[0];
                buffer = buffer.Slice(1);
                value |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) return true;
            }

            return false;
        }
    }
}
