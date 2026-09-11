using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ProtoBuf.Connect.Internal
{
    /// <summary>
    /// Reads the Connect error object.
    /// </summary>
    /// <remarks>
    /// Hand-written over <see cref="Utf8JsonReader"/> rather than a serializer, on purpose. The shape is
    /// tiny and fixed, so there is nothing to gain from reflection or a source-generated context - and it
    /// keeps the honest position that this assembly has no JSON <em>codec</em>: it parses the protocol's
    /// own error envelope, never a payload.
    /// </remarks>
    internal static class ConnectErrorReader
    {
        /// <summary>
        /// Parses <c>{"code":"...","message":"...","details":[...]}</c>.
        /// </summary>
        /// <remarks>
        /// <paramref name="hasCode"/> is separate from the return value on purpose. The protocol names
        /// several shapes whose <em>code</em> is unusable - <c>{}</c>, <c>{"code": null}</c>, a code it
        /// does not recognise - but such a body is still an error object, and its <c>message</c> and
        /// <c>details</c> are still the caller's. Reporting "not parseable" for those threw the message
        /// away and reported the whole raw body in its place, which is what the conformance suite caught
        /// ("expected message 'oops'", against a message of <c>{ "message": "oops" }</c>).
        /// <para>
        /// So: the return value says whether this was an error object at all; <paramref name="hasCode"/>
        /// says whether it named its own code, and when it did not, the caller infers one - from the HTTP
        /// status for a unary body, or <see cref="ConnectCode.Unknown"/> in a terminating envelope, which
        /// has no status to fall back on.
        /// </para>
        /// </remarks>
        public static bool TryParse(
            ReadOnlySpan<byte> utf8,
            out ConnectCode code,
            out string? message,
            out IReadOnlyList<ConnectErrorDetail> details,
            out bool hasCode)
        {
            code = ConnectCode.Unknown;
            message = null;
            details = Array.Empty<ConnectErrorDetail>();
            hasCode = false;

            try
            {
                var reader = new Utf8JsonReader(utf8, new JsonReaderOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                });

                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;
                return TryParseObject(ref reader, out code, out message, out details, out hasCode);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Parses the error object itself, with the reader already positioned on its <c>StartObject</c>.
        /// </summary>
        /// <remarks>
        /// Split out because the same object appears in two places: alone in a unary error body, and
        /// nested under <c>error</c> in a stream's terminating message. One parse, two entry points.
        /// </remarks>
        public static bool TryParseObject(
            ref Utf8JsonReader reader,
            out ConnectCode code,
            out string? message,
            out IReadOnlyList<ConnectErrorDetail> details,
            out bool hasCode)
        {
            code = ConnectCode.Unknown;
            message = null;
            details = Array.Empty<ConnectErrorDetail>();
            hasCode = false;
            List<ConnectErrorDetail>? collected = null;

            try
            {
                while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("code"u8))
                    {
                        if (!reader.Read()) return false;

                        // {"code": null}, or a name from a protocol revision we do not know, leaves the
                        // code unusable - but the object is still an error, so parsing continues
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            hasCode = ConnectCodes.TryFromWireName(reader.GetString(), out code);
                        }
                        else
                        {
                            reader.Skip();
                        }
                    }
                    else if (reader.ValueTextEquals("message"u8))
                    {
                        if (!reader.Read()) return false;
                        message = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                    }
                    else if (reader.ValueTextEquals("details"u8))
                    {
                        if (!reader.Read()) return false;
                        if (reader.TokenType != JsonTokenType.StartArray) { reader.Skip(); continue; }
                        collected = ReadDetails(ref reader);
                    }
                    else
                    {
                        // unknown members are tolerated; a later protocol revision may add some
                        if (!reader.Read()) return false;
                        reader.Skip();
                    }
                }
            }
            catch (JsonException)
            {
                return false;
            }

            if (collected is not null) details = collected;
            return true;
        }

        private static List<ConnectErrorDetail> ReadDetails(ref Utf8JsonReader reader)
        {
            var list = new List<ConnectErrorDetail>();
            while (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
            {
                string? type = null;
                byte[]? value = null;

                while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("type"u8))
                    {
                        if (!reader.Read()) break;
                        type = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                    }
                    else if (reader.ValueTextEquals("value"u8))
                    {
                        if (!reader.Read()) break;
                        value = reader.TokenType == JsonTokenType.String ? DecodeBase64(reader.GetString()) : null;
                    }
                    else
                    {
                        // "debug" is arbitrary JSON for human consumption and is explicitly not authoritative
                        if (!reader.Read()) break;
                        reader.Skip();
                    }
                }

                if (type is not null && value is not null) list.Add(new ConnectErrorDetail(type, value));
            }
            return list;
        }

        private static byte[]? DecodeBase64(string? text)
        {
            if (text is null) return null;
            // implementations must accept both padded and unpadded base64
            var padding = (4 - (text.Length % 4)) % 4;
            if (padding == 3) return null; // not a valid length under any padding scheme
            if (padding != 0) text += new string('=', padding);
            try
            {
                return Convert.FromBase64String(text);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
