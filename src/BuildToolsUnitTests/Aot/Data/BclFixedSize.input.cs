using ProtoBuf;
using ProtoBuf.Meta;
using System;

// gap B26, item 1: DataFormat.FixedSize on DateTime/TimeSpan selects a Fixed64 header, under which
// BclHelpers writes the flat eight-byte form rather than a message - so the member is tag + 8 bytes
// with no length prefix, and the measure is a folded constant.
//
// The measure emitter reaches BCL kinds from three separate places - the nullable path, the tuple
// path, and the main switch - each calling RawScalarMeasure, which has nothing for these kinds and
// is dereferenced with `!`. Missing one emits `len += 1 + ;`, which compiles nowhere; that has
// bitten three times (DateOnly, the level-200 pair, then Guid/decimal via an unrelated fixture).
//
// Two of the three are pinned here - the main switch and the nullable path. The TUPLE path is NOT,
// and cannot be: that branch fires on `contract.IsTuple` for a tuple's own synthesised members,
// which carry no attributes and therefore no DataFormat, so a fixed-width member is unreachable
// through it today. The arm there is defensive only. The tuple member below still earns its place -
// it proves a BCL-carrying tuple measures correctly alongside these - but it does not exercise that
// third path, and saying otherwise would be a coverage claim this fixture cannot support.
namespace AotFixtures.BclFixedSize;

[ProtoContract]
public class Fixed
{
    [ProtoMember(1, DataFormat = DataFormat.FixedSize)] public DateTime When { get; set; }
    [ProtoMember(2, DataFormat = DataFormat.FixedSize)] public TimeSpan Took { get; set; }

    // the nullable path
    [ProtoMember(3, DataFormat = DataFormat.FixedSize)] public DateTime? MaybeWhen { get; set; }
    [ProtoMember(4, DataFormat = DataFormat.FixedSize)] public TimeSpan? MaybeTook { get; set; }

    // the tuple path
    [ProtoMember(5)] public (DateTime, TimeSpan) Pair { get; set; }

    // the default-format twins, so a golden reader can see the two framings side by side: these stay
    // length-prefixed and keep their real measure
    [ProtoMember(6)] public DateTime PlainWhen { get; set; }
    [ProtoMember(7)] public TimeSpan PlainTook { get; set; }
}

public static class BclFixedSizeSamples
{
    public static object[] Values =>
    [
        new Fixed(),
        new Fixed
        {
            When = new DateTime(2026, 8, 19, 10, 11, 12, DateTimeKind.Utc),
            Took = TimeSpan.FromMilliseconds(1234),
            MaybeWhen = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc),
            MaybeTook = TimeSpan.FromSeconds(-90),
            Pair = (new DateTime(1999, 12, 31, 23, 59, 58, DateTimeKind.Utc), TimeSpan.FromHours(2)),
            PlainWhen = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PlainTook = TimeSpan.FromMinutes(5),
        },
        // a nullable that is present but zero: presence decides, so it is written and measured
        new Fixed { MaybeWhen = default(DateTime), MaybeTook = TimeSpan.Zero },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Fixed))]
public partial class BclFixedSizeModel : TypeModel { }
