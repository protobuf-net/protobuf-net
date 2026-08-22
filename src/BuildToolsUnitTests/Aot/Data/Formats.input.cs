using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.Formats;

[ProtoContract]
public class Inner
{
    [ProtoMember(1)] public int Value { get; set; }
}

[ProtoContract]
public class Formatted
{
    // DataFormat selects the wire type; ZigZag additionally needs a state.Hint on read
    [ProtoMember(1, DataFormat = DataFormat.ZigZag)] public int ZigZagInt { get; set; }
    [ProtoMember(2, DataFormat = DataFormat.FixedSize)] public int FixedInt { get; set; }
    [ProtoMember(3, DataFormat = DataFormat.ZigZag)] public long ZigZagLong { get; set; }
    [ProtoMember(4, DataFormat = DataFormat.FixedSize)] public long FixedLong { get; set; }

    // TwosComplement is byte-identical to the default
    [ProtoMember(5, DataFormat = DataFormat.TwosComplement)] public int TwosComplement { get; set; }

    // IsRequired drops the write guard; it does not affect the read
    [ProtoMember(6, IsRequired = true)] public int RequiredInt { get; set; }
    [ProtoMember(7, IsRequired = true)] public string RequiredString { get; set; }

    // group encoding differs on the write only
    [ProtoMember(8, DataFormat = DataFormat.Group)] public Inner Grouped { get; set; }
    [ProtoMember(9)] public Inner Plain { get; set; }

    // ... and on a collection it is only a features swap
    [ProtoMember(10, DataFormat = DataFormat.ZigZag)] public int[] ZigZagArray { get; set; }
    [ProtoMember(11, DataFormat = DataFormat.FixedSize, IsPacked = true)] public long[] PackedFixed { get; set; }
}

// The same formats with nothing UNMEASURABLE beside them. Formatted above carries an unpacked
// repeated ZigZag member, which is blocked, and one blocked member takes the whole contract - so it
// can say nothing about whether a formatted scalar measures. This one exists to pin that.
//
// The write is unchanged and stays stateful (WriteFieldHeader + WriteInt32/64); only the SIZE is
// arithmetic. ZigZag is wire type 0 like any varint - SignedVarint is a protobuf-net distinction,
// not a wire one - while FixedSize is 5 or 1 by width, so a kind-only tag would misframe it.
[ProtoContract]
public class Sized
{
    [ProtoMember(1, DataFormat = DataFormat.ZigZag)] public int ZigInt { get; set; }
    [ProtoMember(2, DataFormat = DataFormat.ZigZag)] public long ZigLong { get; set; }
    [ProtoMember(3, DataFormat = DataFormat.FixedSize)] public int FixInt { get; set; }
    [ProtoMember(4, DataFormat = DataFormat.FixedSize)] public long FixLong { get; set; }
    [ProtoMember(5, DataFormat = DataFormat.FixedSize)] public uint FixUInt { get; set; }
    [ProtoMember(6, DataFormat = DataFormat.ZigZag)] public int? NullableZig { get; set; }
    // past 15, so a two-byte tag has to be folded correctly alongside the format's wire bits
    [ProtoMember(20, DataFormat = DataFormat.FixedSize)] public int FarFixed { get; set; }
    [ProtoMember(7)] public int Plain { get; set; }
}

[ProtoContract]
public class SizedHolder
{
    [ProtoMember(1)] public Sized Inner { get; set; }
    [ProtoMember(2)] public int Tag { get; set; }
}

public static class FormatsSamples
{
    public static object[] Values =>
    [
        new Sized(),
        // negatives are the point of ZigZag: -1 is one byte zig-zagged and ten bytes sign-extended
        new Sized { ZigInt = -1, ZigLong = -1, FixInt = -1, FixLong = -1, FixUInt = 7, NullableZig = -3, FarFixed = -9, Plain = 5 },
        new Sized { ZigInt = 150, ZigLong = long.MinValue, FixInt = int.MaxValue, FixLong = long.MaxValue },
        // a nullable zero is written on presence, where a plain zero is not
        new Sized { NullableZig = 0 },
        new SizedHolder(),
        new SizedHolder { Inner = new Sized { ZigInt = -2, FixLong = 3 }, Tag = 4 },
        new Formatted(),                                         // required members still written
        new Formatted { ZigZagInt = -1, FixedInt = -2, ZigZagLong = -3L, FixedLong = -4L },
        new Formatted { ZigZagInt = int.MinValue, ZigZagLong = long.MaxValue },
        new Formatted { TwosComplement = 5, RequiredInt = 0, RequiredString = null },
        new Formatted { RequiredInt = 6, RequiredString = "x" },
        new Formatted { Grouped = new Inner { Value = 7 }, Plain = new Inner { Value = 8 } },
        new Formatted { ZigZagArray = [-1, 0, 1], PackedFixed = [-2L, 2L] },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Formatted))]
[ProtoSerializable(typeof(Sized))]
[ProtoSerializable(typeof(SizedHolder))]
public partial class FormatsModel : TypeModel
{
}
