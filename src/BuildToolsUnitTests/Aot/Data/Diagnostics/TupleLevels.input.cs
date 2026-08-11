using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;

namespace AotFixtures.TupleLevels;

// An auto-tuple is keyed in the model by type alone, but its *encoding* follows the compatibility
// level it is reached at - so the same tuple reached at two levels is not expressible with one
// serializer, and protobuf-net refuses the model: "must use a single compatibility level".
// One serializer per type is our constraint too, so the tuple is dropped and its referrers cascade.

[ProtoContract]
public class Conflicting
{
    // Level300 here...
    [ProtoMember(1)]
    [CompatibilityLevel(CompatibilityLevel.Level300)]
    public (DateTime, TimeSpan) Explicit { get; set; }

    // ...and the ambient Level200 here, on the same tuple type
    [ProtoMember(2)]
    public List<(DateTime, TimeSpan)> Ambient { get; set; }
}

// The same shape with both members agreeing is *fine* and must still emit - the check is about
// disagreement, not about annotating tuples at all. Note it has to use a *different* tuple type:
// the conflict above belongs to the tuple, not to the contract, so sharing one would cascade to
// here as well - which is right, and is exactly what protobuf-net does to a whole model.
[ProtoContract]
public class Agreeing
{
    [ProtoMember(1)]
    [CompatibilityLevel(CompatibilityLevel.Level300)]
    public (Guid, decimal) One { get; set; }

    [ProtoMember(2)]
    [CompatibilityLevel(CompatibilityLevel.Level300)]
    public List<(Guid, decimal)> Two { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Conflicting))]
[ProtoSerializable(typeof(Agreeing))]
public partial class TupleLevelsModel : TypeModel
{
}
