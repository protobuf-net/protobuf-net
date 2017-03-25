using ProtoBuf;

namespace demo_rpc_client_silverlight.Northwind
{
    [ProtoContract(DataMemberOffset = 1)]
    partial class Customer {}

    [ProtoContract(DataMemberOffset = 1)]
    partial class Order {}

    [ProtoContract(DataMemberOffset = 1)]
    [ProtoPartialMember(1, "OrderID")]
    [ProtoPartialMember(2, "ProductID")]
    [ProtoPartialMember(3, "UnitPrice")]
    partial class Order_Detail { }
}
