using ProtoBuf;
using ProtoBuf.Meta;

// one namespace per fixture: every fixture is linked into a single assembly by both
// AotRefGen and AotConformanceTests, so unqualified type names would collide
namespace AotFixtures.Simple;

[ProtoContract]
public class Order
{
    [ProtoMember(1)]
    public int Id { get; set; }

    [ProtoMember(2)]
    public string Name { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Order))]
public partial class SimpleModel : TypeModel
{
}

/// <summary>
/// Values exercised by the differential tests. Chosen, not randomised: the interesting cases are
/// the boundaries the emitted guards turn on (default vs non-default, null vs empty).
/// </summary>
public static class SimpleSamples
{
    public static object[] Values =>
    [
        new Order(),                                            // all defaults - writes nothing at all
        new Order { Id = 1, Name = "abc" },
        new Order { Id = -1, Name = "" },                       // empty string is not null
        new Order { Id = int.MinValue, Name = null },           // null string is skipped entirely
        new Order { Id = int.MaxValue, Name = "é中" }, // multi-byte utf-8
    ];
}
