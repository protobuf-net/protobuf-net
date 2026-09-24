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

public static class FormatsSamples
{
    public static object[] Values =>
    [
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
public partial class FormatsModel : TypeModel
{
}
