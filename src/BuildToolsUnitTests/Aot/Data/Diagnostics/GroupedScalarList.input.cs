using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.GroupedScalarList;

// DataFormat.Group on a collection of *scalars* is refused by protobuf-net itself - there is no
// sub-message to frame, so there is nothing for the group markers to wrap. Both ref-emit paths
// throw "Operation is not valid due to the current state of the object" while building the model,
// so refusing it here is a match rather than a shortfall.
[ProtoContract]
public class GroupedScalars
{
    [ProtoMember(1, DataFormat = DataFormat.Group)] public List<int> Numbers { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(GroupedScalars))]
public partial class GroupedScalarListModel : TypeModel
{
}
