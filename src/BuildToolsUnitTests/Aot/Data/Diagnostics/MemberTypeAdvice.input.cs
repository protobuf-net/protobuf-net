using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Net;

namespace AotFixtures.MemberTypeAdvice;

// "has unsupported type X" on its own reads as our backlog even where the route is right there, so
// two cases say which one it is. Both are determined, not guessed: the first by re-asking
// GetMemberShape with the option on, the second by the type's own name.
[ProtoContract]
public class NeedsOption
{
    // IPAddress has ToString() and a static Parse(string), so it qualifies as parseable - but
    // AllowParseableTypes is off by default here, matching RuntimeTypeModel
    [ProtoMember(1)] public IPAddress Address { get; set; }
}

[ProtoContract]
public class UsesSystemType
{
    // ref-emit does serialize this, via assembly-qualified names through Type.GetType - which is
    // exactly the reflection native AOT cannot do, so it is refused on purpose rather than pending
    [ProtoMember(1)] public Type Which { get; set; }
}

[ProtoContract]
public class UsesSystemTypeArray
{
    [ProtoMember(1)] public Type[] Several { get; set; }
}

public interface IUndecorated { int N { get; } }

// a collection is never itself the problem, so the question moves one level down to the element -
// which is how List<ISomething> gets an answer rather than a bare "unsupported type". protobuf-net
// throws "No serializer for type IUndecorated is available" here, on both ref-emit paths
[ProtoContract]
public class ListOfUndecorated
{
    [ProtoMember(1)] public System.Collections.Generic.List<IUndecorated> Items { get; set; }
}

// ...but a map is left alone: its key and value are separate, so there is no single element to
// name, and an enum on either side is a gap of ours rather than something protobuf-net refuses
[ProtoContract]
public class MapWithEnumKey
{
    [ProtoMember(1)] public System.Collections.Generic.Dictionary<Shade, int> ByShade { get; set; }
}

public enum Shade { None, Red }

[ProtoModel]
[ProtoSerializable(typeof(NeedsOption))]
[ProtoSerializable(typeof(UsesSystemType))]
[ProtoSerializable(typeof(UsesSystemTypeArray))]
[ProtoSerializable(typeof(ListOfUndecorated))]
[ProtoSerializable(typeof(MapWithEnumKey))]
public partial class MemberTypeAdviceModel : TypeModel
{
}
