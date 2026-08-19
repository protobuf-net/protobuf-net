using ProtoBuf;
using System;

namespace AotFixtures.BclMeasure;

// A level-200 DateTime/TimeSpan contract with nothing else in it, because no existing fixture
// exercises the arithmetic measure for these: the one contract that had a level-200 DateTime
// (Getter) is unmeasurable for unrelated reasons, and Compat is entirely level 240/300. Without
// this the measure arm would be dead code that still compiled.
//
// The point of the contract is that it must be MEASURABLE - so no getter-only members, no
// callbacks, no surrogate, nothing that would drop it back to write-to-count and hide the arm.

[ProtoContract]
public class Timings
{
    // DateTime is written UNCONDITIONALLY - zero is a legitimate date - while TimeSpan is guarded
    // against Zero. The measure has to make the same choice, so both are here.
    [ProtoMember(1)] public DateTime When { get; set; }
    [ProtoMember(2)] public TimeSpan Elapsed { get; set; }

    [ProtoMember(3)] public DateTime? MaybeWhen { get; set; }
    [ProtoMember(4)] public TimeSpan? MaybeElapsed { get; set; }

    // NO repeated BCL member here, deliberately: DateTime is not in the repeated whitelist, and a
    // repeated member is tested for eligibility BEFORE the BCL arm - so one `List<DateTime>` would
    // drop this whole contract back to write-to-count and the scalar arm this fixture exists to
    // cover would silently not be reached. Repeated BCL is its own step; see notes/gaps.md B26.

    // Guid and decimal complete the level-200 family. Both are guarded (against Guid.Empty and
    // 0m), unlike DateTime, and decimal's body is value-dependent where Guid's is a constant 18.
    [ProtoMember(6)] public Guid Id { get; set; }
    [ProtoMember(7)] public decimal Amount { get; set; }
    [ProtoMember(8)] public Guid? MaybeId { get; set; }
    [ProtoMember(9)] public decimal? MaybeAmount { get; set; }

    // an ordinary scalar alongside, so the golden shows the BCL contribution next to a plain one
    [ProtoMember(10)] public int Sequence { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Timings))]
public partial class BclMeasureModel : ProtoBuf.Meta.TypeModel { }

public static class BclMeasureSamples
{
    public static object[] Values =>
    [
        new Timings(),
        new Timings
        {
            // values chosen to move the parts the format varies on: the SCALE (a whole number of
            // days encodes differently from ticks), the SIGN (the value field is zigzag, so a
            // pre-epoch date matters), and zero (which omits the field and empties the body)
            When = new DateTime(1900, 1, 1),
            Elapsed = TimeSpan.FromDays(2),
            MaybeWhen = new DateTime(2026, 8, 16, 13, 45, 30, 123),
            MaybeElapsed = TimeSpan.FromTicks(-1),
            Id = Guid.Parse("5bad8f0f-cbd9-9f46-a165-708677289505"),
            Amount = -123456789.987654321m,
            MaybeId = Guid.Empty,          // present, but an EMPTY body
            MaybeAmount = 0m,              // ditto: presence without content
            Sequence = 42,
        },
        new Timings
        {
            When = default,                 // the epoch: an EMPTY body, still written
            Elapsed = TimeSpan.Zero,        // guarded away entirely
            Id = Guid.Empty,                // guarded away
            Amount = 0m,                    // guarded away
            Sequence = -1,
        },
    ];
}
