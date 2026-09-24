using ProtoBuf;
using ProtoBuf.Meta;

// Every way a [ProtoSubType] declaration can be wrong. All of these are refusals that *match*
// protobuf-net: MetaType.AddSubType throws for each, so there is no behaviour to reproduce - which
// is why the messages quote what it says rather than reading as our backlog.
//
// Note where they are reported: against the base type, and only when the base is actually reached.
// A declaration lives on an assembly, so a mistake in a library would otherwise warn in every model
// that references it, including models with no interest in the hierarchy.
namespace AotFixtures.SubTypeUnsupported;

// a type declared as a sub-type of itself; the runtime gets as far as "Cyclic inheritance"
[ProtoContract]
public class SelfBase
{
    [ProtoMember(1)] public int Value { get; set; }
}

// a type that simply does not derive from the base named
[ProtoContract]
public class StrangerBase
{
    [ProtoMember(1)] public int Value { get; set; }
}

[ProtoContract]
public class Stranger
{
    [ProtoMember(1)] public int Value { get; set; }
}

// "Sub-types can only be added to non-sealed classes"
[ProtoContract]
public sealed class SealedBase
{
    [ProtoMember(1)] public int Value { get; set; }
}

[ProtoContract]
public class NotReallyDerived
{
    [ProtoMember(1)] public int Value { get; set; }
}

// field numbers are checked exactly as AddSubType checks them
[ProtoContract]
public class RangeBase
{
    [ProtoMember(1)] public int Value { get; set; }
}

[ProtoContract]
public class OutOfRange : RangeBase
{
    [ProtoMember(1)] public int Extra { get; set; }
}

// a declaration colliding with an include the base declares itself: the two surfaces are checked
// together, because a duplicate switch label does not compile whichever surface it came from
[ProtoContract]
[ProtoInclude(20, typeof(First))]
public class CollidingBase
{
    [ProtoMember(1)] public int Value { get; set; }
}

[ProtoContract]
public class First : CollidingBase
{
    [ProtoMember(1)] public int Alpha { get; set; }
}

[ProtoContract]
public class Second : CollidingBase
{
    [ProtoMember(1)] public int Beta { get; set; }
}

// one sub-type named twice, at two different field numbers
[ProtoContract]
public class TwiceBase
{
    [ProtoMember(1)] public int Value { get; set; }
}

[ProtoContract]
public class Twice : TwiceBase
{
    [ProtoMember(1)] public int Repeat { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(SelfBase))]
[ProtoSerializable(typeof(StrangerBase))]
[ProtoSerializable(typeof(SealedBase))]
[ProtoSerializable(typeof(RangeBase))]
[ProtoSerializable(typeof(CollidingBase))]
[ProtoSerializable(typeof(TwiceBase))]
[ProtoSubType(typeof(SelfBase), typeof(SelfBase), 100)]
[ProtoSubType(typeof(StrangerBase), typeof(Stranger), 100)]
[ProtoSubType(typeof(SealedBase), typeof(NotReallyDerived), 100)]
[ProtoSubType(typeof(RangeBase), typeof(OutOfRange), 0)]
[ProtoSubType(typeof(CollidingBase), typeof(Second), 20)]
[ProtoSubType(typeof(TwiceBase), typeof(Twice), 30)]
[ProtoSubType(typeof(TwiceBase), typeof(Twice), 31)]
public partial class SubTypeUnsupportedModel : TypeModel
{
}
