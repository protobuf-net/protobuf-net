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

// Regenerating proto/wide.proto: DUMP_WIDE=1 prints what protobuf-net derives, which is then
// checked in with only the package and csharp_namespace changed. The drift check below compares the
// two on every run, so the file cannot quietly stop describing the contracts beside it.
if (Environment.GetEnvironmentVariable("DUMP_WIDE") == "1")
{
    Console.WriteLine(ProtoBuf.Serializer.GetProto<ProtoBuf.ConnectJsonDifferential.Wide>());
    return 0;
}

SchemaMatchesContracts(failures);

foreach (var (name, ours, theirs) in Cases.All())
{
    Compare(name, ours, theirs, failures);
}

Boundary(failures);
Sweep.Run(JsonModel.Instance, failures);
KnownDivergences(failures);

foreach (var failure in failures) Console.Error.WriteLine(failure);
Console.WriteLine(failures.Count == 0
    ? $"JSON differential: {Cases.All().Count()} paired + {Sweep.Count} breadth cases agree with Google.Protobuf; the subset boundary holds"
    : $"JSON differential: {failures.Count} FAILURES");
return failures.Count == 0 ? 0 : 1;

// -------------------------------------------------------------------------------------------

/// <summary>
/// The JSON surface is a strict subset of the binary one, and this pins exactly where the line is.
/// </summary>
/// <remarks>
/// Asserted rather than eyeballed, in both directions. A refusal that quietly stopped refusing would
/// mean emitting a mapping for a shape that has none - the silent-interop-break failure this whole
/// exercise exists to avoid - and a supported shape that quietly started being refused would mean
/// losing JSON for something that works, with only an Info diagnostic to say so.
/// </remarks>
static void Boundary(List<string> failures)
{
    var model = (IJsonModel)ProbeModel.Instance;

    Refused<HasHashSet>("a collection the reader cannot construct");
    Refused<HasQueue>("likewise");
    Refused<HasLevel200DateTime>("a level-200 DateTime is a protobuf-net message, not a Timestamp");
    Refused<HasBoolKeyedMap>("bool is not a protobuf map key");
    Refused<HasEnumKeyedMap>("nor is an enum - protoc rejects the schema protobuf-net generates");
    Refused<Base>("[ProtoInclude] sub-type framing has no JSON form");
    Refused<Derived>("likewise");
    Refused<Holder>("and it cascades to anything reaching one");

    Supports<Supported>("an array, a nullable and a getter-only List");

    // and the binary codec must still serve every one of them: narrowing the JSON surface must not
    // narrow the other, which is the thing that would make this a regression rather than a subset
    foreach (var type in new[]
    {
        typeof(HasHashSet), typeof(HasQueue), typeof(HasLevel200DateTime),
        typeof(HasBoolKeyedMap), typeof(HasEnumKeyedMap), typeof(Holder), typeof(Supported),
    })
    {
        if (!ProbeModel.Instance.CanSerialize(type))
        {
            failures.Add($"[boundary] {type.Name} lost its BINARY serializer, which the JSON pass must never do");
        }
    }

    void Refused<T>(string why)
    {
        if (model.GetJsonSerializer<T>() is not null)
        {
            failures.Add($"[boundary] {typeof(T).Name} now has a JSON serializer, but should not: {why}");
        }
    }

    void Supports<T>(string what)
    {
        if (model.GetJsonSerializer<T>() is null)
        {
            failures.Add($"[boundary] {typeof(T).Name} lost its JSON serializer; it covers {what}");
        }
    }
}



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
        if (Json.Canonical(actual) != Json.Canonical(expected)) failures.Add($"[divergence: {what}]\n  got:      {actual}\n  expected: {expected}");
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
    if (Json.Canonical(actual) != Json.Canonical(expected))
    {
        failures.Add($"[{name}] WRITE\n  ours:   {Json.Canonical(actual)}\n  google: {Json.Canonical(expected)}");
    }

    // 2. READ, from Google's bytes: the direction that catches a writer and reader agreeing with each
    //    other about a spelling nobody else uses
    var readBack = Read(serializer, expected);
    var reEmitted = Write(serializer, readBack);
    if (Json.Canonical(reEmitted) != Json.Canonical(expected))
    {
        failures.Add($"[{name}] READ of Google's JSON lost something\n  in:  {Json.Canonical(expected)}\n  out: {Json.Canonical(reEmitted)}");
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
    Check("oracle.proto", Serializer.GetProto<Shapes>());
    Check("wide.proto", Serializer.GetProto<Wide>());

    void Check(string file, string derivedProto)
    {
        var derived = Normalise(derivedProto);
        var checkedIn = Normalise(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, file)));
        if (derived != checkedIn)
        {
            failures.Add($"SCHEMA DRIFT: proto/{file} is no longer what protobuf-net derives.\n"
                + "  derived:\n" + derived + "\n  checked in:\n" + checkedIn);
        }
    }

    // the package and csharp_namespace differ by design, and comments are not schema
    static string Normalise(string proto) => string.Join("\n", proto
        .Split('\n')
        .Select(x => x.Trim())
        .Where(x => x.Length != 0 && !x.StartsWith("//") && !x.StartsWith("package ")
            && !x.StartsWith("option csharp_namespace")));
}
