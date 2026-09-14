using System.Text;
using System.Text.Json;
using ProtoBuf;
using ProtoBuf.Connect;

// Does the generated canonical-JSON surface survive ILC? Everything else about this feature is
// tested on a JIT runtime, where a reflective fallback would still work and hide a gap.
var model = (IJsonModel)Model.Instance;
var serializer = model.GetJsonSerializer<Payload>();
if (serializer is null) return Fail("no JSON serializer was generated");

var original = new Payload
{
    Count = 7,
    Big = 9007199254740993L,
    Name = "smoke",
    Blob = new byte[] { 1, 2, 3 },
    Level = Level.High,
    Child = new Nested { Note = "inner" },
    Many = { 1, 2, 3 },
    Tally = { ["a"] = 1 },
};

var buffer = new MemoryStream();
using (var writer = new Utf8JsonWriter(buffer)) serializer.Write(writer, original);
var json = Encoding.UTF8.GetString(buffer.ToArray());

var reader = new Utf8JsonReader(buffer.ToArray());
var round = serializer.Read(ref reader, null);

if (round.Count != original.Count) return Fail($"Count: {round.Count}");
if (round.Big != original.Big) return Fail($"Big: {round.Big}");
if (round.Name != original.Name) return Fail($"Name: {round.Name}");
if (round.Blob is not [1, 2, 3]) return Fail("Blob");
if (round.Level != Level.High) return Fail($"Level: {round.Level}");
if (round.Child?.Note != "inner") return Fail("Child");
if (round.Many is not [1, 2, 3]) return Fail("Many");
if (round.Tally.Count != 1 || round.Tally["a"] != 1) return Fail("Tally");

// the two rules a POCO serializer gets wrong, asserted rather than assumed: an int64 is a string,
// and an enum is its name
if (!json.Contains("\"Big\":\"9007199254740993\"")) return Fail($"int64 is not a string: {json}");
if (!json.Contains("\"Level\":\"High\"")) return Fail($"enum is not a name: {json}");

// ...and the binary codec on the SAME model, because otherwise ILC simply trims it and a zero
// warning count says only that the binary path was never reached
var binary = new MemoryStream();
Model.Instance.Serialize(binary, original);
binary.Position = 0;
var fromBinary = Model.Instance.Deserialize<Payload>(binary);
if (fromBinary.Big != original.Big || fromBinary.Child?.Note != "inner") return Fail("binary round-trip");

Console.WriteLine($"AotConnectJsonSmoke: ok - {json}");
return 0;

static int Fail(string what)
{
    Console.Error.WriteLine("AotConnectJsonSmoke FAILED: " + what);
    return 1;
}

[ProtoContract]
public class Payload
{
    [ProtoMember(1)] public int Count { get; set; }
    [ProtoMember(2)] public long Big { get; set; }
    [ProtoMember(3)] public string Name { get; set; }
    [ProtoMember(4)] public byte[] Blob { get; set; }
    [ProtoMember(5)] public Level Level { get; set; }
    [ProtoMember(6)] public Nested Child { get; set; }
    [ProtoMember(7)] public List<int> Many { get; } = new();
    [ProtoMember(8)] public Dictionary<string, int> Tally { get; } = new();
}

[ProtoContract]
public class Nested
{
    [ProtoMember(1)] public string Note { get; set; }
}

public enum Level { None = 0, Low = 1, High = 2 }

[ProtoModel]
[ProtoSerializable(typeof(Payload))]
public partial class Model : ProtoBuf.Meta.TypeModel { }
