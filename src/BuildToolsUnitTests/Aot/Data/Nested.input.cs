using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.Nested;

[ProtoContract]
public class Address
{
    [ProtoMember(1)]
    public string City { get; set; }
}

[ProtoContract]
public class Customer
{
    [ProtoMember(1)]
    public int Id { get; set; }

    // Address is never seeded directly - it must arrive via the transitive closure
    [ProtoMember(2)]
    public Address Address { get; set; }
}

[ProtoContract]
public class Invoice
{
    [ProtoMember(1)]
    public int Number { get; set; }

    [ProtoMember(2)]
    public Customer Customer { get; set; }

    // second path to Address, so the closure has to de-duplicate
    [ProtoMember(3)]
    public Address ShipTo { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Invoice))]
[ProtoSerializable(typeof(Customer))] // both a seed and reachable from Invoice: also de-duplicated
public partial class NestedModel : TypeModel
{
}

public static class NestedSamples
{
    public static object[] Values =>
    [
        new Invoice(),
        new Invoice { Number = 7 },
        new Invoice { Number = 7, Customer = new Customer { Id = 1 } },
        // an all-default sub-message is not the same as an absent one
        new Invoice { Number = 7, ShipTo = new Address() },
        new Invoice
        {
            Number = 7,
            Customer = new Customer { Id = 1, Address = new Address { City = "Ipswich" } },
            ShipTo = new Address { City = "" },
        },
        new Customer { Id = 2, Address = new Address { City = "Cambridge" } },
    ];
}
