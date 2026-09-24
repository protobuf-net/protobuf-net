using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;

namespace AotFixtures.MapNested;

[ProtoContract]
public class Leaf
{
    [ProtoMember(1)] public int Id { get; set; }
}

// protobuf-net refuses a nested collection almost everywhere - a List<List<int>> throws - but
// exempts dictionaries specifically (RepeatedSerializerStub.TestIfNestedNotSupported), so a
// Dictionary<K, List<V>> is legal. Note such a shape is *not* a valid protobuf map, so it also picks
// up OptionFailOnDuplicateKey.
[ProtoContract]
public class Nested
{
    [ProtoMember(1)] public Dictionary<int, List<int>> Lists { get; set; }
    [ProtoMember(2)] public Dictionary<long, long[]> Arrays { get; set; }
    // a nested *map* value works the same way: MapSerializer is also an IRepeatedSerializer, so it
    // is an ISerializer<Dictionary<..>> and the model can serve one
    [ProtoMember(3)] public Dictionary<string, Dictionary<string, string>> Maps { get; set; }

    // a float key is not valid for a protobuf map either, and combines with the above
    [ProtoMember(4)] public Dictionary<float, List<int>> FloatKeyed { get; set; }

    // A *message* element is where this went wrong (#1337). Every nested value passes no
    // serializer at all, message element or not: ref-emit writes `this as ISerializer<List<Leaf>>`,
    // which is null at run time (the services type implements ISerializer<KeyValuePair<int, Leaf>>,
    // not ISerializer<List<Leaf>>), so both paths land on `serializer ??= GetSerializer<T>(Model)`
    // and find the ISerializerProxy<List<Leaf>> above. Saying `this` instead - which is what a
    // message element used to do, since the map plan's ValueKind describes the *element* - hands
    // over an ISerializer<Leaf> where an ISerializer<List<Leaf>> is wanted: CS1503 in the
    // consumer's build. It never got that far, because the drop cascade paired that same ValueKind
    // with the *collection's* name, found no contract called List<Leaf>, and removed the contract.
    [ProtoMember(5)] public Dictionary<int, List<Leaf>> Messages { get; set; }

    // ... and an auto-tuple element is a message like any other; both spellings were reported
    [ProtoMember(6)] public Dictionary<int, List<Tuple<int, string>>> Tuples { get; set; }
    [ProtoMember(7)] public Dictionary<int, List<(int, string)>> ValueTuples { get; set; }

    // a message reached only through a nested *map* value still has to be enqueued
    [ProtoMember(8)] public Dictionary<int, Dictionary<int, Leaf>> MappedMessages { get; set; }

    // A levelled BCL *element* is the other shape the mismatched pair reached: ValueKind says
    // DateTime while the type name says List<DateTime>, so above level 200 the value serializer
    // was rendered as GetInbuiltSerializer<List<DateTime>>(...) - the element's kind applied to
    // the collection's name. It resolves from the model like every other nested value.
    [ProtoMember(9), CompatibilityLevel(CompatibilityLevel.Level300)]
    public Dictionary<int, List<DateTime>> Stamps { get; set; }
}

public static class MapNestedSamples
{
    public static object[] Values =>
    [
        new Nested(),
        new Nested { Lists = new() { [1] = [2, 3] } },
        new Nested { Arrays = new() { [4L] = [5L, 6L] } },
        new Nested { Maps = new() { ["a"] = new() { ["b"] = "c" } } },
        new Nested { FloatKeyed = new() { [1.5f] = [7] } },
        new Nested { Messages = new() { [8] = [new Leaf { Id = 9 }, new Leaf { Id = 10 }] } },
        new Nested { Tuples = new() { [11] = [Tuple.Create(12, "a")] } },
        new Nested { ValueTuples = new() { [13] = [(14, "b")] } },
        new Nested { MappedMessages = new() { [15] = new() { [16] = new Leaf { Id = 17 } } } },
        new Nested { Stamps = new() { [18] = [new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc)] } },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Nested))]
public partial class MapNestedModel : TypeModel
{
}
