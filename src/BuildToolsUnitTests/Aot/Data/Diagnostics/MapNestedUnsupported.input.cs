using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.MapNestedUnsupported;

// A dictionary may nest a *repeated* value (MapNested.input.cs), because the plan only needs that
// collection's factory. A nested *map* value would need a ProtoMapPlan inside a ProtoMapPlan, which
// a struct cannot hold - so this one is still refused, and unlike most of the remaining refusals it
// is genuinely ours rather than protobuf-net's: ref-emit handles it.
[ProtoContract]
public class NestedMap
{
    [ProtoMember(1)] public Dictionary<string, Dictionary<string, string>> Value { get; set; }
}

// a repeated *key* is refused for the same reason it always was: no reference to derive from
[ProtoContract]
public class NestedKey
{
    [ProtoMember(1)] public Dictionary<List<int>, List<string>> Value { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(NestedMap))]
[ProtoSerializable(typeof(NestedKey))]
public partial class MapNestedUnsupportedModel : TypeModel
{
}
