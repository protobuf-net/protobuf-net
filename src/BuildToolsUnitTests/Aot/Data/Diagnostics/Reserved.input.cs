using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.Reserved;

// [ProtoReserved] looks like schema decoration and is not: MetaType.ValidateReservations throws
// while building the model when a member or sub-type lands on a reserved number or name. So these
// contracts do not work in protobuf-net at all, and the refusal is a match rather than a shortfall.
// Found by the corpus differential on ProtoBuf.Test.Issues.Issue633, where we emitted all six.

[ProtoContract]
[ProtoReserved(31, "iz 31")]
public class ReservedSingle
{
    [ProtoMember(30)] public int A { get; set; }
    [ProtoMember(31)] public int B { get; set; }
}

[ProtoContract]
[ProtoReserved(20, 40, "iz 32")]
public class ReservedRange
{
    [ProtoMember(1)] public int A { get; set; }
    [ProtoMember(32)] public int B { get; set; }
}

[ProtoContract]
[ProtoReserved("B", "iz B")]
public class ReservedName
{
    [ProtoMember(1)] public int A { get; set; }
    [ProtoMember(33)] public int B { get; set; }
}

[ProtoContract]
[ProtoInclude(31, typeof(ReservedSubTypeLeaf))]
[ProtoReserved(31, "iz 31")]
public class ReservedSubType
{
    [ProtoMember(1)] public int A { get; set; }
}

[ProtoContract]
public class ReservedSubTypeLeaf : ReservedSubType
{
    [ProtoMember(2)] public int C { get; set; }
}

// ...and the shape that is *fine*: a reservation nothing collides with, which must still emit
[ProtoContract]
[ProtoReserved(100, 200, "unused")]
[ProtoReserved("Missing")]
public class ReservedButClear
{
    [ProtoMember(1)] public int A { get; set; }
    [ProtoMember(2)] public string B { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(ReservedSingle))]
[ProtoSerializable(typeof(ReservedRange))]
[ProtoSerializable(typeof(ReservedName))]
[ProtoSerializable(typeof(ReservedSubType))]
[ProtoSerializable(typeof(ReservedButClear))]
public partial class ReservedModel : TypeModel
{
}
