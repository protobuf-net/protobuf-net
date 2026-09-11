using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace ProtoBuf.Connect.AspNetCore.Internal
{
    /// <summary>
    /// Writes the envelope that terminates a response stream: <c>{}</c> on success, or
    /// <c>{"error": {...}, "metadata": {...}}</c>.
    /// </summary>
    /// <remarks>
    /// This is where a streaming call reports how it went, and it is the whole reason the protocol needs
    /// no HTTP trailers. Note the HTTP status was committed to <c>200</c> before the first message went
    /// out, so a failure here is not a failed response - it is a successful response describing a failed
    /// call.
    /// </remarks>
    internal static class EndStreamWriter
    {
        public static async Task WriteAsync(
            PipeWriter writer, ConnectException? error, Metadata? trailers, CancellationToken cancellationToken)
        {
            var buffer = new ArrayBufferWriter<byte>(64);
            using (var json = new Utf8JsonWriter(buffer))
            {
                json.WriteStartObject();

                if (error is not null)
                {
                    json.WriteStartObject("error"u8);
                    json.WriteString("code"u8, error.Code.ToWireName());
                    if (!string.IsNullOrEmpty(error.RawMessage)) json.WriteString("message"u8, error.RawMessage);
                    if (error.Details.Count != 0)
                    {
                        json.WriteStartArray("details"u8);
                        foreach (var detail in error.Details)
                        {
                            json.WriteStartObject();
                            json.WriteString("type"u8, detail.TypeName);
                            json.WriteString("value"u8, ProtoBuf.Connect.Internal.ConnectBase64.Encode(detail.Value));
                            json.WriteEndObject();
                        }
                        json.WriteEndArray();
                    }
                    json.WriteEndObject();
                }

                if (trailers is { Count: > 0 })
                {
                    // each name maps to an ARRAY of values, and a name may legitimately repeat - so the
                    // entries must be GROUPED first. Writing one array per entry produces duplicate JSON
                    // keys instead, which is not valid metadata and which the conformance suite rejects
                    // by name ("contains duplicate key").
                    var grouped = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                    foreach (var entry in trailers)
                    {
                        if (!grouped.TryGetValue(entry.Key, out var values))
                        {
                            grouped[entry.Key] = values = new List<string>();
                        }

                        values.Add(entry.IsBinary ? ProtoBuf.Connect.Internal.ConnectBase64.Encode(entry.ValueBytes) : entry.Value);
                    }

                    json.WriteStartObject("metadata"u8);
                    foreach (var pair in grouped)
                    {
                        json.WriteStartArray(pair.Key);
                        foreach (var value in pair.Value) json.WriteStringValue(value);
                        json.WriteEndArray();
                    }
                    json.WriteEndObject();
                }

                json.WriteEndObject();
            }

            ConnectEnvelope.WriteHeader(writer, ConnectEnvelope.FlagEndOfStream, buffer.WrittenCount);
            writer.Write(buffer.WrittenSpan);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
