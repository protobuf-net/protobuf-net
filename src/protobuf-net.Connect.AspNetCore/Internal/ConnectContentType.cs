using System;

namespace ProtoBuf.Connect.AspNetCore.Internal;

/// <summary>
/// The two independent things a Connect content-type states: which codec, and whether the body is
/// enveloped.
/// </summary>
internal readonly struct ConnectContentType
{
    private ConnectContentType(string codecName, bool isEnveloped)
    {
        CodecName = codecName;
        IsEnveloped = isEnveloped;
    }

    /// <summary><c>proto</c>, <c>json</c>, or whatever else was offered.</summary>
    public string CodecName { get; }

    /// <summary><c>true</c> for <c>application/connect+*</c>, i.e. a streaming call.</summary>
    public bool IsEnveloped { get; }

    /// <summary>
    /// Parses <c>application/proto</c>, <c>application/json</c>, <c>application/connect+proto</c>, and
    /// so on; parameters such as <c>; charset=utf-8</c> are ignored.
    /// </summary>
    public static bool TryParse(string? contentType, out ConnectContentType result)
    {
        result = default;
        if (string.IsNullOrEmpty(contentType)) return false;

        var span = contentType.AsSpan();
        var semicolon = span.IndexOf(';');
        if (semicolon >= 0) span = span[..semicolon];
        span = span.Trim();

        const string Prefix = "application/";
        if (!span.StartsWith(Prefix.AsSpan(), StringComparison.OrdinalIgnoreCase)) return false;
        span = span[Prefix.Length..];
        if (span.IsEmpty) return false;

        const string Enveloped = "connect+";
        var isEnveloped = span.StartsWith(Enveloped.AsSpan(), StringComparison.OrdinalIgnoreCase);
        if (isEnveloped)
        {
            span = span[Enveloped.Length..];
            if (span.IsEmpty) return false;
        }

        result = new ConnectContentType(span.ToString().ToLowerInvariant(), isEnveloped);
        return true;
    }
}
