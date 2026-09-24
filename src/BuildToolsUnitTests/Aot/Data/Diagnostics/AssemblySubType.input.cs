using ProtoBuf;
using ProtoBuf.Meta;

// [ProtoSubType] can be declared on an *assembly* or a *module* as well as on a model, which is what
// lets a library ship the linkage for a hierarchy its consumer never names - the case the ticket is
// actually about (protobuf-net#1308), where the sub-type is a generic construction the base library
// could not have written a [ProtoInclude] for.
//
// Unlike [ProtoSurrogate], declarations accumulate rather than override: two references each naming
// a sub-type of one base give a hierarchy with both. There is nothing to be more specific *about* -
// a hierarchy is the union of everything declared for it.
//
// Under Diagnostics/ because every fixture in Data/ is linked into one assembly, so an
// assembly-level attribute there would apply to all of them (the same reason the [module:
// CompatibilityLevel] fixture is here); the golden tests compile each input in isolation.
// ProtoSubTypeReferenceTests covers the same thing across genuinely separate compilations.
[assembly: ProtoSubType(typeof(AotFixtures.AssemblySubType.Node), typeof(AotFixtures.AssemblySubType.Leaf), 50)]
[module: ProtoSubType(typeof(AotFixtures.AssemblySubType.Node), typeof(AotFixtures.AssemblySubType.Branch), 51)]

namespace AotFixtures.AssemblySubType;

// no [ProtoInclude] here: this is the type in the base library
[ProtoContract]
public class Node
{
    [ProtoMember(1)] public string Name { get; set; }
}

[ProtoContract]
public class Leaf : Node
{
    [ProtoMember(1)] public int Value { get; set; }
}

[ProtoContract]
public class Branch : Node
{
    [ProtoMember(1)] public int Count { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Node))]
public partial class AssemblySubTypeModel : TypeModel
{
}
