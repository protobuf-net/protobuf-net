using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.ImplicitIgnore;

// Implicit numbering interacts with the *type-level* exclusion and pinning attributes, and it has to
// be worked out over the whole candidate set - so a member wrongly left in the set does not merely
// serialize itself, it shifts every unpinned tag after it.
//
// Found by the corpus differential on Examples.TestAutoFields+ImplicitPublicPOCO, which numbered
// from 5 where ref-emit numbers from 4: [ProtoPartialIgnore] excluded the member from the read/write
// loop but not from the numbering, so the excluded name still consumed a tag.

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, ImplicitFirstTag = 4)]
[ProtoPartialIgnore(nameof(Excluded.IgnoreIndirect))]
public class Excluded
{
    // pinned, so it keeps tag 1 and does not consume a sequential number
    [ProtoMember(1)] public int Pinned { get; set; }

    // sorts before the two below and is excluded by the *type* attribute, so it must not take tag 4
    public int IgnoreIndirect { get; set; }

    // ...and this one is excluded by the member attribute, which already worked
    [ProtoIgnore] public int IgnoreDirect { get; set; }

    // so these are the only unpinned candidates: 4 and 5, in ordinal name order
    public int ImplicitField;
    public int ImplicitProperty { get; set; }
}

// [ProtoPartialMember] pins from the type exactly as [ProtoMember] pins from the member, so the name
// it names must also be kept out of the sequential run
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, ImplicitFirstTag = 10)]
[ProtoPartialMember(2, nameof(PartiallyPinned.Beta))]
public class PartiallyPinned
{
    public int Alpha { get; set; }
    public int Beta { get; set; }
    public int Gamma { get; set; }
}

public static class ImplicitIgnoreSamples
{
    public static object[] Values =>
    [
        new Excluded(),
        new Excluded { Pinned = 1, IgnoreIndirect = 2, IgnoreDirect = 3, ImplicitField = 4, ImplicitProperty = 5 },
        new PartiallyPinned(),
        new PartiallyPinned { Alpha = 6, Beta = 7, Gamma = 8 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Excluded))]
[ProtoSerializable(typeof(PartiallyPinned))]
public partial class ImplicitIgnoreModel : TypeModel
{
}
