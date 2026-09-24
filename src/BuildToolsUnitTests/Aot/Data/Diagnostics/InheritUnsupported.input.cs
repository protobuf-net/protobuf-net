using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.InheritUnsupported;

// protobuf-net treats a derived contract its base does not [ProtoInclude] as a standalone contract
// that silently ignores every inherited member. Refusing is the safer half of that surprise.
[ProtoContract]
public class Base
{
    [ProtoMember(1)] public int Shared { get; set; }
}

[ProtoContract]
public class Unlinked : Base
{
    [ProtoMember(2)] public int Extra { get; set; }
}

// an abstract type with no sub-types can never be constructed, so it has nothing to serialize
[ProtoContract]
public abstract class AbstractLeaf
{
    [ProtoMember(1)] public int Value { get; set; }
}

// the (int, string) form defers to runtime type resolution
[ProtoContract]
[ProtoInclude(100, "AotFixtures.InheritUnsupported.ByName")]
public class ByNameBase
{
    [ProtoMember(1)] public int Value { get; set; }
}

[ProtoContract]
public class ByName : ByNameBase
{
    [ProtoMember(2)] public int Extra { get; set; }
}

// a dropped sub-type takes the whole hierarchy with it: the root dispatches to it by name
[ProtoContract]
[ProtoInclude(100, typeof(BadLeaf))]
public class GoodRoot
{
    [ProtoMember(1)] public int Value { get; set; }
}

[ProtoContract]
public class BadLeaf : GoodRoot
{
    [ProtoMember(2)] public System.Exception Unsupported { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Unlinked))]
[ProtoSerializable(typeof(AbstractLeaf))]
[ProtoSerializable(typeof(ByNameBase))]
[ProtoSerializable(typeof(GoodRoot))]
public partial class InheritUnsupportedModel : TypeModel
{
}
