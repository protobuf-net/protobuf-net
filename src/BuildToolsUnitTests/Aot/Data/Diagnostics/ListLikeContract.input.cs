using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections;
using System.Collections.Generic;

// a [ProtoContract] that also looks like a list: protobuf-net would serialize it as a *collection*
// and ignore Label entirely, so we refuse it rather than emit a message and disagree on the wire.
// The referring contract then drops by cascade, which is what PBN2004 reports.
namespace AotFixtures.ListLikeContract;

[ProtoContract]
public class Basket : IEnumerable<int>
{
    private readonly List<int> _items = new();
    public void Add(int value) => _items.Add(value);
    public IEnumerator<int> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    [ProtoMember(1)] public string Label { get; set; }
}

[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public Basket Basket { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Holder))]
public partial class ListLikeContractModel : TypeModel
{
}
