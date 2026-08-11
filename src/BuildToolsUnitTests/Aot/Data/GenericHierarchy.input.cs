using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.GenericHierarchy;

// A generic base *inside* a hierarchy. The open definition is never a contract itself - the includes
// name closed constructions - but its own [ProtoInclude] list is shared by every construction, so
// each one sees includes that belong to a sibling.
//
// That is legal and unambiguous, because a construction only ever matches the includes that actually
// derive from it. protobuf-net filters them (MetaType: `if (IsValidSubType(knownType))`); we used to
// refuse the whole list on the first non-deriving entry, which left both leaves *unlinked* - and an
// unlinked contract is emitted standalone, so the enclosing hierarchy vanished from the wire
// silently rather than failing loudly. Found by the corpus differential on Examples.Issues.SO9408133.

[ProtoContract]
public class Ship { [ProtoMember(1)] public int Foo { get; set; } }

[ProtoContract]
public class Crate { [ProtoMember(1)] public string Bar { get; set; } }

[ProtoContract]
[ProtoInclude(1, typeof(PlainNode))]
[ProtoInclude(3, typeof(Holder<Ship>))]
[ProtoInclude(4, typeof(Holder<Crate>))]
public class Node { }

[ProtoContract]
public class PlainNode : Node { [ProtoMember(1)] public int N { get; set; } }

// Both includes carry tag 1, which is fine: Holder<Ship> matches only ShipHolder and Holder<Crate>
// only CrateHolder, so neither construction ever sees two sub-types at one tag - and ref-emit
// serializes it happily. PBN0003 used to report this as a build *error*, counting tags across the
// whole include list without noticing that a generic declaring type shares that list between its
// constructions; the numbering space is now split per construction, so this compiles clean.
[ProtoContract]
[ProtoInclude(1, typeof(ShipHolder))]
[ProtoInclude(1, typeof(CrateHolder))]
public class Holder<T> : Node { }

[ProtoContract]
public class ShipHolder : Holder<Ship> { [ProtoMember(1)] public Ship Value { get; set; } }

[ProtoContract]
public class CrateHolder : Holder<Crate> { [ProtoMember(1)] public Crate Value { get; set; } }

public static class GenericHierarchySamples
{
    public static object[] Values =>
    [
        new Node(),
        new PlainNode { N = 1 },
        new ShipHolder { Value = new Ship { Foo = 2 } },
        new CrateHolder { Value = new Crate { Bar = "x" } },
        new ShipHolder(),
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Node))]
public partial class GenericHierarchyModel : TypeModel
{
}
