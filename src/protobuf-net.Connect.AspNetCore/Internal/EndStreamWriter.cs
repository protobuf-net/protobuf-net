using System;
using System.Buffers;
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
                            json.WriteBase64String("value"u8, detail.Value);
                            json.WriteEndObject();
                        }
                        json.WriteEndArray();
                    }
                    json.WriteEndObject();
                }

                if (trailers is { Count: > 0 })
                {
                    json.WriteStartObject("metadata"u8);
                    foreach (var entry in trailers)
                    {
                        // each name maps to an ARRAY of values, since metadata may repeat
                        json.WriteStartArray(entry.Key);
                        json.WriteStringValue(entry.IsBinary ? Convert.ToBase64String(entry.ValueBytes) : entry.Value);
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
