using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.TupleMembers;

[ProtoContract]
public class HasTuples
{
    // named and anonymous are the *same* type - element names are identity-convertible decoration.
    // Both must resolve to a single ISerializer<(int, string)>; emitting one per spelling would be
    // the same interface twice, and would not compile.
    [ProtoMember(1)] public (int Id, string Name) Named { get; set; }
    [ProtoMember(2)] public (int, string) Anonymous { get; set; }

    // nested names have to be erased too
    [ProtoMember(3)] public (int Outer, (int Inner, string Label) Nested) Deep { get; set; }

    // a non-tuple tuple-like type as a member
    [ProtoMember(4)] public KeyValuePair<int, string> Pair { get; set; }

    // a nullable struct tuple
    [ProtoMember(5)] public (int, string)? MaybePair { get; set; }

    [ProtoMember(6)] public int Other { get; set; }
}

public static class TupleMembersSamples
{
    public static object[] Values =>
    [
        new HasTuples(),
        new HasTuples { Named = (1, "a") },
        new HasTuples { Anonymous = (2, "b") },
        new HasTuples { Named = (3, "c"), Anonymous = (4, "d") },
        new HasTuples { Deep = (5, (6, "e")) },
        new HasTuples { Pair = new KeyValuePair<int, string>(7, "f") },
        new HasTuples { MaybePair = (8, "g") },
        new HasTuples { Named = (9, null), Other = 10 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(HasTuples))]
public partial class TupleMembersModel : TypeModel
{
}
