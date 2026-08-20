// DateOnly/TimeOnly, whose BclHelpers methods live inside #if NET6_0_OR_GREATER - so the generator
// probes for the *method*, not the language type.
//
// This fixture is <Compile Remove>d from AotRefGen, which is net472 and has no DateOnly at all, so it
// has no .reference.cs - deliberate, not neglect. The golden here is a drop (the golden tests compile
// against the netstandard2.0 BuildTools assembly); the differential suite on net8.0 is where it is
// really exercised.

using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;

namespace AotFixtures.DateOnlyFixture;

// DateOnly and TimeOnly go through BclHelpers, like the four compatibility-level BCL types - but
// under a *varint* header rather than a length prefix, and the compatibility level does not reach
// them at all.
//
// The golden output here is deliberately a *drop*: BclHelpers.ReadDateOnly is inside
// `#if NET6_0_OR_GREATER`, and the golden tests compile against the netstandard2.0 BuildTools
// assembly, which has the language type but not the method. The generator probes for the method
// rather than the type, so it declines here and emits for real everywhere else. The differential
// suite references the net8.0 library, so that is where these are actually exercised.
[ProtoContract]
public class Days
{
    [ProtoMember(1)] public DateOnly Date { get; set; }
    [ProtoMember(2)] public TimeOnly Time { get; set; }

    [ProtoMember(3)] public DateOnly? MaybeDate { get; set; }
    [ProtoMember(4)] public TimeOnly? MaybeTime { get; set; }

    [ProtoMember(5)] public List<DateOnly> Dates { get; set; }
    [ProtoMember(6)] public DateOnly[] More { get; set; }
}

public static class DateOnlySamples
{
    public static object[] Values =>
    [
        new Days(),
        new Days { Date = new DateOnly(2026, 7, 26), Time = new TimeOnly(13, 45, 30) },
        new Days { MaybeDate = default(DateOnly), MaybeTime = default(TimeOnly) },
        new Days { MaybeDate = new DateOnly(1999, 1, 1), MaybeTime = new TimeOnly(0, 0, 1) },
        new Days { Dates = [new DateOnly(2020, 2, 29), new DateOnly(2024, 12, 31)] },
        new Days { More = [new DateOnly(2000, 1, 1)] },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Days))]
public partial class DateOnlyModel : TypeModel
{
}
