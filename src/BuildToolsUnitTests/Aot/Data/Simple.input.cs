using ProtoBuf;
using ProtoBuf.Meta;

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
