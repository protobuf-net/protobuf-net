using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.Generic;

// A *closed* generic is an ordinary contract: Roslyn hands us its members already substituted, so
// Wrapper<int> and Wrapper<string> are simply two contracts that happen to share a definition. Only
// an open one is refused, since the services type is a single non-generic class with nowhere to put
// a type parameter.
[ProtoContract]
public class Wrapper<T>
{
    [ProtoMember(1)] public T Value { get; set; }
    [ProtoMember(2)] public string Label { get; set; }
}

[ProtoContract]
public class Nested
{
    [ProtoMember(1)] public int Id { get; set; }
}

// two instantiations of one definition, plus a message-typed one and a nested construction - each
// is its own entry in the model, with its own ISerializer<>
[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public Wrapper<int> Number { get; set; }
    [ProtoMember(2)] public Wrapper<string> Text { get; set; }
    [ProtoMember(3)] public Wrapper<Nested> Message { get; set; }
    [ProtoMember(4)] public Wrapper<Wrapper<int>> Deep { get; set; }

    // the substituted member is itself a collection, which resolves as one in the usual way
    [ProtoMember(5)] public Wrapper<List<int>> Many { get; set; }

    // a generic *struct*, so the substituted contract is a value type
    [ProtoMember(6)] public Pair<int, string> Pair { get; set; }

    // ...and a collection of a closed generic, to prove the element resolves as a message
    [ProtoMember(7)] public List<Wrapper<int>> Wrappers { get; set; }
}

[ProtoContract]
public struct Pair<TKey, TValue>
{
    [ProtoMember(1)] public TKey Key { get; set; }
    [ProtoMember(2)] public TValue Value { get; set; }
}

// a closed generic can be a seed in its own right, not only something reached from one
[ProtoContract]
public class Standalone<T>
{
    [ProtoMember(1)] public T Item { get; set; }
}

public static class GenericSamples
{
    public static object[] Values =>
    [
        new Holder(),
        new Holder { Number = new Wrapper<int> { Value = 42, Label = "n" } },
        new Holder { Text = new Wrapper<string> { Value = "hi", Label = "t" } },
        new Holder { Message = new Wrapper<Nested> { Value = new Nested { Id = 7 } } },
        new Holder { Deep = new Wrapper<Wrapper<int>> { Value = new Wrapper<int> { Value = 3 } } },
        new Holder { Many = new Wrapper<List<int>> { Value = [1, 2, 3] } },
        new Holder { Pair = new Pair<int, string> { Key = 1, Value = "one" } },
        new Holder { Wrappers = [new Wrapper<int> { Value = 8 }, new Wrapper<int> { Value = 9 }] },
        new Standalone<int> { Item = 5 },
        new Standalone<string> { Item = "five" },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Holder))]
[ProtoSerializable(typeof(Standalone<int>))]
[ProtoSerializable(typeof(Standalone<string>))]
public partial class GenericModel : TypeModel
{
}
