using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.MapNestedUnsupported;

// A dictionary may nest a repeated *or map* value (MapNested.input.cs): the plan only needs that
// collection's factory, and both RepeatedSerializer and MapSerializer implement
// IRepeatedSerializer<TCollection>, which is an ISerializer<TCollection> the model can serve.
//
// A nested *key* is a different matter - there is no reference to derive from - so it stays refused.
[ProtoContract]
public class NestedKey
{
    [ProtoMember(1)] public Dictionary<List<int>, List<string>> Value { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(NestedKey))]
public partial class MapNestedUnsupportedModel : TypeModel
{
}
