using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections;
using System.Collections.Generic;

namespace AotFixtures.ListLike;

// protobuf-net serializes anything that *looks* like a list as a collection even when it carries
// [ProtoContract] - its own members are ignored entirely. We refuse that shape rather than guess;
// IgnoreListHandling is the documented opt-out and makes it an ordinary message.
[ProtoContract(IgnoreListHandling = true)]
public class NotAList : IEnumerable<int>
{
    private readonly List<int> _items = new();
    public void Add(int value) => _items.Add(value);
    public IEnumerator<int> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    [ProtoMember(1)] public string Label { get; set; }
    [ProtoMember(2)] public int Count2 { get; set; }
}

[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public NotAList NotAList { get; set; }
    [ProtoMember(2)] public int Other { get; set; }
}

public static class ListLikeSamples
{
    public static object[] Values =>
    [
        new Holder(),
        new Holder { NotAList = new NotAList { Label = "a", Count2 = 1 } },
        new Holder { NotAList = new NotAList(), Other = 2 },
        new NotAList { Label = "b", Count2 = 3 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Holder))]
[ProtoSerializable(typeof(NotAList))]
public partial class ListLikeModel : TypeModel
{
}
