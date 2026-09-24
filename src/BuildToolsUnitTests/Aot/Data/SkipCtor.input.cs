using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.SkipCtor;

// the constructor is bypassed entirely on read, so Marker stays 0 rather than becoming 42
[ProtoContract(SkipConstructor = true)]
public class Bypassed
{
    public Bypassed() => Marker = 42;

    [ProtoMember(1)] public int Value { get; set; }
    [ProtoMember(2)] public string Text { get; set; }

    // deliberately not serialized: its value is what shows whether the constructor ran
    public int Marker { get; set; }
}

// contrast: same shape, constructor honoured
[ProtoContract]
public class Constructed
{
    public Constructed() => Marker = 42;

    [ProtoMember(1)] public int Value { get; set; }

    public int Marker { get; set; }
}

public static class SkipCtorSamples
{
    public static object[] Values =>
    [
        new Bypassed(),
        new Bypassed { Value = 1, Text = "a" },
        new Constructed(),
        new Constructed { Value = 2 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Bypassed))]
[ProtoSerializable(typeof(Constructed))]
public partial class SkipCtorModel : TypeModel
{
}
