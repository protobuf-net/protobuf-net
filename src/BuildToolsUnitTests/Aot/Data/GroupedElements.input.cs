using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.GroupedElements;

// DataFormat.Group on a *collection* or *map* member is not the same thing as on a scalar. On a
// scalar it changes the write only (WriteGroup for WriteMessage); on a collection it lands in the
// element features as WireTypeStartGroup, so both directions change and the element carries a
// group header rather than a length prefix.
//
// Found by the corpus differential, where it was the largest single cluster.

[ProtoContract]
public class Item
{
    [ProtoMember(1)] public string Name { get; set; }
    [ProtoMember(2)] public int Count { get; set; }
}

[ProtoContract]
public class Grouped
{
    [ProtoMember(1, DataFormat = DataFormat.Group)] public List<Item> Items { get; set; }
    [ProtoMember(2, DataFormat = DataFormat.Group)] public Item[] Array { get; set; }

    // for contrast: the same shapes at the default format stay length-prefixed
    [ProtoMember(3)] public List<Item> Plain { get; set; }

    // note there is no grouped *scalar* collection here: protobuf-net refuses that outright, on
    // both paths - see Diagnostics/GroupedScalarList.input.cs

    // a grouped sub-message member, which is the shape that changes on the write only
    [ProtoMember(5, DataFormat = DataFormat.Group)] public Item Single { get; set; }
}

// The shape gap B14 is about, and the reason it needs its own contract: `Grouped` above is
// blocked from measure-first by its grouped COLLECTION members whatever we do about the unary
// case, so it cannot show whether a unary grouped sub-message is measurable. This one's only
// non-default member is a unary group, so it must come out MEASURABLE - a grouped sub-message
// carries no length prefix, so its size is the body plus two folded tags, with no varint measure
// at all. Before B14 the contract lost its Measure_ entirely, which meant reaching for groups
// *because* they are cheap to write put the whole tree on the write-to-count path instead.
[ProtoContract]
public class GroupedOnly
{
    [ProtoMember(1)] public int Id { get; set; }
    [ProtoMember(2, DataFormat = DataFormat.Group)] public Item Body { get; set; }
    [ProtoMember(3)] public string Trailer { get; set; }
}

// Marc's nightmare fuel, and the reason B14 needed a depth guard rather than just a faster
// measure:
//
//     Node a = new(), b = new() { GroupTail = a };
//     a.GroupTail = b;
//     Serialize(a);
//
// A length-prefixed cycle is caught by the MEASURE, which is depth-budgeted. A grouped cycle is
// not measured at all - that is the whole point of a group - so before the guard this recursed
// until the process died. It must now throw. NOT in Values: it cannot be serialized, so the
// differential suite must never see it; GroupCycleTests drives it directly.
[ProtoContract]
public class Node
{
    [ProtoMember(1)] public int Id { get; set; }
    [ProtoMember(2, DataFormat = DataFormat.Group)] public Node GroupTail { get; set; }
}

[ProtoContract]
public class GroupedMaps
{
    [ProtoMember(1, DataFormat = DataFormat.Group)] public Dictionary<int, Item> ByIndex { get; set; }
    [ProtoMember(2, DataFormat = DataFormat.Group)] public Dictionary<string, string> Scalars { get; set; }

    // the per-value format from [ProtoMap], which is the other route to a grouped value
    [ProtoMember(3)]
    [ProtoMap(ValueFormat = DataFormat.Group)]
    public Dictionary<int, Item> ViaMap { get; set; }

    [ProtoMember(4)] public Dictionary<int, Item> Plain { get; set; }
}

public static class GroupedElementsSamples
{
    private static Item One => new() { Name = "a", Count = 1 };
    private static Item Two => new() { Name = "b", Count = 2 };

    public static object[] Values =>
    [
        new Grouped(),
        new Grouped { Items = [One, Two], Array = [One], Plain = [Two], Single = One },
        new Grouped { Items = [] },
        // the unary-group contract: empty, populated, and with the group absent but the
        // surrounding scalars present - the last one is what proves the guard, since a null
        // group must contribute nothing at all to the measure
        new GroupedOnly(),
        new GroupedOnly { Id = 7, Body = One, Trailer = "tail" },
        new GroupedOnly { Id = 8, Trailer = "no group here" },
        new GroupedMaps(),
        new GroupedMaps
        {
            ByIndex = new Dictionary<int, Item> { { 1, One } },
            Scalars = new Dictionary<string, string> { { "k", "v" } },
            ViaMap = new Dictionary<int, Item> { { 2, Two } },
            Plain = new Dictionary<int, Item> { { 3, One } },
        },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Grouped))]
[ProtoSerializable(typeof(GroupedMaps))]
[ProtoSerializable(typeof(GroupedOnly))]
[ProtoSerializable(typeof(Node))]
public partial class GroupedElementsModel : TypeModel
{
}
