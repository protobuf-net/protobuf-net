using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.ListOptions;

// IsPacked and OverwriteList are pure features flags on a collection: the first *omits*
// OptionPackedDisabled, the second *adds* OptionClearCollection, and they compose.
// DataFormat and IsRequired are not supported yet - they change the emitted shape, not just features.
[ProtoContract]
public class Options
{
    [ProtoMember(1)] public int[] Default { get; set; }
    [ProtoMember(2, IsPacked = true)] public int[] Packed { get; set; }
    [ProtoMember(3, OverwriteList = true)] public List<int> Overwrite { get; set; }
    [ProtoMember(4, IsPacked = true, OverwriteList = true)] public List<int> PackedOverwrite { get; set; }

    // explicitly false is the same as the default
    [ProtoMember(5, IsPacked = false)] public int[] NotPacked { get; set; }

    [ProtoMember(6, IsPacked = true)] public double[] PackedDouble { get; set; }
}

public static class ListOptionsSamples
{
    public static object[] Values =>
    [
        new Options(),
        new Options { Default = [1, 2, 3], Packed = [1, 2, 3] },
        new Options { Overwrite = [4, 5], PackedOverwrite = [6, 7] },
        new Options { NotPacked = [8], PackedDouble = [1.5d, 2.5d] },
        new Options { Packed = [] },                     // empty, packed
        new Options { Default = [0], Packed = [0] },     // zero elements
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Options))]
public partial class ListOptionsModel : TypeModel
{
}
