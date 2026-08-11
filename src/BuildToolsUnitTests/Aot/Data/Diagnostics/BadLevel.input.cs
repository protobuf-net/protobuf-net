using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.BadLevel;

// CompatibilityLevelAttribute.AssertValid admits only NotSpecified, 200, 240 and 300; anything else
// throws while building the model. The attribute takes the enum, so reaching this needs a cast -
// but casting to an enum is legal C# and the corpus does exactly that, so it is checked rather than
// assumed. Both the type-level and member-level declarations are validated.

[ProtoContract]
[CompatibilityLevel((CompatibilityLevel)42)]
public class BadOnType
{
    [ProtoMember(1)] public int Value { get; set; }
}

[ProtoContract]
public class BadOnMember
{
    [ProtoMember(1)]
    [CompatibilityLevel((CompatibilityLevel)42)]
    public int Value { get; set; }
}

// the three legal levels, which must still emit
[ProtoContract]
[CompatibilityLevel(CompatibilityLevel.Level240)]
public class GoodLevel
{
    [ProtoMember(1)]
    [CompatibilityLevel(CompatibilityLevel.Level300)]
    public int Value { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(BadOnType))]
[ProtoSerializable(typeof(BadOnMember))]
[ProtoSerializable(typeof(GoodLevel))]
public partial class BadLevelModel : TypeModel
{
}
