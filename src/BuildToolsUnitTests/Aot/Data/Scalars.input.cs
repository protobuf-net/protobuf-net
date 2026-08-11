using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.Scalars;

[ProtoContract]
public class Primitives
{
    [ProtoMember(1)] public bool Bool { get; set; }
    [ProtoMember(2)] public sbyte SByte { get; set; }
    [ProtoMember(3)] public byte Byte { get; set; }
    [ProtoMember(4)] public short Int16 { get; set; }
    [ProtoMember(5)] public ushort UInt16 { get; set; }
    [ProtoMember(6)] public int Int32 { get; set; }
    [ProtoMember(7)] public uint UInt32 { get; set; }
    [ProtoMember(8)] public long Int64 { get; set; }
    [ProtoMember(9)] public ulong UInt64 { get; set; }
    [ProtoMember(10)] public float Single { get; set; }
    [ProtoMember(11)] public double Double { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Primitives))]
public partial class ScalarsModel : TypeModel
{
}

public static class ScalarsSamples
{
    public static object[] Values =>
    [
        new Primitives(),
        new Primitives { Bool = true, SByte = -1, Byte = 255, Int16 = -2, UInt16 = 65535 },
        new Primitives { Int32 = -3, UInt32 = uint.MaxValue, Int64 = -4L, UInt64 = ulong.MaxValue },
        new Primitives { Single = 1.5f, Double = -2.25d },
        new Primitives
        {
            Bool = true,
            SByte = sbyte.MinValue, Byte = byte.MaxValue,
            Int16 = short.MinValue, UInt16 = ushort.MaxValue,
            Int32 = int.MinValue, UInt32 = uint.MaxValue,
            Int64 = long.MinValue, UInt64 = ulong.MaxValue,
            Single = float.MaxValue, Double = double.MinValue,
        },
    ];
}
