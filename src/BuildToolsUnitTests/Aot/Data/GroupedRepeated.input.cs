using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

// gap B35: a REPEATED grouped message member. Before this, DataFormat.Group was admitted only on a
// UNARY message, so a contract carrying one of these lost measure-first entirely - and because the
// exclusion runs to a fixed point, it took every contract referencing it too. The benchmark that
// found it (marc/bench-delimited-v4) measured 5.7x, from a member it never even populated.
//
// Both members are here deliberately: Child pins the unary form that always worked, Children the
// repeated form that did not, and the pair in one contract is the shape that used to fail - the
// whole point is that Child stays fast even though Children exists.
namespace AotFixtures.GroupedRepeated;

[ProtoContract]
public class Node
{
    [ProtoMember(1)] public int Value { get; set; }
    [ProtoMember(2, DataFormat = DataFormat.Group)] public Node Child { get; set; }
    [ProtoMember(3, DataFormat = DataFormat.Group)] public List<Node> Children { get; set; }
    // an array reaches the same emit through a different span source (the array itself, rather than
    // CollectionsMarshal.AsSpan), so both admitted collection shapes are pinned
    [ProtoMember(4, DataFormat = DataFormat.Group)] public Node[] More { get; set; }
    // the length-prefixed twin, so the two framings can be read side by side in the golden
    [ProtoMember(5)] public List<Node> Prefixed { get; set; }
}

public static class GroupedRepeatedSamples
{
    public static object[] Values =>
    [
        new Node(),
        new Node { Value = 1, Child = new Node { Value = 2 } },
        new Node
        {
            Value = 3,
            Children = [new Node { Value = 4 }, new Node { Value = 5, Child = new Node { Value = 6 } }],
            More = [new Node { Value = 7 }],
            Prefixed = [new Node { Value = 8 }],
        },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Node))]
public partial class GroupedRepeatedModel : TypeModel { }
