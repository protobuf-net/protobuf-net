using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ProtoBuf.Connect.Internal;

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
    /// Parses <c>{"code":"...","message":"...","details":[...]}</c>. Returns <c>false</c> when the
    /// payload is not a usable error object, which the caller must handle by inferring from the HTTP
    /// status instead - the protocol names several such shapes explicitly invalid, including
    /// <c>{}</c> and <c>{"code": null}</c>.
    /// </summary>
    public static bool TryParse(
        ReadOnlySpan<byte> utf8,
        out ConnectCode code,
        out string? message,
        out IReadOnlyList<ConnectErrorDetail> details)
    {
        code = ConnectCode.Unknown;
        message = null;
        details = Array.Empty<ConnectErrorDetail>();

        var haveCode = false;
        List<ConnectErrorDetail>? collected = null;

        try
        {
            var reader = new Utf8JsonReader(utf8, new JsonReaderOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });

            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                if (reader.ValueTextEquals("code"u8))
                {
                    if (!reader.Read()) return false;
                    if (reader.TokenType != JsonTokenType.String) return false; // {"code": null} is invalid
                    code = ConnectCodes.FromWireName(reader.GetString());
                    haveCode = true;
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

        if (!haveCode) return false;
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
