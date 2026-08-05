using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;

namespace AotFixtures.CompatElements;

// The compatibility level reaches collection *elements* and map keys/values, not just plain scalar
// members - and when it selects anything other than the level-200 form, ref-emit passes an explicit
// element serializer alongside the collection rather than relying on the inbuilt default.
//
// Found by the corpus differential (src/AotDifferential), which is the only harness that would:
// the shape compiles and the features are right, so only the bytes disagree.

[ProtoContract]
[CompatibilityLevel(CompatibilityLevel.Level300)]
public class Level300Lists
{
    // level 300 makes these GuidString and DecimalString, where 200 is the bcl.proto form
    [ProtoMember(1)] public List<Guid> Guids { get; set; }
    [ProtoMember(2)] public List<decimal> Decimals { get; set; }

    // ...and Timestamp/Duration rather than the bcl.proto DateTime/TimeSpan
    [ProtoMember(3)] public List<DateTime> Dates { get; set; }
    [ProtoMember(4)] public List<TimeSpan> Spans { get; set; }

    // FixedSize at level 300 selects GuidBytes instead of GuidString
    [ProtoMember(5, DataFormat = DataFormat.FixedSize)] public List<Guid> Fixed { get; set; }
}

// WellKnown promotes a level-200 member to 240, which is the other route to Timestamp/Duration
[ProtoContract]
public class WellKnownLists
{
    [ProtoMember(1, DataFormat = DataFormat.WellKnown)] public List<DateTime> Dates { get; set; }
    [ProtoMember(2, DataFormat = DataFormat.WellKnown)] public List<TimeSpan> Spans { get; set; }

    // for contrast: no format, so the level-200 bcl.proto form and no element serializer
    [ProtoMember(3)] public List<DateTime> Plain { get; set; }
}

[ProtoContract]
public class CompatMaps
{
    [ProtoMember(1, DataFormat = DataFormat.WellKnown)]
    public Dictionary<int, DateTime> ViaMember { get; set; }

    [ProtoMember(2)]
    [ProtoMap(ValueFormat = DataFormat.WellKnown)]
    public Dictionary<int, DateTime> ViaMap { get; set; }

    [ProtoMember(3)]
    public Dictionary<int, DateTime> Plain { get; set; }
}

[ProtoContract]
[CompatibilityLevel(CompatibilityLevel.Level300)]
public class Level300Map
{
    [ProtoMember(1)] public Dictionary<int, Guid> ByIndex { get; set; }
    [ProtoMember(2)] public Dictionary<Guid, int> ByGuid { get; set; }
}

public static class CompatElementsSamples
{
    private static readonly Guid Sample = new("11111111-2222-3333-4444-555555555555");
    private static readonly DateTime When = new(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static object[] Values =>
    [
        new Level300Lists(),
        new Level300Lists
        {
            Guids = [Sample, Guid.Empty],
            Decimals = [1.5m, 0m],
            Dates = [When, default],
            Spans = [TimeSpan.FromMinutes(3), TimeSpan.Zero],
            Fixed = [Sample],
        },
        new WellKnownLists(),
        new WellKnownLists { Dates = [When], Spans = [TimeSpan.FromSeconds(90)], Plain = [When] },
        new CompatMaps(),
        new CompatMaps
        {
            ViaMember = new Dictionary<int, DateTime> { { 1, When } },
            ViaMap = new Dictionary<int, DateTime> { { 2, When } },
            Plain = new Dictionary<int, DateTime> { { 3, When } },
        },
        new Level300Map(),
        new Level300Map
        {
            ByIndex = new Dictionary<int, Guid> { { 1, Sample } },
            ByGuid = new Dictionary<Guid, int> { { Sample, 1 } },
        },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Level300Lists))]
[ProtoSerializable(typeof(WellKnownLists))]
[ProtoSerializable(typeof(CompatMaps))]
[ProtoSerializable(typeof(Level300Map))]
public partial class CompatElementsModel : TypeModel
{
}
