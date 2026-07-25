using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections;
using System.Collections.Generic;

namespace AotFixtures.Derived;

// which factory serves a collection is decided by walking the base types and then the interfaces
// against a priority-ordered table, where most entries are *exact-only* - they apply to the member's
// own type but not to anything deriving from it. That is why MySet, despite being a HashSet, lands
// on CreateEnumerable while MyQueue keeps CreateQueue.
public class MyList : List<int> { }

public class MySet : HashSet<int> { }

public class MyQueue : Queue<int> { }

// two matches at the same priority resolve to two different serializers, so ref-emit treats the type
// as not-a-collection at all - which leaves an ordinary message, members and all
[ProtoContract]
public class Ambiguous : IEnumerable<int>, IEnumerable<string>
{
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => null;
    IEnumerator<string> IEnumerable<string>.GetEnumerator() => null;
    IEnumerator IEnumerable.GetEnumerator() => null;

    [ProtoMember(1)] public string Label { get; set; }
}

[ProtoContract]
public class Derives
{
    [ProtoMember(1)] public MyList List { get; set; }
    [ProtoMember(2)] public MySet Set { get; set; }
    [ProtoMember(3)] public MyQueue Queue { get; set; }
    [ProtoMember(4)] public Ambiguous Ambiguous { get; set; }
}

public static class DerivedSamples
{
    public static object[] Values =>
    [
        new Derives(),
        new Derives { List = new MyList { 1, 2 } },
        new Derives { Set = new MySet { 3 } },
        new Derives { Queue = new MyQueue() },
        new Derives { Ambiguous = new Ambiguous { Label = "a" } },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Derives))]
public partial class DerivedModel : TypeModel
{
}
