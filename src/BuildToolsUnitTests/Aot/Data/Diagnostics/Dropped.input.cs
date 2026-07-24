using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

// every contract here is expected to be dropped, each for a different reason; the point of the
// fixture is the .txt golden, which is the user-visible explanation of why
namespace AotFixtures.Dropped;

[ProtoContract]
public class HasUnsupportedMember
{
    [ProtoMember(1)]
    public int Id { get; set; }

    // collections are not handled yet -> PBN2001
    [ProtoMember(2)]
    public List<string> Tags { get; set; }
}

[ProtoContract]
public class ReferencesDropped
{
    // fine in itself, but its member type is dropped -> PBN2004
    [ProtoMember(1)]
    public HasUnsupportedMember Child { get; set; }
}

[ProtoContract]
public class NoParameterlessConstructor
{
    public NoParameterlessConstructor(int id) => Id = id;

    // -> PBN2002
    [ProtoMember(1)]
    public int Id { get; set; }
}

[ProtoContract]
public class UsesMemberOptions
{
    // named arguments change the wire format -> PBN2003
    [ProtoMember(1, IsRequired = true)]
    public int Value { get; set; }
}

[ProtoContract]
public class InitOnlyMember
{
    // -> PBN2001
    [ProtoMember(1)]
    public int Value { get; init; }
}

[ProtoModel]
[ProtoSerializable(typeof(ReferencesDropped))]
[ProtoSerializable(typeof(NoParameterlessConstructor))]
[ProtoSerializable(typeof(UsesMemberOptions))]
[ProtoSerializable(typeof(InitOnlyMember))]
public partial class DroppedModel : TypeModel
{
}
