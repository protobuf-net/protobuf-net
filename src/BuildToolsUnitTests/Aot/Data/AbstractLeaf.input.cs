using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.AbstractLeaf;

// An abstract contract with no [ProtoInclude] can only ever hold null: protobuf-net accepts the type
// and writes nothing for a null, but throws "Unexpected sub-type" for any real value, since there is
// no sub-type to dispatch to and the type itself cannot be constructed.
//
// Refusing it used to cascade, dropping otherwise-usable referrers like Holder below - which is the
// only reason this is worth emitting at all.
[ProtoContract]
public abstract class Shape
{
    [ProtoMember(1)] public int Sides { get; set; }
}

[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public Shape Value { get; set; }
    [ProtoMember(2)] public string Name { get; set; }
}

public static class AbstractLeafSamples
{
    public static object[] Values =>
    [
        new Holder(),
        new Holder { Name = "x" },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Holder))]
public partial class AbstractLeafModel : TypeModel
{
}
