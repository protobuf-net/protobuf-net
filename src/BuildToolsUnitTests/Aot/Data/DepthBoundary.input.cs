// Gap B15: the RAW write path carries its own remaining-depth budget and never touches
// writer.Depth, which is fine until a raw body hands BACK to the stateful engine - the engine then
// counts nesting from whatever writer.Depth was at the outer boundary, so the two caps do not add.
//
// Isolating that needs a contract that is BOTH raw-written (so Next deepens without touching
// writer.Depth) and carries a stateful nesting call. A nullable-struct message member is exactly
// that pair: it is measurable, so it does not take the contract off the raw path, but its write
// stays on state.WriteMessage because RawNativeMessageTarget refuses IsNullable.
using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.DepthBoundary;

[ProtoContract]
public struct Link
{
    [ProtoMember(1)] public Rung Target { get; set; }
}

[ProtoContract]
public class Rung
{
    // length-prefixed: measured, and so already additive across a boundary - the measure recursion
    // crosses it without re-seeding
    [ProtoMember(1)] public Rung Next { get; set; }
    // GROUPED: a group carries no length prefix, so nothing measures this chain, which leaves the
    // raw write's own budget as the only guard on the way down. That is the only shape that can
    // observe gap B15 at all, and RawDepthBoundaryTests builds its ladder through this member.
    //
    // NOTE the samples never put Side underneath Deep. That combination writes a stream the reader
    // rejects ("Sub-message not read entirely") - a stateful hand-back inside a GROUPED raw body -
    // and it is gap B44, not this fixture's subject.
    [ProtoMember(4, DataFormat = DataFormat.Group)] public Rung Deep { get; set; }
    // stateful: crosses back into the engine, which then re-enters the raw path for Target
    [ProtoMember(2)] public Link? Side { get; set; }
    [ProtoMember(3)] public int Id { get; set; }
}

// The ladder gap B15 is actually about, isolated. NOTHING here is ever measured: `Deep` and
// `Target` are both GROUPED, so neither is a length-prefixed site, so the contract has no slot
// consumers and its Write entry emits RawWrite_ with no measure pass at all (gap B35).
//
// That matters because a measure recursion crosses a stateful boundary WITHOUT re-seeding, and so
// is already additive - any contract with a measured site is guarded by that and cannot observe
// B15. Only a fully-unmeasured chain leaves the raw write's own budget as the sole guard.
[ProtoContract]
public struct Hop
{
    [ProtoMember(1, DataFormat = DataFormat.Group)] public Step Target { get; set; }
}

[ProtoContract]
public class Step
{
    [ProtoMember(1, DataFormat = DataFormat.Group)] public Step Deep { get; set; }
    [ProtoMember(2)] public Hop? Side { get; set; }
    [ProtoMember(3)] public int Id { get; set; }
}

public static class DepthBoundarySamples
{
    public static object[] Values =>
    [
        new Rung(),
        new Rung { Id = 1, Next = new Rung { Id = 2 } },
        new Rung { Id = 3, Side = new Link { Target = new Rung { Id = 4 } } },
        new Rung { Id = 5, Next = new Rung { Id = 6, Side = new Link { Target = new Rung { Id = 7 } } } },
        new Rung { Id = 8, Deep = new Rung { Id = 9, Deep = new Rung { Id = 10 } } },
        new Step { Id = 11 },
        new Step { Id = 12, Deep = new Step { Id = 13 } },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Rung))]
[ProtoSerializable(typeof(Step))]
public partial class DepthBoundaryModel : TypeModel { }
