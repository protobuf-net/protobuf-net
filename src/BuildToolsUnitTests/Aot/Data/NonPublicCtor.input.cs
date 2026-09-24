using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.NonPublicCtor;

// A non-public parameterless constructor is reached through [UnsafeAccessor], which matches what
// RuntimeTypeModel does (it calls it by reflection). Ref-emit's *compiled* path refuses these -
// "Non-public member cannot be used with full dll compilation" - exactly as it refuses a non-public
// setter, so there is no .reference.cs for this fixture.
//
// Having *no* parameterless constructor at all is a different case and stays refused: ref-emit
// throws "No parameterless constructor found" on both its paths, so there is nothing to match.
[ProtoContract]
public class PrivateCtor
{
    private PrivateCtor() { }
    public PrivateCtor(int value) => Value = value;

    [ProtoMember(1)] public int Value { get; set; }
    [ProtoMember(2)] public string Name { get; set; }
}

[ProtoContract]
public class InternalCtor
{
    internal InternalCtor() { }
    public InternalCtor(int value) => Value = value;

    [ProtoMember(1)] public int Value { get; set; }
}

[ProtoContract]
public class ProtectedCtor
{
    protected ProtectedCtor() { }
    public ProtectedCtor(int value) => Value = value;

    [ProtoMember(1)] public int Value { get; set; }
}

// the constructed instance is a member of another contract, so the accessor is exercised through
// the sub-message merge path as well as at the root
[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public PrivateCtor Child { get; set; }
}

public static class NonPublicCtorSamples
{
    public static object[] Values =>
    [
        new PrivateCtor(0),
        new PrivateCtor(7) { Name = "seven" },
        new InternalCtor(3),
        new ProtectedCtor(4),
        new Holder(),
        new Holder { Child = new PrivateCtor(9) { Name = "nine" } },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(PrivateCtor))]
[ProtoSerializable(typeof(InternalCtor))]
[ProtoSerializable(typeof(ProtectedCtor))]
[ProtoSerializable(typeof(Holder))]
public partial class NonPublicCtorModel : TypeModel
{
}
