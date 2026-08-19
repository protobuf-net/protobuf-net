using ProtoBuf;
using ProtoBuf.Meta;
using System;

// gap B26, item 2: at level 240+, DateTime becomes google.protobuf.Timestamp and TimeSpan becomes
// Duration - a seconds+nanos message, genuinely different arithmetic from the level-200 ScaledTicks
// form. Both fields are omitted when zero, so the body is value-dependent and a default value has
// an EMPTY body.
//
// The samples matter more than usual here. NormalizeSecondsNanoseconds runs before the write and
// decides the final pair, so a measure that skipped it would agree on ordinary values and disagree
// at every boundary - which is why sub-second, negative, and exact-second values are all present.
namespace AotFixtures.BclLevel240;

[ProtoContract, CompatibilityLevel(CompatibilityLevel.Level240)]
public class Level240
{
    [ProtoMember(1)] public DateTime When { get; set; }
    [ProtoMember(2)] public TimeSpan Took { get; set; }
    [ProtoMember(3)] public DateTime? MaybeWhen { get; set; }
    [ProtoMember(4)] public TimeSpan? MaybeTook { get; set; }
    // DateTime is written unconditionally, so the epoch (an empty body) already reaches the wire
    // through When; IsRequired does the same for TimeSpan.Zero
    [ProtoMember(5, IsRequired = true)] public TimeSpan AlwaysTook { get; set; }
}

public static class BclLevel240Samples
{
    public static object[] Values =>
    [
        // all defaults: the epoch and zero, both of which have an empty seconds+nanos body
        new Level240(),
        new Level240
        {
            When = new DateTime(2026, 8, 19, 10, 11, 12, DateTimeKind.Utc),
            Took = TimeSpan.FromSeconds(90),
            MaybeWhen = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            MaybeTook = TimeSpan.Zero,
            AlwaysTook = TimeSpan.FromHours(3),
        },
        // sub-second only: seconds omitted, nanos present
        new Level240 { Took = TimeSpan.FromMilliseconds(250), MaybeTook = TimeSpan.FromTicks(1) },
        // negatives, which sign-extend to the ten-byte varint form in both fields
        new Level240
        {
            When = new DateTime(1900, 6, 5, 4, 3, 2, DateTimeKind.Utc),
            Took = TimeSpan.FromMilliseconds(-1500),
            MaybeTook = TimeSpan.FromSeconds(-1),
        },
        // exact seconds: nanos omitted, seconds present
        new Level240 { Took = TimeSpan.FromSeconds(-42), AlwaysTook = TimeSpan.FromSeconds(7) },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Level240))]
public partial class BclLevel240Model : TypeModel { }
