using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.MapUnsupported;

// an enum on either side of a map, and a repeated value, both resolve their serializer *from the
// model* - ref-emit emits `this as ISerializer<T>` - and the generated services type does not expose
// one. Note protobuf-net allows a nested collection on a dictionary specifically, unlike a list.
public enum Shade { None, Light, Dark }

[ProtoContract]
public class EnumValue
{
    [ProtoMember(1)] public Dictionary<int, Shade> Value { get; set; }
}

[ProtoContract]
public class EnumKey
{
    [ProtoMember(1)] public Dictionary<Shade, int> Value { get; set; }
}

[ProtoContract]
public class RepeatedValue
{
    [ProtoMember(1)] public Dictionary<int, List<int>> Value { get; set; }
}

// [ProtoMap] itself is supported (see MapFormat.input.cs); this one is here to prove the enum
// refusal still applies through it, since the attribute does not change how the key resolves
[ProtoContract]
public class MappedEnum
{
    [ProtoMember(1), ProtoMap(KeyFormat = DataFormat.ZigZag)]
    public Dictionary<Shade, int> Value { get; set; }
}

// ...and on a member that is not a dictionary at all, where protobuf-net silently ignores it
[ProtoContract]
public class NotADictionary
{
    [ProtoMember(1), ProtoMap(KeyFormat = DataFormat.ZigZag)]
    public List<int> Value { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(MappedEnum))]
[ProtoSerializable(typeof(NotADictionary))]
[ProtoSerializable(typeof(EnumValue))]
[ProtoSerializable(typeof(EnumKey))]
[ProtoSerializable(typeof(RepeatedValue))]
public partial class MapUnsupportedModel : TypeModel
{
}
