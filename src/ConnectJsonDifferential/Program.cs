using System.Text;
using System.Text.Json;
using Google.Protobuf;
using ProtoBuf;
using ProtoBuf.Connect;
using ProtoBuf.ConnectJsonDifferential;

// The only oracle that means anything for a JSON mapping: Google's own formatter, over protoc's own
// C# for the same schema. Every defect this branch has found was a place where both of our ends
// agreed with each other and neither agreed with the protocol, which no self-test can find - and a
// JSON writer is especially prone to it, since ours would round-trip perfectly against ours while
// spelling every key differently from everyone else.
var failures = new List<string>();

SchemaMatchesContracts(failures);

foreach (var (name, ours, theirs) in Cases.All())
{
    Compare(name, ours, theirs, failures);
}

KnownDivergences(failures);

foreach (var failure in failures) Console.Error.WriteLine(failure);
Console.WriteLine(failures.Count == 0
    ? $"JSON differential: {Cases.All().Count()} cases, all agree with Google.Protobuf"
    : $"JSON differential: {failures.Count} FAILURES");
return failures.Count == 0 ? 0 : 1;

// -------------------------------------------------------------------------------------------

/// <summary>
/// The places we knowingly differ from a canonical writer, pinned so they cannot drift silently.
/// </summary>
/// <remarks>
/// Both come from the same root, and it is worth stating plainly: <b>protobuf-net's code-first
/// nullable has no representation in the schema protobuf-net itself generates.</b> An <c>int?</c>
/// emits <c>int32 Maybe = 43</c>, not <c>optional int32</c> - so a peer generating from our .proto
/// gets an implicit-presence field, whose canonical JSON omits a zero. We write the zero anyway:
/// every conformant reader accepts an explicitly-stated default, so writing it costs nothing in
/// interop and keeps the null-versus-zero distinction between two protobuf-net ends. Omitting it
/// would be canonical and lossy; this is interoperable and lossless, which is the better trade.
/// </remarks>
static void KnownDivergences(List<string> failures)
{
    var serializer = ((IJsonModel)JsonModel.Instance).GetJsonSerializer<Shapes>()!;

    Expect("nullable holding the type's default is written, not omitted",
        Write(serializer, new Shapes { Maybe = 0 }), "{\"Maybe\":0}");

    // ...and the canonical writer's answer for the same data, for contrast
    Expect("a canonical writer omits it", JsonFormatter.Default.Format(new Oracle.Shapes { Maybe = 0 }), "{}");

    void Expect(string what, string actual, string expected)
    {
        // through Canonical, because Google's formatter emits "{ }" and ours "{}" - whitespace is
        // not part of the mapping, and pinning it would pin an implementation detail
        if (Canonical(actual) != Canonical(expected)) failures.Add($"[divergence: {what}]\n  got:      {actual}\n  expected: {expected}");
    }
}

// -------------------------------------------------------------------------------------------

void Compare(string name, Shapes ours, Oracle.Shapes theirs, List<string> failures)
{
    var serializer = ((IJsonModel)JsonModel.Instance).GetJsonSerializer<Shapes>()
        ?? throw new InvalidOperationException("no JSON serializer was generated for Shapes");

    // 1. WRITE: our bytes against Google's, compared as trees rather than as text - key *order* is
    //    not part of the mapping, and pinning it would be pinning an implementation detail
    var expected = JsonFormatter.Default.Format(theirs);
    var actual = Write(serializer, ours);
    if (Canonical(actual) != Canonical(expected))
    {
        failures.Add($"[{name}] WRITE\n  ours:   {Canonical(actual)}\n  google: {Canonical(expected)}");
    }

    // 2. READ, from Google's bytes: the direction that catches a writer and reader agreeing with each
    //    other about a spelling nobody else uses
    var readBack = Read(serializer, expected);
    var reEmitted = Write(serializer, readBack);
    if (Canonical(reEmitted) != Canonical(expected))
    {
        failures.Add($"[{name}] READ of Google's JSON lost something\n  in:  {Canonical(expected)}\n  out: {Canonical(reEmitted)}");
    }

    // 3. READ, by Google, of ours: proves our output is not merely equivalent but actually parses
    try
    {
        var parsed = JsonParser.Default.Parse<Oracle.Shapes>(actual);
        if (!parsed.Equals(theirs))
        {
            failures.Add($"[{name}] Google parsed our JSON to a different message\n  {parsed}\n  {theirs}");
        }
    }
    catch (Exception ex)
    {
        failures.Add($"[{name}] Google could not parse our JSON: {ex.Message}\n  {actual}");
    }
}

static string Write(IJsonSerializer<Shapes> serializer, Shapes value)
{
    var buffer = new MemoryStream();
    using (var writer = new Utf8JsonWriter(buffer)) serializer.Write(writer, value);
    return Encoding.UTF8.GetString(buffer.ToArray());
}

static Shapes Read(IJsonSerializer<Shapes> serializer, string json)
{
    var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
    return serializer.Read(ref reader, null);
}

/// <summary>Key order and whitespace are not part of the mapping, so both are normalised away.</summary>
static string Canonical(string json)
{
    using var document = JsonDocument.Parse(json);
    var buffer = new MemoryStream();
    using (var writer = new Utf8JsonWriter(buffer)) WriteCanonical(document.RootElement, writer);
    return Encoding.UTF8.GetString(buffer.ToArray());
}

static void WriteCanonical(JsonElement element, Utf8JsonWriter writer)
{
    switch (element.ValueKind)
    {
        case JsonValueKind.Object:
            writer.WriteStartObject();
            foreach (var property in element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                writer.WritePropertyName(property.Name);
                WriteCanonical(property.Value, writer);
            }
            writer.WriteEndObject();
            return;
        case JsonValueKind.Array:
            writer.WriteStartArray();
            foreach (var item in element.EnumerateArray()) WriteCanonical(item, writer);
            writer.WriteEndArray();
            return;
        default:
            element.WriteTo(writer);
            return;
    }
}

/// <summary>
/// The .proto beside this must still be what protobuf-net derives from the contracts.
/// </summary>
/// <remarks>
/// Without this the comparison is against a schema that has quietly stopped describing our types,
/// and a field-name difference - the exact thing this differential exists to catch - would show up
/// as the two sides agreeing about nothing in particular.
/// </remarks>
static void SchemaMatchesContracts(List<string> failures)
{
    var derived = Normalise(Serializer.GetProto<Shapes>());
    var checkedIn = Normalise(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "oracle.proto")));
    if (derived != checkedIn)
    {
        failures.Add("SCHEMA DRIFT: proto/oracle.proto is no longer Serializer.GetProto<Shapes>().\n"
            + "  derived:\n" + derived + "\n  checked in:\n" + checkedIn);
    }

    // the package and csharp_namespace differ by design, and comments are not schema
    static string Normalise(string proto) => string.Join("\n", proto
        .Split('\n')
        .Select(x => x.Trim())
        .Where(x => x.Length != 0 && !x.StartsWith("//") && !x.StartsWith("package ")
            && !x.StartsWith("option csharp_namespace")));
}
