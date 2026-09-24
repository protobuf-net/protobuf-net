using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.Unlinked;

// Deriving from a type that does not [ProtoInclude] us. protobuf-net binds only the type's own
// declared members and ignores the base entirely - uniformly, whether the base is a contract or not,
// and whether or not it includes some *other* type. So the inherited members are silently dropped;
// that is a real surprise, but it is the shipped analyzer's PBN0013 to report (and it does), not a
// reason for the generator to decline the contract.
//
// Verified against RuntimeTypeModel rather than assumed: every shape below round-trips with the
// base's value reset to its default, and the model binds only the derived member.

[ProtoContract]
public class ContractBase
{
    [ProtoMember(1)] public int FromBase { get; set; }
}

// (a) a contract base with no link: FromBase is not written
[ProtoContract]
public class Derived : ContractBase
{
    [ProtoMember(2)] public int FromDerived { get; set; }
}

// (b) the derived type re-uses the base's field number, which is legal precisely because the base's
// members are not in play at all
[ProtoContract]
public class Reuses : ContractBase
{
    [ProtoMember(1)] public int Mine { get; set; }
}

// (c) a plain base, carrying no protobuf attributes
public class PlainBase
{
    public int Ignored { get; set; }
}

[ProtoContract]
public class FromPlain : PlainBase
{
    [ProtoMember(1)] public string Name { get; set; }
}

// (d) the base includes a *sibling*, so it is a hierarchy - just not one we are part of
[ProtoContract]
[ProtoInclude(10, typeof(Sibling))]
public class ForkedBase
{
    [ProtoMember(1)] public int FromBase { get; set; }
}

[ProtoContract]
public class Sibling : ForkedBase
{
    [ProtoMember(2)] public int Linked { get; set; }
}

[ProtoContract]
public class Unlinked : ForkedBase
{
    [ProtoMember(3)] public int NotLinked { get; set; }
}

public static class UnlinkedSamples
{
    public static object[] Values =>
    [
        new Derived(),
        new Derived { FromBase = 7, FromDerived = 9 },
        new Reuses { FromBase = 7, Mine = 9 },
        new FromPlain { Ignored = 7, Name = "x" },
        new Unlinked { FromBase = 7, NotLinked = 9 },
        new Sibling { FromBase = 7, Linked = 9 },
        new ForkedBase { FromBase = 7 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Derived))]
[ProtoSerializable(typeof(Reuses))]
[ProtoSerializable(typeof(FromPlain))]
[ProtoSerializable(typeof(Unlinked))]
[ProtoSerializable(typeof(ForkedBase))]
public partial class UnlinkedModel : TypeModel
{
}
