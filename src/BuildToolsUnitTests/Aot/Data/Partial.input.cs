using ProtoBuf;
using ProtoBuf.Meta;
using System.Runtime.Serialization;

namespace AotFixtures.Partial;

// [ProtoPartialMember] is [ProtoMember] applied to a member by name, from the type - the point being
// that the member may be in a generated half of a partial class you cannot decorate. [ProtoPartialIgnore]
// is [ProtoIgnore] by the same route.
[ProtoContract]
[ProtoPartialMember(1, nameof(Described.Id))]
[ProtoPartialMember(2, nameof(Described.Name))]
[ProtoPartialMember(3, nameof(Described.Fixed), DataFormat = DataFormat.FixedSize)]
[ProtoPartialMember(4, nameof(Described.Always), IsRequired = true)]
[ProtoPartialMember(5, nameof(Described.Values), IsPacked = true)]
// OverwriteList was refused here until MetaType stopped reading it from the wrong attribute map -
// it read the member's own [ProtoMember], which is necessarily null in that branch, so it was
// silently ignored. Both paths honour it now, and the differential compares the merge rather than
// taking it on trust: RepeatedFieldOccurrencesMergeIdentically is what exercises it.
[ProtoPartialMember(6, nameof(Described.Replaced), OverwriteList = true)]
public class Described
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Fixed { get; set; }
    public int Always { get; set; }
    public int[] Values { get; set; }
    public int[] Replaced { get; set; }
}

// the member's own [ProtoMember] wins: NormalizeProtoMember only consults the partial list when the
// member did not pin a tag itself.
// PBN0008 is suppressed deliberately - describing a member twice *is* what this shape is for, and
// the analyzer is right that it is a mistake in ordinary code. Pinning the precedence needs a
// contradiction to resolve, so there is no version of this test that the analyzer would allow.
#pragma warning disable PBN0008 // The underlying member is described multiple times
[ProtoContract]
[ProtoPartialMember(9, nameof(Contested.Pinned))]
[ProtoPartialMember(2, nameof(Contested.FromPartial))]
public class Contested
{
    [ProtoMember(1)] public int Pinned { get; set; }
    public int FromPartial { get; set; }

    // named by neither, so it is not serialized at all
    public int Undeclared { get; set; }
}
#pragma warning restore PBN0008

// [ProtoPartialIgnore] excludes by name, and MetaType tests it before anything else - so it beats
// even a [ProtoMember] the member declares itself. PBN0010 suppressed for the same reason as
// PBN0008 above: the contradiction is the thing under test.
#pragma warning disable PBN0010 // The member is marked to be ignored; additional annotations will be ignored
[ProtoContract]
[ProtoPartialIgnore(nameof(Excluded.Hidden))]
[ProtoPartialIgnore(nameof(Excluded.AlsoHidden))]
public class Excluded
{
    [ProtoMember(1)] public int Kept { get; set; }
    [ProtoMember(2)] public int Hidden { get; set; }
    public int AlsoHidden { get; set; }
}
#pragma warning restore PBN0010

// against [DataMember]: the partial member is read in the ProtoBuf family branch, which runs first
[ProtoContract]
[DataContract]
[ProtoPartialMember(7, nameof(Mixed.Both))]
public class Mixed
{
    [DataMember(Order = 1)] public int Both { get; set; }
    [DataMember(Order = 2)] public int OrderOnly { get; set; }
}

// OverwriteList on a partial member: silently ignored by MetaType until the `attrib`/`ppma` mix-up
// was fixed, so the generator refused it rather than merge differently from ref-emit. Both honour it
// now, and the differential compares the merge behaviour rather than taking it on trust.
public static class PartialSamples
{
    public static object[] Values =>
    [
        new Described(),
        new Described { Id = 1, Name = "x", Fixed = 2, Always = 3, Values = [4, 5] },
        new Described { Always = 0 },                   // required: written even at the default
        new Contested { Pinned = 6, FromPartial = 7 },
        new Excluded { Kept = 8, Hidden = 9, AlsoHidden = 10 },
        new Mixed { Both = 11, OrderOnly = 12 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Described))]
[ProtoSerializable(typeof(Contested))]
[ProtoSerializable(typeof(Excluded))]
[ProtoSerializable(typeof(Mixed))]
public partial class PartialModel : TypeModel
{
}
