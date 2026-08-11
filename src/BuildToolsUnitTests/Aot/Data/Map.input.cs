using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace AotFixtures.Map;

// dictionaries resolve through the same provider walk as any other collection, but land on a
// MapSerializer factory and carry two element types. Everything here is a *valid* protobuf map:
// an integral or string key, and a value that is not itself repeated.
public enum Shade { None = 0, Red = 1, Green = 2 }

[ProtoContract]
public class Payload
{
    [ProtoMember(1)] public int Id { get; set; }
}

[ProtoContract]
public class Maps
{
    [ProtoMember(1)] public Dictionary<int, string> Scalars { get; set; }

    // a message value is passed `this` as its serializer, positionally after a null for the key
    [ProtoMember(2)] public Dictionary<string, Payload> Messages { get; set; }

    // Dictionary<K,V> alone gets the two-arg factory; everything else needs the collection type too
    [ProtoMember(3)] public IDictionary<int, int> Interface { get; set; }
    [ProtoMember(4)] public IReadOnlyDictionary<int, int> ReadOnly { get; set; }
    [ProtoMember(5)] public ConcurrentDictionary<int, int> Concurrent { get; set; }
    [ProtoMember(6)] public ImmutableDictionary<int, int> Immutable { get; set; }
    [ProtoMember(7)] public ImmutableSortedDictionary<int, int> ImmutableSorted { get; set; }
    [ProtoMember(8)] public IImmutableDictionary<int, int> ImmutableInterface { get; set; }

    // SortedDictionary is not in the table at all: it matches through IDictionary<K,V>
    [ProtoMember(9)] public SortedDictionary<int, int> Sorted { get; set; }

    // the collection options compose exactly as they do for a repeated member
    [ProtoMember(10, OverwriteList = true)] public Dictionary<int, int> Overwrite { get; set; }
    [ProtoMember(11, IsPacked = true)] public Dictionary<int, int> Packed { get; set; }

    // a nullable value needs no null-wrapping: PrimaryTypeProvider serves ISerializer<int?> directly
    [ProtoMember(12)] public Dictionary<int, int?> NullableValue { get; set; }
    [ProtoMember(13)] public Dictionary<int, byte[]> BytesValue { get; set; }

    // DataFormat selects the root wire type, and Group is the only value that changes anything
    [ProtoMember(14, DataFormat = DataFormat.Group)] public Dictionary<int, int> Grouped { get; set; }

    // ... FixedSize on a map is simply ignored; the per-key format comes from [ProtoMap]
    [ProtoMember(15, DataFormat = DataFormat.FixedSize)] public Dictionary<int, int> Fixed { get; set; }

    // An enum on either side. Like a repeated enum, a map resolves the element serializer from the
    // *model* rather than taking it inline, so each of these needs an ISerializerProxy<TEnum> on the
    // services type; without one the failure is a runtime "no serializer for type", not a build
    // error. The wire type is the underlying scalar's.
    [ProtoMember(16)] public Dictionary<Shade, int> EnumKey { get; set; }
    [ProtoMember(17)] public Dictionary<int, Shade> EnumValue { get; set; }
    [ProtoMember(18)] public Dictionary<Shade, Shade> EnumBoth { get; set; }
}

public static class MapSamples
{
    public static object[] Values =>
    [
        new Maps(),
        new Maps { Scalars = new() { [1] = "a" } },
        new Maps { Messages = new() { ["k"] = new Payload { Id = 2 } } },
        new Maps { Interface = new Dictionary<int, int> { [3] = 4 } },
        new Maps { ReadOnly = new Dictionary<int, int> { [5] = 6 } },
        new Maps { Concurrent = new ConcurrentDictionary<int, int>([new(7, 8)]) },
        new Maps { Immutable = ImmutableDictionary.Create<int, int>().Add(9, 10) },
        new Maps { ImmutableSorted = ImmutableSortedDictionary.Create<int, int>().Add(11, 12).Add(13, 14) },
        new Maps { ImmutableInterface = ImmutableDictionary.Create<int, int>().Add(15, 16) },
        new Maps { Sorted = new SortedDictionary<int, int> { [17] = 18, [19] = 20 } },
        new Maps { Overwrite = new() { [21] = 22 } },
        new Maps { EnumKey = new() { [Shade.Red] = 1, [Shade.None] = 0 } },
        new Maps { EnumValue = new() { [1] = Shade.Green, [2] = Shade.None } },
        new Maps { EnumBoth = new() { [Shade.Green] = Shade.Red } },
        new Maps { Packed = new() { [23] = 24 } },
        new Maps { NullableValue = new() { [25] = 26 } },
        new Maps { NullableValue = new() { [27] = null } },
        new Maps { BytesValue = new() { [28] = [1, 2, 3] } },
        new Maps { Grouped = new() { [29] = 30 } },
        new Maps { Fixed = new() { [31] = 32 } },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Maps))]
public partial class MapModel : TypeModel
{
}
