using System;
using System.Buffers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProtoBuf.Connect.AspNetCore.Internal
{
    /// <summary>
    /// Writes the Connect error object.
    /// </summary>
    /// <remarks>
    /// Hand-written over <see cref="Utf8JsonWriter"/>, for the same reason the reader is hand-written: the
    /// shape is tiny and fixed, and doing it this way keeps the honest position that nothing here is a JSON
    /// <em>codec</em> - this is the protocol's error envelope, never a payload.
    /// </remarks>
    internal static class ConnectErrorWriter
    {
        public static async Task WriteAsync(HttpResponse response, ConnectException error)
        {
            // a unary error is a non-200 with a JSON body; a streaming error will instead be a 200 whose
            // final envelope carries this same object, which is why the shape is written once here
            response.StatusCode = error.HttpStatus ?? error.Code.ToHttpStatus();
            response.ContentType = "application/json";

            var buffer = new ArrayBufferWriter<byte>(256);
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                writer.WriteString("code"u8, error.Code.ToWireName());

                var message = error.RawMessage;
                if (!string.IsNullOrEmpty(message)) writer.WriteString("message"u8, message);

                if (error.Details.Count != 0)
                {
                    writer.WriteStartArray("details"u8);
                    foreach (var detail in error.Details)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("type"u8, detail.TypeName);
                        writer.WriteString("value"u8, ProtoBuf.Connect.Internal.ConnectBase64.Encode(detail.Value));
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            response.ContentLength = buffer.WrittenCount;
            await response.BodyWriter.WriteAsync(buffer.WrittenMemory).ConfigureAwait(false);
        }
    }
}
