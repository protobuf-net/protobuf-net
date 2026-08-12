// Not a diagnostics fixture: it lives here because this folder is golden-only (excluded from
// AotRefGen and AotConformanceTests). Post-swap the raw read pass gates on the REAL
// ProtoReader.State raw surface (present in the compiled-in Core sources), so no stub is
// needed; the golden output shows the second emission pass, side by side with the first.
using ProtoBuf;
using System.Collections.Generic;

namespace AotFixtures.RawPass
{
    public enum Status
    {
        Unknown = 0,
        Open = 1,
        Closed = 2,
    }

    [ProtoContract]
    public class Order
    {
        [ProtoMember(1)] public int Id { get; set; }
        [ProtoMember(2)] public string Name { get; set; }
        [ProtoMember(3)] public bool Active { get; set; }
        [ProtoMember(4)] public long Total { get; set; }
        [ProtoMember(5)] public Status Status { get; set; }
        [ProtoMember(6)] public int? Priority { get; set; }
        [ProtoMember(7)] public List<string> Tags { get; } = new List<string>();
        [ProtoMember(8)] public List<int> Codes { get; } = new List<int>();
        [ProtoMember(9)] public List<Child> Items { get; } = new List<Child>();
        [ProtoMember(10)] public Child Favourite { get; set; }
        [ProtoMember(11)] public double Score { get; set; }
        [ProtoMember(12)] public byte[] Blob { get; set; }
    }

    [ProtoContract]
    public class Child
    {
        [ProtoMember(1)] public int Value { get; set; }
    }

    // ineligible for the raw read pass (map member), and the CASCADE is the point: Chain references
    // it through a message member, so Chain falls out of the raw-read set too even though Chain
    // is eligible alone - emitting a call to a RawRead_ method that does not exist would be a
    // compile error in the consumer's build
    [ProtoContract]
    public class Holder
    {
        [ProtoMember(1)] public Dictionary<int, string> Lookup { get; } = new Dictionary<int, string>();
    }

    [ProtoContract]
    public class Chain
    {
        [ProtoMember(1)] public Holder Inner { get; set; }
    }

    // extensible: the default case CAPTURES unknown fields instead of skipping them
    [ProtoContract]
    public class Bag : Extensible
    {
        [ProtoMember(1)] public int Id { get; set; }
    }

    [ProtoModel]
    [ProtoSerializable(typeof(Order))]
    [ProtoSerializable(typeof(Holder))]
    [ProtoSerializable(typeof(Chain))]
    [ProtoSerializable(typeof(Bag))]
    public partial class RawPassModel : ProtoBuf.Meta.TypeModel
    {
    }
}
