using ProtoBuf;
using ProtoBuf.Meta;
using System;

namespace AotFixtures.Compat;

// DateTime, TimeSpan, Guid and decimal are the only types the compatibility level touches, and it is
// what picks between the bcl.proto forms and the well-known/string ones. All four are length-
// prefixed and go through BclHelpers; the level chooses which method.
//
// Note DateTime is written *unconditionally* - zero is a legitimate date, so unlike the other three
// there is no trivial value to skip.
[ProtoContract]
public class Legacy
{
    [ProtoMember(1)] public DateTime When { get; set; }
    [ProtoMember(2)] public TimeSpan How { get; set; }
    [ProtoMember(3)] public Guid Id { get; set; }
    [ProtoMember(4)] public decimal Amount { get; set; }

    [ProtoMember(5)] public DateTime? WhenMaybe { get; set; }
    [ProtoMember(6)] public decimal? AmountMaybe { get; set; }
}

[ProtoContract, CompatibilityLevel(CompatibilityLevel.Level240)]
public class Level240
{
    [ProtoMember(1)] public DateTime When { get; set; }
    [ProtoMember(2)] public TimeSpan How { get; set; }
    [ProtoMember(3)] public Guid Id { get; set; }
    [ProtoMember(4)] public decimal Amount { get; set; }
}

[ProtoContract, CompatibilityLevel(CompatibilityLevel.Level300)]
public class Level300
{
    [ProtoMember(1)] public DateTime When { get; set; }
    [ProtoMember(2)] public TimeSpan How { get; set; }
    [ProtoMember(3)] public Guid Id { get; set; }
    [ProtoMember(4)] public decimal Amount { get; set; }

    // the 16-byte bytes variant of a level-300 Guid
    [ProtoMember(5, DataFormat = DataFormat.FixedSize)] public Guid Fixed { get; set; }
}

// per-member overrides, in both directions
[ProtoContract, CompatibilityLevel(CompatibilityLevel.Level300)]
public class Mixed
{
    [ProtoMember(1)] public DateTime Inherited { get; set; }

    [ProtoMember(2), CompatibilityLevel(CompatibilityLevel.Level200)]
    public DateTime Downgraded { get; set; }

    [ProtoMember(3), CompatibilityLevel(CompatibilityLevel.Level200)]
    public Guid DowngradedGuid { get; set; }
}

#pragma warning disable CS0618 // DataFormat.WellKnown is the older single-step form of the same idea
[ProtoContract]
public class WellKnown
{
    [ProtoMember(1, DataFormat = DataFormat.WellKnown)] public DateTime When { get; set; }
    [ProtoMember(2, DataFormat = DataFormat.WellKnown)] public TimeSpan How { get; set; }

    // nothing to promote: level 240 is the same as 200 for these two
    [ProtoMember(3, DataFormat = DataFormat.WellKnown)] public Guid Id { get; set; }
}

// FixedSize on a Guid below level 300 is simply ignored
[ProtoContract]
public class LegacyFixed
{
    [ProtoMember(1, DataFormat = DataFormat.FixedSize)] public Guid Id { get; set; }
}
#pragma warning restore CS0618

// DataFormat on a BCL type shifts the field header, and not uniformly. Note the BclHelpers methods
// are wire-type aware: under a Fixed64 header WriteDateTime emits the 8-byte fixed form rather than
// a message, so this is a compact encoding rather than a mislabelled message.
[ProtoContract]
public class Formats
{
    [ProtoMember(1, DataFormat = DataFormat.FixedSize)] public DateTime FixedWhen { get; set; }
    [ProtoMember(2, DataFormat = DataFormat.FixedSize)] public TimeSpan FixedHow { get; set; }
    [ProtoMember(3, DataFormat = DataFormat.Group)] public DateTime GroupedWhen { get; set; }
    [ProtoMember(4, DataFormat = DataFormat.Group)] public TimeSpan GroupedHow { get; set; }
    [ProtoMember(5, DataFormat = DataFormat.Group)] public Guid GroupedId { get; set; }

    // ... and the ones the format does not reach: decimal ignores it entirely, and a Guid below
    // level 300 ignores FixedSize
    [ProtoMember(6, DataFormat = DataFormat.Group)] public decimal GroupedAmount { get; set; }
    [ProtoMember(7, DataFormat = DataFormat.FixedSize)] public decimal FixedAmount { get; set; }
    [ProtoMember(8, DataFormat = DataFormat.FixedSize)] public Guid FixedId { get; set; }
}

// the type-level attribute is inherited by derived contracts
[ProtoContract, CompatibilityLevel(CompatibilityLevel.Level300)]
[ProtoInclude(100, typeof(InheritsLevel))]
public class LevelledBase
{
    [ProtoMember(1)] public DateTime When { get; set; }
}

[ProtoContract]
public class InheritsLevel : LevelledBase
{
    [ProtoMember(1)] public Guid Id { get; set; }
}

public static class CompatSamples
{
    private static readonly DateTime When = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
    private static readonly Guid Id = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e");

    public static object[] Values =>
    [
        new Legacy(),
        new Legacy { When = When, How = TimeSpan.FromMinutes(90), Id = Id, Amount = 1.25m },
        new Legacy { WhenMaybe = When, AmountMaybe = 0m },
        new Level240(),
        new Level240 { When = When, How = TimeSpan.FromSeconds(3), Id = Id, Amount = -2.5m },
        new Level300(),
        new Level300 { When = When, How = TimeSpan.FromHours(2), Id = Id, Amount = 3.75m, Fixed = Id },
        new Mixed { Inherited = When, Downgraded = When, DowngradedGuid = Id },
        new WellKnown { When = When, How = TimeSpan.FromDays(1), Id = Id },
        new LegacyFixed { Id = Id },
        new LevelledBase { When = When },
        new InheritsLevel { When = When, Id = Id },
        new Formats(),
        new Formats
        {
            FixedWhen = When,
            FixedHow = TimeSpan.FromMinutes(90),
            GroupedWhen = When,
            GroupedHow = TimeSpan.FromSeconds(7),
            GroupedId = Id,
            GroupedAmount = 4.5m,
            FixedAmount = -6.25m,
            FixedId = Id,
        },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Legacy))]
[ProtoSerializable(typeof(Level240))]
[ProtoSerializable(typeof(Level300))]
[ProtoSerializable(typeof(Mixed))]
[ProtoSerializable(typeof(WellKnown))]
[ProtoSerializable(typeof(LegacyFixed))]
[ProtoSerializable(typeof(Formats))]
[ProtoSerializable(typeof(LevelledBase))]
public partial class CompatModel : TypeModel
{
}
