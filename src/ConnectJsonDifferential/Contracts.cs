using System.Collections.Generic;
using ProtoBuf;

namespace ProtoBuf.ConnectJsonDifferential;

/// <summary>
/// The code-first half of the differential: ordinary protobuf-net contracts, written the way a
/// consumer would write them.
/// </summary>
/// <remarks>
/// Deliberately <b>not</b> shaped to flatter the JSON mapping. Member names are PascalCase because
/// that is what people write in C#, which is exactly the input the "lowerCamelCase" summary of
/// canonical JSON gets wrong - see <c>Shapes.UserName</c>, whose JSON key is <c>UserName</c>.
/// </remarks>
[ProtoContract]
public class Shapes
{
    [ProtoMember(1)] public int Count { get; set; }
    [ProtoMember(2)] public long BigNumber { get; set; }
    [ProtoMember(3)] public uint Unsigned { get; set; }
    [ProtoMember(4)] public ulong BigUnsigned { get; set; }
    [ProtoMember(5)] public bool Flag { get; set; }
    [ProtoMember(6)] public float Ratio { get; set; }
    [ProtoMember(7)] public double Precise { get; set; }
    [ProtoMember(8)] public string UserName { get; set; }
    [ProtoMember(9)] public byte[] Blob { get; set; }
    [ProtoMember(10)] public Shade Colour { get; set; }
    [ProtoMember(11)] public Leaf Child { get; set; }

    [ProtoMember(20)] public List<int> Many { get; set; } = new();
    [ProtoMember(21)] public List<string> Words { get; set; } = new();
    [ProtoMember(22)] public List<Leaf> Leaves { get; set; } = new();
    [ProtoMember(23)] public List<Shade> Shades { get; set; } = new();

    [ProtoMember(30)] public Dictionary<string, int> Tally { get; set; } = new();
    [ProtoMember(31)] public Dictionary<int, string> Names { get; set; } = new();
    [ProtoMember(32)] public Dictionary<string, Leaf> Nested { get; set; } = new();

    // an array, and a getter-only collection - both idiomatic, and both needing the reader to do
    // something other than "build a List and assign it"
    [ProtoMember(41)] public string[] Tags { get; set; }
    [ProtoMember(42)] public List<int> Fixed { get; } = new();

    // explicit presence: a nullable scalar is written even when it holds the type's default, which
    // is the one case where "omit defaults" must NOT apply
    [ProtoMember(43)] public int? Maybe { get; set; }

    // the compatibility-level group, which is where code-first protobuf-net and canonical JSON are
    // furthest apart: at level 300 a DateTime IS a google.protobuf.Timestamp, so its JSON is an
    // RFC 3339 string - nothing System.Text.Json would ever produce for a DateTime
    [ProtoMember(50)] public Temporal Times { get; set; }

    // the pinned-name case: the schema says `pinned_name`, so the JSON key is `pinnedName` - and a
    // reader must accept both spellings. Everything above is unpinned, where the two coincide
    [ProtoMember(40, Name = "pinned_name")] public string Pinned { get; set; }
}

[ProtoContract]
[CompatibilityLevel(CompatibilityLevel.Level300)]
public class Temporal
{
    [ProtoMember(1)] public DateTime At { get; set; }
    [ProtoMember(2)] public TimeSpan Took { get; set; }
}

[ProtoContract]
public class Leaf
{
    [ProtoMember(1)] public string Label { get; set; }
    [ProtoMember(2)] public int Weight { get; set; }
}

[ProtoContract]
public enum Shade
{
    // the zero must exist and is the default; protojson omits a field holding it
    [ProtoEnum] Unknown = 0,
    Green = 1,
    Blue = 2,
}

[ProtoModel]
[ProtoSerializable(typeof(Shapes))]
public partial class JsonModel : ProtoBuf.Meta.TypeModel
{
}
