using ProtoBuf;
using ProtoBuf.Meta;
using System;

// gap B26, item 3: the level-300 BCL forms. Guid becomes GuidString (36 chars) or, with
// DataFormat.FixedSize, GuidBytes (16); decimal becomes DecimalString, which is the only
// value-dependent one. All three stay length-prefixed, so the emitted shape is the usual
// tag + varint(len) + len - only the body measure differs.
//
// Guid.Empty and 0m are worth their own samples: the writer short-circuits an empty Guid to an
// EMPTY payload rather than 36 zeroed characters, so its measure is 0, and getting that wrong would
// be invisible on a populated sample. The guarded members never write it, which is exactly why
// Required exists below - IsRequired drops the guard, so the empty case actually reaches the wire.
namespace AotFixtures.BclLevel300;

[ProtoContract, CompatibilityLevel(CompatibilityLevel.Level300)]
public class Level300
{
    [ProtoMember(1)] public Guid AsString { get; set; }
    [ProtoMember(2, DataFormat = DataFormat.FixedSize)] public Guid AsBytes { get; set; }
    [ProtoMember(3)] public decimal Amount { get; set; }

    // the nullable path, which is a separate arm of the measure emitter
    [ProtoMember(4)] public Guid? MaybeGuid { get; set; }
    [ProtoMember(5)] public decimal? MaybeAmount { get; set; }

    // IsRequired drops the write guard, so an EMPTY Guid and a zero decimal are written and must
    // measure - the empty Guid as a zero-length payload
    [ProtoMember(6, IsRequired = true)] public Guid AlwaysGuid { get; set; }
    [ProtoMember(7, IsRequired = true)] public decimal AlwaysAmount { get; set; }
}

public static class BclLevel300Samples
{
    public static object[] Values =>
    [
        new Level300(),
        new Level300
        {
            AsString = Guid.Parse("c416e4af-455e-414c-948c-f27873263547"),
            AsBytes = Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00"),
            Amount = 12345.6789m,
            MaybeGuid = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"),
            MaybeAmount = -0.0001m,
            AlwaysGuid = Guid.Parse("55667788-99aa-bbcc-ddee-ff0011223344"),
            AlwaysAmount = 1m,
        },
        // the empty/zero cases, reachable only through the unguarded members
        new Level300 { MaybeGuid = Guid.Empty, MaybeAmount = 0m },
        // decimals whose formatted length varies: the only value-dependent measure here
        new Level300 { Amount = 0.1m, AlwaysAmount = -79228162514264337593543950335m },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Level300))]
public partial class BclLevel300Model : TypeModel { }
