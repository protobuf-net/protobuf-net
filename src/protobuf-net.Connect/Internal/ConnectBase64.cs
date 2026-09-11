using System;

namespace ProtoBuf.Connect.Internal
{
    /// <summary>
    /// Base64 as the Connect protocol spells it: standard alphabet, <b>no padding</b>.
    /// </summary>
    /// <remarks>
    /// The protocol's error-detail values are unpadded, and readers are entitled to be strict about it -
    /// connect-go decodes with <c>base64.RawStdEncoding</c>, which rejects a trailing <c>=</c> outright
    /// ("is not valid unpadded base64-encoding"). <see cref="Convert.ToBase64String(byte[])"/> pads, and
    /// <see cref="System.Text.Json.Utf8JsonWriter.WriteBase64String(System.ReadOnlySpan{byte}, System.ReadOnlySpan{byte})"/>
    /// does too, so neither can be used directly.
    /// <para>
    /// Our own reader re-pads before decoding and so accepts either form. That asymmetry is deliberate
    /// and is the usual rule: strict in what we send, lenient in what we accept.
    /// </para>
    /// </remarks>
    internal static class ConnectBase64
    {
        /// <summary>Encodes without padding.</summary>
        public static string Encode(ReadOnlySpan<byte> value)
        {
            var text = Convert.ToBase64String(value);

            // at most two '=' can ever be appended
            var end = text.Length;
            while (end > 0 && text[end - 1] == '=') end--;

            return end == text.Length ? text : text.Substring(0, end);
        }
    }
}
