using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.SetterUnsupported;

// ref-emit's *compiled* path refuses a non-public setter outright - "cannot apply changes to
// property" - even though its runtime path reaches one by reflection. We match the compiled path.
[ProtoContract]
public class NonPublicSetter
{
    [ProtoMember(1)] public int Value { get; private set; }
}

[ProtoContract]
public struct Point
{
    [ProtoMember(1)] public int X { get; set; }
}

// discarding the read only means something where the instance itself is mutated; a struct
// sub-message would be mutating a copy, and there is no ref-emit reference for it
[ProtoContract]
public class ReadOnlyStructMessage
{
    [ProtoMember(1)] public Point Where { get; }
}

[ProtoContract]
public class ReadOnlyNullableMessage
{
    [ProtoMember(1)] public Point? Where { get; }
}

[ProtoModel]
[ProtoSerializable(typeof(NonPublicSetter))]
[ProtoSerializable(typeof(ReadOnlyStructMessage))]
[ProtoSerializable(typeof(ReadOnlyNullableMessage))]
public partial class SetterUnsupportedModel : TypeModel
{
}
