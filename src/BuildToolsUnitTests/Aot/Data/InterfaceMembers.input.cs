using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.InterfaceMembers;

// Interface *members* beyond the plain root-plus-implementations shape Interface.input.cs covers.
// All of these work on both ref-emit paths; the point of the fixture is that they agree on the wire,
// since several of them route through machinery that had never been exercised through an interface.

// an interface deriving another interface: two hierarchy layers, both interfaces
[ProtoContract, ProtoInclude(10, typeof(IMiddle))]
public interface IRoot { }

[ProtoContract, ProtoInclude(11, typeof(Leaf))]
public interface IMiddle : IRoot { }

[ProtoContract]
public class Leaf : IMiddle { [ProtoMember(1)] public int N { get; set; } }

// a closed generic interface - open ones are refused, as everywhere else
[ProtoContract, ProtoInclude(10, typeof(Box))]
public interface IBox<T> { }

[ProtoContract]
public class Box : IBox<int> { [ProtoMember(1)] public int N { get; set; } }

[ProtoContract, ProtoInclude(10, typeof(Named))] public interface INameable { }

[ProtoContract]
public class Named : INameable { [ProtoMember(1)] public string S { get; set; } }

[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public IRoot ViaRoot { get; set; }
    [ProtoMember(2)] public IMiddle ViaMiddle { get; set; }
    [ProtoMember(3)] public IBox<int> Boxed { get; set; }
    // an interface on either side of a map; the key case is unusual but protobuf-net allows it
    [ProtoMember(6)] public Dictionary<int, INameable> ByIndex { get; set; }
    [ProtoMember(7)] public Dictionary<INameable, int> ByName { get; set; }
}

public static class InterfaceMembersSamples
{
    public static object[] Values =>
    [
        new Holder(),
        new Holder { ViaRoot = new Leaf { N = 1 }, ViaMiddle = new Leaf { N = 2 } },
        new Holder { Boxed = new Box { N = 3 } },
        new Holder { ByIndex = new Dictionary<int, INameable> { { 1, new Named { S = "a" } } } },
        new Holder { ByName = new Dictionary<INameable, int> { { new Named { S = "b" }, 2 } } },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Holder))]
public partial class InterfaceMembersModel : TypeModel
{
}
