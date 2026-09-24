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
public partial class GroupedElementsModel : TypeModel
{
}
