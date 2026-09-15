using System.Text;
using System.Text.Json;

namespace ProtoBuf.ConnectJsonDifferential;

/// <summary>Comparison helpers shared by the paired cases and the breadth sweep.</summary>
internal static class Json
{
    /// <summary>
    /// Normalises a document so that two writers can be compared on what they <em>mean</em>.
    /// </summary>
    /// <remarks>
    /// Key order, whitespace and escaping choices are all outside the mapping: Google's formatter
    /// emits <c>{ }</c> where <c>Utf8JsonWriter</c> emits <c>{}</c>, and the two escape non-ASCII
    /// differently, both validly. Comparing text would pin those as if they were the protocol.
    /// </remarks>
    public static string Canonical(string json)
    {
        using var document = JsonDocument.Parse(json);
        var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer)) Write(document.RootElement, writer);
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void Write(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    Write(property.Value, writer);
                }
                writer.WriteEndObject();
                return;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) Write(item, writer);
                writer.WriteEndArray();
                return;
            // a NUMBER is normalised through double, because the two writers spell the same value
            // differently and neither spelling is the protocol: Utf8JsonWriter emits 5E-324 where
            // Google emits 5e-324, and JSON does not distinguish them. Comparing the raw text would
            // pin an exponent's capitalisation as if it mattered.
            case JsonValueKind.Number:
                writer.WriteNumberValue(element.GetDouble());
                return;
            default:
                element.WriteTo(writer);
                return;
        }
    }
}
