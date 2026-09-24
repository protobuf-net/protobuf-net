using ProtoBuf;

namespace AotFixtures.Implicit;

// [ProtoContract(ImplicitFields = ...)] infers members by convention instead of by attribute:
// AllPublic takes any public member, AllFields any field public or not. Tags are then assigned by
// sorting on the member *name* (ordinal) and numbering from ImplicitFirstTag.
//
// Declaration order below is deliberately not name order, since that is the whole question.
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class AllPublic
{
    public int Zebra { get; set; }
    public string Apple { get; set; }
    public int Mango;

    // "public" means a public *getter*; the setter's accessibility is not consulted. Not exercised
    // here because ref-emit's compiled path refuses a non-public setter outright and would decline
    // the whole model - NonPublicSetter.input.cs covers that interaction instead.

    // not public, so not taken
    internal int Hidden { get; set; }
    private int _secret;

    public int Secret() => _secret;
}

[ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
public class AllFields
{
    public int Zebra;
    public int Apple;

    // a property is not a field, so it is not taken even though it is public
    public int Ignored { get; set; }
}

// numbering starts where told
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, ImplicitFirstTag = 10)]
public class FirstTag
{
    public int Beta { get; set; }
    public int Alpha { get; set; }
}

// an explicit [ProtoMember] pins its own tag; the rest are numbered around it
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class Mixed
{
    [ProtoMember(5)] public int Pinned { get; set; }
    public int Zulu { get; set; }
    public int Alpha { get; set; }
}

// ...and [ProtoIgnore] still excludes
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class Ignoring
{
    public int Kept { get; set; }
    [ProtoIgnore] public int Dropped { get; set; }
}

public static class ImplicitSamples
{
    public static object[] Values =>
    [
        new AllPublic(),
        new AllPublic { Zebra = 1, Apple = "a", Mango = 2 },
        new AllFields { Zebra = 3, Apple = 11 },
        new FirstTag { Alpha = 4, Beta = 5 },
        new Mixed { Pinned = 6, Zulu = 7, Alpha = 8 },
        new Ignoring { Kept = 9, Dropped = 10 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(AllPublic))]
[ProtoSerializable(typeof(AllFields))]
[ProtoSerializable(typeof(FirstTag))]
[ProtoSerializable(typeof(Mixed))]
[ProtoSerializable(typeof(Ignoring))]
public partial class ImplicitModel : ProtoBuf.Meta.TypeModel
{
}
