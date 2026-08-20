using ProtoBuf;
using ProtoBuf.Meta;

// gap B30 item 5: DataFormat.ZigZag on a decimal. protobuf-net IGNORES the format for decimal -
// ValueMember's Decimal arm sets WireType.String unconditionally and calls DecimalSerializer.Create
// with no format argument - so the runtime model serializes this quite happily. The generator used
// to refuse it, which made a real JIT/AOT divergence: runtime works, generated model drops the
// contract and cascades to its referrers.
//
// The refusal was a deliberate over-reach (ZigZag DOES throw for the other three BCL kinds), and it
// pre-dates [ProtoDataFormat]; that attribute only made it reachable without anyone typing ZigZag.
//
// This fixture is the proof either way: AotConformanceTests compares our bytes against ref-emit's,
// so if the runtime actually refused this, the comparison could not pass.
namespace AotFixtures.DecimalZigZag;

[ProtoContract]
public class Prices
{
    [ProtoMember(1, DataFormat = DataFormat.ZigZag)] public decimal Ignored { get; set; }
    [ProtoMember(2)] public decimal Plain { get; set; }
    // a referrer, so a cascade would be visible if the refusal came back
    [ProtoMember(3)] public string Label { get; set; }
}

public static class DecimalZigZagSamples
{
    public static object[] Values =>
    [
        new Prices(),
        new Prices { Ignored = 12345.6789m, Plain = -0.5m, Label = "both" },
        new Prices { Ignored = 0m, Plain = 79228162514264337593543950335m },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Prices))]
public partial class DecimalZigZagModel : TypeModel { }
