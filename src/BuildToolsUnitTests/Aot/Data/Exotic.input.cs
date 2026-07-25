using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace AotFixtures.Exotic;

// which RepeatedSerializer factory serves which collection is a lookup table taken from ref-emit.
// Two shapes: Create{X}<TCollection, TElement>() needs the declared type, Create{X}<TElement>() has
// it fixed by the factory (arrays, List<T>, and the immutable family).
[ProtoContract]
public class Exotics
{
    [ProtoMember(1)] public IList<int> Interface { get; set; }
    [ProtoMember(2)] public ICollection<int> Collection { get; set; }
    [ProtoMember(3)] public IEnumerable<int> Enumerable { get; set; }
    [ProtoMember(4)] public IReadOnlyList<int> ReadOnlyList { get; set; }

    [ProtoMember(5)] public HashSet<int> Set { get; set; }
    [ProtoMember(6)] public Queue<int> Queue { get; set; }
    [ProtoMember(7)] public Stack<int> Stack { get; set; }

    // a struct: neither side null-tests it
    [ProtoMember(8)] public ImmutableArray<int> ImmutableArray { get; set; }
    [ProtoMember(9)] public ImmutableList<int> ImmutableList { get; set; }
    [ProtoMember(10)] public IImmutableList<int> ImmutableInterface { get; set; }

    [ProtoMember(11)] public ConcurrentQueue<int> ConcurrentQueue { get; set; }
    [ProtoMember(12)] public ConcurrentBag<int> ConcurrentBag { get; set; }

    // and a string element, to prove the element type still drives the features
    [ProtoMember(13)] public IList<string> Strings { get; set; }
}

public static class ExoticSamples
{
    public static object[] Values =>
    [
        new Exotics(),
        new Exotics { Interface = [1, 2], Collection = [3], Enumerable = [4, 5] },
        new Exotics { ReadOnlyList = [6], Set = [7, 8] },
        new Exotics { Queue = new Queue<int>([9, 10]) },
        new Exotics { Stack = new Stack<int>([11, 12]) },
        new Exotics { ImmutableArray = [13, 14], ImmutableList = ImmutableList.Create(15) },
        new Exotics { ImmutableInterface = ImmutableList.Create(16, 17) },
        new Exotics { ConcurrentQueue = new ConcurrentQueue<int>([18]) },
        new Exotics { ConcurrentBag = new ConcurrentBag<int>([19]) },
        new Exotics { Strings = ["a", "b"] },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Exotics))]
public partial class ExoticModel : TypeModel
{
}
