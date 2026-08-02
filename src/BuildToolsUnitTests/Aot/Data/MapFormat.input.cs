using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.MapFormat;

// [ProtoMap] carries per-key and per-value DataFormat, plus DisableMap. The point of this fixture is
// the *reference* output: MapKeyFormat/MapValueFormat feed the key and value wire types that
// MapSerializer takes as separate arguments, and DisableMap drops out of map handling entirely.
[ProtoContract]
public class Nested
{
    [ProtoMember(1)] public int Id { get; set; }
}

[ProtoContract]
public class Maps
{
    // baseline: no attribute at all, for direct comparison
    [ProtoMember(1)] public Dictionary<int, int> Plain { get; set; }

    // the attribute with nothing set should be identical to the baseline
    [ProtoMember(2), ProtoMap] public Dictionary<int, int> Bare { get; set; }

    [ProtoMember(3), ProtoMap(KeyFormat = DataFormat.FixedSize)] public Dictionary<int, int> FixedKey { get; set; }
    [ProtoMember(4), ProtoMap(ValueFormat = DataFormat.FixedSize)] public Dictionary<int, int> FixedValue { get; set; }
    [ProtoMember(5), ProtoMap(KeyFormat = DataFormat.FixedSize, ValueFormat = DataFormat.FixedSize)] public Dictionary<int, int> FixedBoth { get; set; }

    [ProtoMember(6), ProtoMap(KeyFormat = DataFormat.ZigZag)] public Dictionary<int, int> ZigZagKey { get; set; }
    [ProtoMember(7), ProtoMap(ValueFormat = DataFormat.ZigZag)] public Dictionary<int, int> ZigZagValue { get; set; }

    // 64-bit, to see whether the width comes from the member as it does for a scalar
    [ProtoMember(8), ProtoMap(KeyFormat = DataFormat.FixedSize, ValueFormat = DataFormat.FixedSize)] public Dictionary<long, long> FixedWide { get; set; }

    // a string key: the format has nothing to select, so it should be ignored
    [ProtoMember(9), ProtoMap(KeyFormat = DataFormat.FixedSize)] public Dictionary<string, int> StringKey { get; set; }

    // a message value with Group: on a scalar that changes the write only
    [ProtoMember(10), ProtoMap(ValueFormat = DataFormat.Group)] public Dictionary<int, Nested> GroupValue { get; set; }

    // map handling off: duplicates throw instead of replacing, and the schema would say "repeated"
    [ProtoMember(11), ProtoMap(DisableMap = true)] public Dictionary<int, int> NoMap { get; set; }

    // DisableMap on a shape that would not be a valid map anyway
    [ProtoMember(12), ProtoMap(DisableMap = true)] public Dictionary<int, Nested> NoMapMessage { get; set; }

    // composed with the collection options we already emit
    [ProtoMember(13, OverwriteList = true), ProtoMap(KeyFormat = DataFormat.FixedSize)] public Dictionary<int, int> Overwrite { get; set; }
}

public static class MapFormatSamples
{
    public static object[] Values =>
    [
        new Maps(),
        new Maps { Plain = new() { [1] = 2 }, Bare = new() { [3] = 4 } },
        new Maps { FixedKey = new() { [5] = 6 }, FixedValue = new() { [7] = 8 }, FixedBoth = new() { [9] = 10 } },
        new Maps { ZigZagKey = new() { [-1] = 2 }, ZigZagValue = new() { [3] = -4 } },
        new Maps { FixedWide = new() { [11L] = 12L }, StringKey = new() { ["k"] = 13 } },
        new Maps { GroupValue = new() { [14] = new Nested { Id = 15 } } },
        new Maps { NoMap = new() { [16] = 17 }, NoMapMessage = new() { [18] = new Nested { Id = 19 } } },
        new Maps { Overwrite = new() { [20] = 21 } },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Maps))]
public partial class MapFormatModel : TypeModel
{
}
