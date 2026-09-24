using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.InterfaceUnsupported;

// Two interface-hierarchy shapes that are refused, both matching protobuf-net rather than falling
// short of it. Only reachable through an interface: a struct cannot derive from a class, and a class
// has one base.

// A value-type sub-type. Every hierarchy API is constrained to reference types -
// ISubTypeSerializer<T>, WriteSubType, ReadSubType, SubTypeState<T> - so emitting this does not
// merely misbehave, it produces code the consumer cannot compile (CS0452, seven of them).
// protobuf-net refuses it at runtime on both paths: "Unexpected sub-type: Point".
[ProtoContract, ProtoInclude(10, typeof(Point))]
public interface IShape { }

[ProtoContract]
public struct Point : IShape { [ProtoMember(1)] public int X { get; set; } }

[ProtoContract]
public class HasShape { [ProtoMember(1)] public IShape Shape { get; set; } }

// One implementation named by two hierarchies. Each works in isolation - the wire form follows the
// *member's* declared type - but protobuf-net refuses the pair once both are in one model, and the
// generator's model is always one model. The two ref-emit paths refuse it differently: the compiled
// one says "can only participate in one inheritance hierarchy", the reflection one fails later with
// "the type cannot be changed once a serializer has been generated".
[ProtoContract, ProtoInclude(10, typeof(TwoFaced))] public interface IFirst { }
[ProtoContract, ProtoInclude(20, typeof(TwoFaced))] public interface ISecond { }

[ProtoContract]
public class TwoFaced : IFirst, ISecond { [ProtoMember(1)] public int N { get; set; } }

[ProtoContract]
public class HasBoth
{
    [ProtoMember(1)] public IFirst First { get; set; }
    [ProtoMember(2)] public ISecond Second { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(HasShape))]
[ProtoSerializable(typeof(HasBoth))]
public partial class InterfaceUnsupportedModel : TypeModel
{
}
