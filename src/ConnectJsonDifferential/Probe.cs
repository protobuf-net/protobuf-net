using System.Collections.Generic;
using ProtoBuf;

namespace ProtoBuf.ConnectJsonDifferential;

/// <summary>
/// Shapes that have no canonical JSON form, or that the JSON emitter cannot yet express.
/// </summary>
/// <remarks>
/// These are not expected to work; they are here so that "does not work" means <em>refused with a
/// diagnostic</em> rather than <em>emits code the consumer's build rejects</em>. The binary
/// serializer for every one of them must still be emitted - the JSON surface is a subset, and
/// narrowing it must not narrow the other.
/// </remarks>
[ProtoContract]
public class Awkward
{
    // a HashSet is not a List, so a reader that builds a List has nowhere to put it
    [ProtoMember(1)] public HashSet<int> Unique { get; set; } = new();

    // an array needs .ToArray() rather than the list itself
    [ProtoMember(2)] public string[] Names { get; set; }

    // a Queue is neither
    [ProtoMember(3)] public Queue<int> Pending { get; set; } = new();

    // level 200 by default, where a DateTime is a protobuf-net message and not a Timestamp
    // (moved out: a level-200 DateTime refused the whole contract and masked everything below it)

    // explicit presence: a nullable scalar is written even when it holds the default
    [ProtoMember(5)] public int? Maybe { get; set; }
}

[ProtoContract]
[ProtoInclude(100, typeof(Derived))]
public class Base
{
    [ProtoMember(1)] public int Id { get; set; }
}

[ProtoContract]
public class Derived : Base
{
    [ProtoMember(1)] public string Extra { get; set; }
}

[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public Base Item { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Awkward))]
[ProtoSerializable(typeof(Holder))]
public partial class ProbeModel : ProtoBuf.Meta.TypeModel
{
}
