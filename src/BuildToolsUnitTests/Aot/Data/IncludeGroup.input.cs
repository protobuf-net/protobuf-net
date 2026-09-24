using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.IncludeGroup;

// [ProtoInclude] takes a DataFormat, which frames the sub-type as a group rather than
// length-prefixed. It is the one named argument on that attribute that reaches the wire, and it was
// refused along with the rest - which left the derived type unlinked, and an unlinked contract is
// emitted standalone, so the base's members disappeared entirely.
//
// Found by the corpus differential on Examples.Issues.SO18277323, where we wrote nothing at all.

[ProtoContract]
[ProtoInclude(3, typeof(Grouped), DataFormat = DataFormat.Group)]
[ProtoInclude(4, typeof(Plain))]
public class Base
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public string Error { get; set; }
}

[ProtoContract]
public class Grouped : Base
{
    [ProtoMember(1)] public int Extra { get; set; }
}

// the contrast: the same hierarchy, default framing
[ProtoContract]
public class Plain : Base
{
    [ProtoMember(1)] public int Extra { get; set; }
}

public static class IncludeGroupSamples
{
    public static object[] Values =>
    [
        new Base(),
        new Base { Success = true, Error = "e" },
        new Grouped { Success = true, Error = "g", Extra = 1 },
        new Plain { Success = false, Error = "p", Extra = 2 },
        new Grouped(),
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Base))]
public partial class IncludeGroupModel : TypeModel
{
}
