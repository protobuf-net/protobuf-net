// A map whose key and value are both plain scalars, so the contract stays MEASURABLE (gap B6).
//
// Map.input.cs cannot cover this: it carries a message-valued map, which is not measurable, and one
// unmeasurable member takes the whole contract - so nothing in that fixture ever emits a map
// measure. This one exists to pin the arithmetic itself.
//
// The samples are chosen to break a naive measure rather than to look tidy. A map entry omits a
// trivial key or value INDEPENDENTLY (KeyValuePairSerializer.Write tests HasNonTrivialValue on each
// side), so:
//   - a zero key with a real value, and a real key with a zero value, each produce a HALF entry;
//   - both trivial produces an EMPTY entry, which is still written as tag + 0x00 - it is the pair's
//     contents that are conditional, not the pair;
//   - an EMPTY STRING is non-trivial ("we write "" for compat", PrimaryTypeProvider), so it must be
//     measured as a present, zero-length field rather than skipped like a null.
using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.MapMeasure;

public enum Hue { None = 0, Warm = 1 }

[ProtoContract]
public class Lookup
{
    [ProtoMember(1)] public Dictionary<int, string> ByNumber { get; set; }
    [ProtoMember(2)] public Dictionary<string, int> ByName { get; set; }
    [ProtoMember(3)] public Dictionary<int, int> Counts { get; set; }
    [ProtoMember(4)] public Dictionary<string, string> Labels { get; set; }
    // a NULLABLE value, which the map-side guard and body have to spell differently: `!= 0` lifts
    // over int? and is false for null, but the payload cast needs GetValueOrDefault()
    [ProtoMember(6)] public Dictionary<int, int?> Maybe { get; set; }
    // an ENUM side is written even when zero, unlike a plain scalar - probed
    [ProtoMember(7)] public Dictionary<int, Hue> Shades { get; set; }
    [ProtoMember(8)] public Dictionary<Hue, int> ByShade { get; set; }
    // a MESSAGE value (gap B6). Multiple entries matter: the corpus symptom was a length that
    // looked like it covered ONE entry rather than all of them
    [ProtoMember(9)] public Dictionary<int, Note> Notes { get; set; }
    // a slot-consuming MESSAGE member AFTER the map in field order. The map's write hands back to
    // the stateful MapSerializer, whose value write re-enters the slot machinery (Mark/SeekTo) -
    // so anything reading a slot after it is where cursor corruption would show. Every corpus
    // contract that disagrees has this shape; Lookup did not, which is why it stayed green.
    [ProtoMember(10)] public Note Tail { get; set; }
    [ProtoMember(5)] public int Trailer { get; set; }
}

[ProtoContract]
public class Note
{
    [ProtoMember(1)] public string Text { get; set; }
    [ProtoMember(2)] public int Rank { get; set; }
    // a repeated MESSAGE, so this type's own measure RESERVES SLOTS. That is the difference
    // between the corpus contracts that disagree and the ones that do not - see notes/gaps.md B6.
    [ProtoMember(3)] public List<Tag> Tags { get; set; }
}

[ProtoContract]
public class Tag
{
    [ProtoMember(1)] public string Name { get; set; }
}

public static class MapMeasureSamples
{
    public static object[] Values =>
    [
        new Lookup(),
        new Lookup { ByNumber = new() { [1] = "one", [2] = "two" } },
        // a zero KEY: the entry carries only the value
        new Lookup { ByNumber = new() { [0] = "zero" } },
        // NOTE: no null map VALUE here. protobuf-net does not round-trip one - the write omits it
        // and the read hands back something that re-serializes as an empty string - which is a
        // property of the RUNTIME model, not of the generated one. See notes/gaps.md B43; the
        // fixture stays green rather than pinning a shape neither engine supports.
        // an EMPTY STRING value is written, unlike a null one
        new Lookup { ByNumber = new() { [8] = "" } },
        // both sides trivial: an empty entry, still emitted
        new Lookup { Counts = new() { [0] = 0 } },
        new Lookup { ByName = new() { ["a"] = 1, [""] = 2 } },
        new Lookup { Counts = new() { [1] = 100, [2] = 0, [0] = 300 } },
        new Lookup { Labels = new() { ["k"] = "v", ["empty"] = "" } },
        // a map beside an ordinary member, so the whole contract's length is exercised
        new Lookup { Counts = new() { [5] = 6 }, Trailer = 42 },
        new Lookup { Maybe = new() { [1] = 2, [2] = null, [3] = 0 } },
        // a zero enum on either side is still written
        new Lookup { Shades = new() { [1] = Hue.Warm, [2] = Hue.None }, ByShade = new() { [Hue.None] = 5, [Hue.Warm] = 0 } },
        // present-but-empty vs null vs populated
        new Lookup { Notes = new() { [1] = new Note { Text = "a", Rank = 1 } } },
        new Lookup { Notes = new() { [1] = new Note { Text = "a", Rank = 1 }, [2] = new Note { Text = "bb", Rank = 2 } } },
        new Lookup { Notes = new() { [1] = new Note(), [2] = new Note { Text = "x" }, [3] = new Note { Rank = 9 } } },
        new Lookup { Notes = new() { [0] = new Note { Text = "zerokey" } }, Trailer = 7 },
        // the value type reserves slots of its own, which the plain Note above does not
        new Lookup { Notes = new() { [1] = new Note { Text = "a", Tags = [new Tag { Name = "t1" }] } } },
        new Lookup { Notes = new() { [1] = new Note { Text = "a", Tags = [new Tag { Name = "t1" }, new Tag { Name = "t2" }] }, [2] = new Note { Rank = 3 } }, Trailer = 9 },
        // a map, then a message member that READS A SLOT after it
        new Lookup { Notes = new() { [1] = new Note { Text = "a", Tags = [new Tag { Name = "t1" }] } }, Tail = new Note { Text = "tail", Tags = [new Tag { Name = "z" }] } },
        new Lookup { Notes = new() { [1] = new Note { Text = "aaaaaaaaaa", Tags = [new Tag { Name = "t1" }, new Tag { Name = "t2" }] }, [2] = new Note { Text = "bb" } }, Tail = new Note { Text = "tail", Rank = 4 }, Trailer = 3 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Lookup))]
public partial class MapMeasureModel : TypeModel { }
