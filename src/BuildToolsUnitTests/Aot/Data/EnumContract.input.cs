using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.EnumContract;

// An enum can carry [ProtoContract] and be a model root in its own right - [ProtoContract]'s own
// AttributeUsage allows class, struct, enum and interface, and RuntimeTypeModel serializes an enum
// root happily. Reached as a *member* an enum has always been an inline scalar; this is about the
// enum being seeded, which was refused by "only classes, structs and interfaces".
[ProtoContract]
public enum Shade
{
    None = 0,
    Light = 1,
    Dark = 2,
}

// a non-int backing type, since the underlying scalar is what goes on the wire
[ProtoContract]
public enum Size : byte
{
    Small = 0,
    Large = 200,
}

// [Flags] makes no difference to the wire form
[ProtoContract, System.Flags]
public enum Options
{
    None = 0,
    A = 1,
    B = 2,
}

// ...and one that is only reachable as a member, to prove the two paths still agree
[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public Shade Shade { get; set; }
    [ProtoMember(2)] public Size Size { get; set; }
    [ProtoMember(3)] public Options Options { get; set; }
}

public static class EnumContractSamples
{
    public static object[] Values =>
    [
        new Holder(),
        new Holder { Shade = Shade.Dark, Size = Size.Large, Options = Options.A | Options.B },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Shade))]
[ProtoSerializable(typeof(Size))]
[ProtoSerializable(typeof(Options))]
[ProtoSerializable(typeof(Holder))]
public partial class EnumContractModel : TypeModel
{
}
