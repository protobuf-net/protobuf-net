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

[ProtoContract]
public class Lookup
{
    [ProtoMember(1)] public Dictionary<int, string> ByNumber { get; set; }
    [ProtoMember(2)] public Dictionary<string, int> ByName { get; set; }
    [ProtoMember(3)] public Dictionary<int, int> Counts { get; set; }
    [ProtoMember(4)] public Dictionary<string, string> Labels { get; set; }
    [ProtoMember(5)] public int Trailer { get; set; }
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
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Lookup))]
public partial class MapMeasureModel : TypeModel { }
