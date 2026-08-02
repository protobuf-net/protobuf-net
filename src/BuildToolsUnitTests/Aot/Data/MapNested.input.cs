using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.MapNested;

// protobuf-net refuses a nested collection almost everywhere - a List<List<int>> throws - but
// exempts dictionaries specifically (RepeatedSerializerStub.TestIfNestedNotSupported), so a
// Dictionary<K, List<V>> is legal. Note such a shape is *not* a valid protobuf map, so it also picks
// up OptionFailOnDuplicateKey.
[ProtoContract]
public class Nested
{
    [ProtoMember(1)] public Dictionary<int, List<int>> Lists { get; set; }
    [ProtoMember(2)] public Dictionary<long, long[]> Arrays { get; set; }
    // a nested *map* value is refused - see Diagnostics/MapNestedUnsupported.input.cs

    // a float key is not valid for a protobuf map either, and combines with the above
    [ProtoMember(4)] public Dictionary<float, List<int>> FloatKeyed { get; set; }
}

public static class MapNestedSamples
{
    public static object[] Values =>
    [
        new Nested(),
        new Nested { Lists = new() { [1] = [2, 3] } },
        new Nested { Arrays = new() { [4L] = [5L, 6L] } },
        new Nested { FloatKeyed = new() { [1.5f] = [7] } },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Nested))]
public partial class MapNestedModel : TypeModel
{
}
