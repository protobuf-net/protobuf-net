using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AotFixtures.MapNested;

public enum Shade { None = 0, Red = 1, Green = 2 }

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

// Nested above is deliberately unmeasurable - a packable element, a nested map value and a BCL
// element are all still blocked - and one blocked member takes the whole contract out of the
// measure-first set, so it could never show the raw shape. This one carries only the nested values
// that ARE arithmetic, so it is measure-first and its Measure_ shows the per-element loop.
[ProtoContract]
public class RawNested
{
    [ProtoMember(1)] public Dictionary<int, List<Leaf>> Messages { get; set; }
    [ProtoMember(2)] public Dictionary<int, List<string>> Labels { get; set; }

    // A PACKABLE element takes the other shape: WriteRepeated packs on
    // (count == 0 || count > 1), so one element is never packed, and zero elements still emit the
    // two-byte zero-length header unless the model opts out. All three arms need a sample.
    [ProtoMember(3)] public Dictionary<int, List<int>> Counts { get; set; }
    [ProtoMember(4)] public Dictionary<int, long[]> Stamps { get; set; }
    [ProtoMember(5)] public Dictionary<int, List<double>> Rates { get; set; }
    [ProtoMember(6)] public Dictionary<int, List<bool>> Flags { get; set; }

    // an enum element arrives as its underlying kind and packs like any other integral, but needs
    // the cast to get there - and its serializer proxy, exactly as a repeated enum does
    [ProtoMember(7)] public Dictionary<int, List<Shade>> Shades { get; set; }

    [ProtoMember(8)] public int Trailer { get; set; }
}

// ... and RawNested has to be reached as a SUB-MESSAGE for any of that to be tested: at root
// RawWrite_ writes straight out and never measures, so a wrong measure would not show. Here the
// holder measures Inner to emit its length prefix, and After sits behind it - so a measure that is
// off by a byte puts After in the wrong place and the differential sees it immediately.
[ProtoContract]
public class RawHolder
{
    [ProtoMember(1)] public RawNested Inner { get; set; }
    [ProtoMember(2)] public int After { get; set; }
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

        // the measure has to agree with WriteMap on every one of these: an empty collection (which
        // contributes nothing for a non-packable element), a single element, several, and a
        // trailing member whose position depends on the map's measured length being exact
        new RawNested(),
        new RawNested { Messages = new() { [1] = [] }, Trailer = 19 },
        new RawNested { Messages = new() { [2] = [new Leaf { Id = 20 }] } },
        new RawNested { Messages = new() { [3] = [new Leaf { Id = 21 }, new Leaf(), new Leaf { Id = 22 }] } },
        new RawNested { Messages = new() { [4] = [new Leaf { Id = 23 }], [5] = [new Leaf { Id = 24 }] }, Trailer = 25 },
        new RawNested { Labels = new() { [6] = [] } },
        new RawNested { Labels = new() { [7] = ["a"] } },
        new RawNested { Labels = new() { [8] = ["", "bb", "ccc"] }, Trailer = 26 },
        new RawNested { Messages = new() { [9] = [new Leaf { Id = 27 }] }, Labels = new() { [10] = ["d"] }, Trailer = 28 },

        new RawHolder(),
        new RawHolder { Inner = new RawNested(), After = 29 },
        new RawHolder { Inner = new RawNested { Messages = new() { [11] = [] } }, After = 30 },
        new RawHolder { Inner = new RawNested { Messages = new() { [12] = [new Leaf { Id = 31 }] } }, After = 32 },
        new RawHolder { Inner = new RawNested { Messages = new() { [13] = [new Leaf { Id = 33 }, new Leaf { Id = 34 }] } }, After = 35 },
        new RawHolder { Inner = new RawNested { Labels = new() { [14] = ["e", "ff"] } }, After = 36 },
        // a payload long enough that the entry length crosses into a two-byte varint, which is
        // where an off-by-one in the per-element arithmetic stops being invisible
        new RawHolder { Inner = new RawNested { Labels = new() { [15] = ["0123456789012345678901234567890123456789012345678901234567890123456789"] } }, After = 37 },
        new RawHolder { Inner = new RawNested { Messages = new() { [16] = [new Leaf { Id = 38 }] }, Labels = new() { [17] = ["g"] }, Trailer = 39 }, After = 40 },

        // the packable arms, each reached through the holder so the measure is what sizes them:
        // empty (the zero-length packed header), one (never packed), and several (packed)
        new RawHolder { Inner = new RawNested { Counts = new() { [18] = [] } }, After = 41 },
        new RawHolder { Inner = new RawNested { Counts = new() { [19] = [42] } }, After = 43 },
        new RawHolder { Inner = new RawNested { Counts = new() { [20] = [44, 45, 46] } }, After = 47 },
        // a payload wide enough that the packed length prefix needs two varint bytes
        new RawHolder { Inner = new RawNested { Counts = new() { [21] = [.. Enumerable.Range(1, 200)] } }, After = 48 },
        // ... and values wide enough that the ELEMENTS are multi-byte varints too
        new RawHolder { Inner = new RawNested { Counts = new() { [22] = [1, 300, 70000, -1] } }, After = 49 },
        new RawHolder { Inner = new RawNested { Stamps = new() { [23] = [] } }, After = 50 },
        new RawHolder { Inner = new RawNested { Stamps = new() { [24] = [51L] } }, After = 52 },
        new RawHolder { Inner = new RawNested { Stamps = new() { [25] = [53L, -54L] } }, After = 55 },
        new RawHolder { Inner = new RawNested { Rates = new() { [26] = [1.5, 2.5] } }, After = 56 },
        new RawHolder { Inner = new RawNested { Rates = new() { [27] = [3.5] } }, After = 57 },
        new RawHolder { Inner = new RawNested { Flags = new() { [28] = [true, false, true] } }, After = 58 },
        new RawHolder { Inner = new RawNested { Flags = new() { [29] = [false] } }, After = 59 },
        new RawHolder { Inner = new RawNested { Shades = new() { [30] = [] } }, After = 60 },
        new RawHolder { Inner = new RawNested { Shades = new() { [31] = [Shade.Green] } }, After = 61 },
        new RawHolder { Inner = new RawNested { Shades = new() { [32] = [Shade.Red, Shade.None, Shade.Green] } }, After = 62 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Nested))]
[ProtoSerializable(typeof(RawNested))]
[ProtoSerializable(typeof(RawHolder))]
public partial class MapNestedModel : TypeModel
{
}
