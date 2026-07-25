using ProtoBuf;
using ProtoBuf.Meta;
using System;

namespace AotFixtures.Extensible;

// an extensible contract keeps the fields it does not recognise instead of discarding them: the read
// stores them rather than skipping, and the write appends them after every declared member.
//
// Which overload is used is *not* simply "whichever interface is implemented" - it is
// ITypedExtensible && (in a hierarchy || IExtensible is not also implemented). ProtoBuf.Extensible
// supplies both, so a standalone one gets the untyped overload and a hierarchy member the typed one.
[ProtoContract]
public class FromBase : ProtoBuf.Extensible
{
    [ProtoMember(1)] public int Value { get; set; }
}

// IExtensible alone, holding the bag itself
[ProtoContract]
public class ByHand : IExtensible
{
    private IExtension _extension;
    IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        => ProtoBuf.Extensible.GetExtensionObject(ref _extension, createIfMissing);

    [ProtoMember(1)] public int Value { get; set; }
}

// only the typed interface, and no hierarchy: still the typed overload
[ProtoContract]
public class TypedOnly : ITypedExtensible
{
    private IExtension _extension;
    IExtension ITypedExtensible.GetExtensionObject(Type type, bool createIfMissing)
        => ProtoBuf.Extensible.GetExtensionObject(ref _extension, createIfMissing);

    [ProtoMember(1)] public int Value { get; set; }
}

// a hierarchy: each layer keys its own bag on its own type, so the same field number can appear at
// more than one level without colliding
[ProtoContract]
[ProtoInclude(100, typeof(DerivedExt))]
public class BaseExt : ProtoBuf.Extensible
{
    [ProtoMember(1)] public int Shared { get; set; }
}

[ProtoContract]
public class DerivedExt : BaseExt
{
    [ProtoMember(1)] public int Extra { get; set; }
}

public static class ExtensibleSamples
{
    // an unrecognised field has to be put there deliberately: nothing in a round-trip of a
    // self-consistent contract would ever produce one
    private static T Extra<T>(T value, int tag, int extra) where T : IExtensible
    {
        ProtoBuf.Extensible.AppendValue(value, tag, extra);
        return value;
    }

    public static object[] Values =>
    [
        new FromBase { Value = 1 },
        Extra(new FromBase { Value = 2 }, 5, 42),
        new ByHand { Value = 3 },
        Extra(new ByHand { Value = 4 }, 6, 43),
        new TypedOnly { Value = 5 },
        new BaseExt { Shared = 6 },
        new DerivedExt { Shared = 7, Extra = 8 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(FromBase))]
[ProtoSerializable(typeof(ByHand))]
[ProtoSerializable(typeof(TypedOnly))]
[ProtoSerializable(typeof(BaseExt))]
public partial class ExtensibleModel : TypeModel
{
}
