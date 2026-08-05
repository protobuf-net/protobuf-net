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

[ProtoModel]
[ProtoSerializable(typeof(NeedsOption))]
[ProtoSerializable(typeof(UsesSystemType))]
[ProtoSerializable(typeof(UsesSystemTypeArray))]
public partial class MemberTypeAdviceModel : TypeModel
{
}
