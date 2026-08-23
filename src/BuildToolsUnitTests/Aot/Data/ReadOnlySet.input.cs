// IReadOnlySet<T>, whose RepeatedSerializer factory lives only in the net6.0+ build of the library -
// so the generator probes for the *factory*, not the language type, and falls back to the 3.x
// spelling CreateReadOnySet (sic, an [Obsolete] forwarder) when only an older Core is referenced.
//
// THIS FIXTURE EXISTS BECAUSE THAT FALLBACK HAD NO TEST AT ALL. It was found by asking what the
// non-seedable slice of the differential corpus contains that nothing else covers: the only
// IReadOnlySet contract in the whole corpus is Examples.ReadOnlySetSerializerTests.ReadOnlySetData<T>,
// which is both non-public AND open-generic, so a typeof(...) in another assembly cannot name it and
// the corpus skips it. Probed against RuntimeTypeModel first, which builds and round-trips it - so
// this was a genuine coverage hole rather than a shape protobuf-net declines.
//
// Treated exactly like DateOnly.input.cs, and for the same reason: <Compile Remove>d from AotRefGen,
// which is net472 and has no IReadOnlySet at all, so it has no .reference.cs - deliberate, not
// neglect. The golden here is a drop (the golden tests compile against the netstandard2.0 BuildTools
// assembly, which has the language type but not the factory); the differential suite on net8.0 is
// where it is really exercised.

using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.ReadOnlySets;

[ProtoContract]
public class Sets
{
    [ProtoMember(1)] public IReadOnlySet<int> Numbers { get; set; }

    // a STRING element as well as a scalar one: the element kind picks a different read/write arm,
    // and the set factory is the thing under test in both
    [ProtoMember(2)] public IReadOnlySet<string> Names { get; set; }

    // beside an ordinary member, so the contract's own length is exercised rather than only the set
    [ProtoMember(3)] public int Tag { get; set; }
}

// a MESSAGE element, which resolves its element serializer rather than inlining a scalar
[ProtoContract]
public class Item
{
    [ProtoMember(1)] public int Id { get; set; }
}

[ProtoContract]
public class MessageSets
{
    [ProtoMember(1)] public IReadOnlySet<Item> Items { get; set; }
}

public static class ReadOnlySetSamples
{
    public static object[] Values =>
    [
        new Sets(),
        new Sets { Numbers = new HashSet<int> { 1, 2, 3 } },
        // a single element, and an EMPTY set - which is not the same as null on the wire
        new Sets { Numbers = new HashSet<int> { 7 } },
        new Sets { Numbers = new HashSet<int>() },
        new Sets { Names = new HashSet<string> { "a", "bb" } },
        // an empty string element is written, unlike a null one
        new Sets { Names = new HashSet<string> { "" } },
        new Sets { Numbers = new HashSet<int> { 4, 5 }, Names = new HashSet<string> { "x" }, Tag = 9 },
        new Sets { Tag = 11 },
        new MessageSets(),
        new MessageSets { Items = new HashSet<Item> { new() { Id = 1 }, new() { Id = 2 } } },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Sets))]
[ProtoSerializable(typeof(MessageSets))]
public partial class ReadOnlySetModel : TypeModel { }
