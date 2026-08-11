using ProtoBuf;
using ProtoBuf.Meta;
using System.Runtime.Serialization;

namespace AotFixtures.ContractOptions;

// IsGroup moves the contract's *own* features from WireTypeString to WireTypeStartGroup
// (MetaType.GetFeatures). Note this is the contract's features, not the member's - a member
// selects its own wire type through DataFormat, which is a separate thing.
[ProtoContract(IsGroup = true)]
public class Grouped
{
    [ProtoMember(1)] public int Id { get; set; }
    [ProtoMember(2)] public string Name { get; set; }
}

// IgnoreUnknownSubTypes reaches TypeSerializer as assertKnownType: false, and that flag guards
// exactly one thing: the ThrowUnexpectedSubtype call. So this is the same omission `sealed` gets,
// asked for explicitly on a type that is not sealed.
[ProtoContract(IgnoreUnknownSubTypes = true)]
public class Lenient
{
    [ProtoMember(1)] public int Id { get; set; }
}

// ...and with a hierarchy, where the is-chain stays but the else-throw goes
[ProtoContract(IgnoreUnknownSubTypes = true)]
[ProtoInclude(10, typeof(LenientDerived))]
public class LenientBase
{
    [ProtoMember(1)] public int Id { get; set; }
}

[ProtoContract]
public class LenientDerived : LenientBase
{
    [ProtoMember(2)] public string Extra { get; set; }
}

// UseProtoMembersOnly narrows the attribute family to ProtoBuf, so the [DataMember] orders below
// stop applying and only the [ProtoMember] survives - the same narrowing ImplicitFields performs.
[ProtoContract(UseProtoMembersOnly = true)]
[DataContract]
public class ProtoOnly
{
    [ProtoMember(3)] public int Tagged { get; set; }
    [DataMember(Order = 1)] public int Ignored { get; set; }
    [DataMember(Order = 2)] public string AlsoIgnored { get; set; }
}

// for contrast: the same shape without the option, where [DataMember] does supply orders
[ProtoContract]
[DataContract]
public class BothFamilies
{
    [ProtoMember(3)] public int Tagged { get; set; }
    [DataMember(Order = 1)] public int Ordered { get; set; }
}

public static class ContractOptionsSamples
{
    public static object[] Values =>
    [
        new Grouped(),
        new Grouped { Id = 1, Name = "x" },
        new Lenient(),
        new Lenient { Id = 2 },
        new LenientBase { Id = 3 },
        new LenientDerived { Id = 4, Extra = "y" },
        new ProtoOnly { Tagged = 5, Ignored = 6, AlsoIgnored = "z" },
        new BothFamilies { Tagged = 7, Ordered = 8 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Grouped))]
[ProtoSerializable(typeof(Lenient))]
[ProtoSerializable(typeof(LenientBase))]
[ProtoSerializable(typeof(ProtoOnly))]
[ProtoSerializable(typeof(BothFamilies))]
public partial class ContractOptionsModel : TypeModel
{
}
