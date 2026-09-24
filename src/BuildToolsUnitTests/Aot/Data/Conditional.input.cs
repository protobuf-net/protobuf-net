using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.Conditional;

[ProtoContract]
public class Conditional
{
    [ProtoMember(1)] public int Value { get; set; }
    public bool ValueSpecified { get; set; }

    [ProtoMember(2)] public string Text { get; set; }
    public bool ShouldSerializeText() => !string.IsNullOrEmpty(Text);

    // both conventions on one member: which wins?
    [ProtoMember(3)] public int Both { get; set; }
    public bool BothSpecified { get; set; }
    public bool ShouldSerializeBoth() => Both > 0;

    // a reference type with Specified, to see whether the null guard survives
    [ProtoMember(4)] public string Named { get; set; }
    public bool NamedSpecified { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Conditional))]
public partial class ConditionalModel : TypeModel
{
}

public static class ConditionalSamples
{
    public static object[] Values =>
    [
        new Conditional(),
        // an explicit zero *is* written when Specified says so - that is the point of the convention
        new Conditional { Value = 0, ValueSpecified = true },
        new Conditional { Value = 7, ValueSpecified = true },
        // ... and a non-zero is omitted when it does not
        new Conditional { Value = 9, ValueSpecified = false },
        new Conditional { Text = "a" },
        new Conditional { Text = "" },
        new Conditional { Both = 3, BothSpecified = true },
        new Conditional { Named = "b", NamedSpecified = true },
        new Conditional { Named = null, NamedSpecified = true },
    ];
}
