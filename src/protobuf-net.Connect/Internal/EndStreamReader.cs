using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ProtoBuf.Connect.Internal
{
    /// <summary>
    /// Reads the <c>EndStreamResponse</c> that terminates every response stream:
    /// <c>{}</c> on success, or <c>{"error": {...}, "metadata": {"name": ["value"]}}</c>.
    /// </summary>
    /// <remarks>
    /// This is where a streaming call's failure and its trailing metadata both live - which is the whole
    /// reason the protocol needs no HTTP trailers, and therefore no HTTP/2. Note a stream that fails
    /// still carries HTTP <c>200</c>: the status was sent before anything went wrong, so it cannot say
    /// anything about the outcome.
    /// </remarks>
    internal static class EndStreamReader
    {
        public static void Parse(
            ReadOnlySpan<byte> utf8,
            out ConnectException? error,
            out IReadOnlyList<KeyValuePair<string, string>> trailers)
        {
            error = null;
            trailers = Array.Empty<KeyValuePair<string, string>>();

            // an empty payload is not legal but means the same thing as {}, and refusing a stream that
            // otherwise completed cleanly would be unhelpful
            if (utf8.IsEmpty) return;

            List<KeyValuePair<string, string>>? collected = null;
            try
            {
                var reader = new Utf8JsonReader(utf8, new JsonReaderOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                });

                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return;

                while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("error"u8))
                    {
                        if (!reader.Read()) return;
                        if (reader.TokenType != JsonTokenType.StartObject) { reader.Skip(); continue; }

                        if (ConnectErrorReader.TryParseObject(
                            ref reader, out var code, out var message, out var details, out var hasCode))
                        {
                            // The PRESENCE of an error object is the failure; the code merely describes
                            // it. A terminator carrying {"error":{}} or a code we do not recognise is
                            // still a failed call, and reporting success there hands the caller a stream
                            // that simply stopped - which the conformance suite catches as "expecting an
                            // error but received none". There is no HTTP status to infer from here: the
                            // response said 200 long before this arrived.
                            error = new ConnectException(
                                hasCode ? code : ConnectCode.Unknown, message, httpStatus: null, details,
                                codeWasInferred: !hasCode);
                        }
                    }
                    else if (reader.ValueTextEquals("metadata"u8))
                    {
                        if (!reader.Read()) return;
                        if (reader.TokenType != JsonTokenType.StartObject) { reader.Skip(); continue; }
                        collected = ReadMetadata(ref reader);
                    }
                    else
                    {
                        if (!reader.Read()) return;
                        reader.Skip();
                    }
                }
            }
            catch (JsonException)
            {
                // a terminator we cannot read means we cannot know whether the stream succeeded, which
                // is worse than a stream that failed cleanly
                error ??= new ConnectException(
                    ConnectCode.Internal, "The stream's terminating message could not be read.");
                return;
            }

            if (collected is not null) trailers = collected;
        }

        /// <summary>Reads <c>{"name": ["v1","v2"], ...}</c>, joining repeats as HTTP semantics allow.</summary>
        private static List<KeyValuePair<string, string>> ReadMetadata(ref Utf8JsonReader reader)
        {
            var list = new List<KeyValuePair<string, string>>();
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                var name = reader.GetString();
                if (!reader.Read()) break;

                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    string? joined = null;
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        if (reader.TokenType != JsonTokenType.String) { reader.Skip(); continue; }
                        var value = reader.GetString();
                        joined = joined is null ? value : joined + "," + value;
                    }
                    if (name is not null && joined is not null) list.Add(new(name, joined));
                }
                else
                {
                    reader.Skip();
                }
            }
            return list;
        }
    }
}
