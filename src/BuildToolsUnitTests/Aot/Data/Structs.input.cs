using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.Structs;

// value types are first-class [ProtoContract] targets: no null check and no construction on read,
// and no ThrowUnexpectedSubtype on write (it is constrained to reference types)
[ProtoContract]
public struct Point
{
    [ProtoMember(1)] public int X { get; set; }
    [ProtoMember(2)] public string Label { get; set; }
}

[ProtoContract]
public class HasStructs
{
    // a struct member can never be null, so neither side tests for it
    [ProtoMember(1)] public Point Location { get; set; }

    // ... but Nullable<TStruct> is expressible, unlike a nullable reference-type message
    [ProtoMember(2)] public Point? MaybeLocation { get; set; }

    [ProtoMember(3)] public int Other { get; set; }
}

public static class StructsSamples
{
    public static object[] Values =>
    [
        new Point(),
        new Point { X = 1, Label = "a" },
        new Point { X = -1, Label = null },
        new HasStructs(),
        new HasStructs { Location = new Point { X = 2, Label = "b" } },
        new HasStructs { MaybeLocation = new Point() },          // present but all-default
        new HasStructs { MaybeLocation = new Point { X = 3 } },
        new HasStructs { Location = new Point { X = 4 }, MaybeLocation = new Point { Label = "c" }, Other = 5 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Point))]
[ProtoSerializable(typeof(HasStructs))]
public partial class StructsModel : TypeModel
{
}
