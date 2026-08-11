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

// Neither option means anything on a member that is not a collection - ComposeListFeatures is only
// reached from the repeated and map paths - so protobuf-net silently ignores them. The one exception
// is OverwriteList on a "bytes" member, which is a scalar here but still reaches BlobSerializer's
// overwriteList: it selects AppendBytes(default) over AppendBytes(existing), i.e. replace over append.
[ProtoContract]
public class NotACollection
{
    [ProtoMember(1, IsPacked = true)] public int PackedScalar { get; set; }
    [ProtoMember(2, OverwriteList = true)] public int OverwriteScalar { get; set; }
    [ProtoMember(3, IsPacked = true, OverwriteList = true)] public string BothOnString { get; set; }

    [ProtoMember(4)] public byte[] AppendedBytes { get; set; }
    [ProtoMember(5, OverwriteList = true)] public byte[] OverwrittenBytes { get; set; }
    [ProtoMember(6, IsPacked = true)] public byte[] PackedBytes { get; set; }
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

        new NotACollection(),
        new NotACollection { PackedScalar = 1, OverwriteScalar = 2, BothOnString = "x" },
        new NotACollection { AppendedBytes = [1, 2], OverwrittenBytes = [3, 4], PackedBytes = [5] },
        new NotACollection { AppendedBytes = [], OverwrittenBytes = [] }, // empty is not null
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Options))]
[ProtoSerializable(typeof(NotACollection))]
public partial class ListOptionsModel : TypeModel
{
}
