using ProtoBuf;
using ProtoBuf.Meta;

// [ProtoSubType] is the compile-time equivalent of MetaType.AddSubType: it links a sub-type onto a
// base that has never heard of it, which is the one thing [ProtoInclude] cannot express - the base
// lives in a package that does not reference yours, or the sub-type is a generic construction the
// base library cannot name. The outcome is exactly as though the base had carried [ProtoInclude].
//
// Declared on the *model* here so that the fixture stays self-contained; assembly and module are the
// other two sites, and are what let a library ship the linkage (Diagnostics/AssemblySubType covers
// that spelling, and ProtoSubTypeReferenceTests covers it across real assembly boundaries - it has
// to be there rather than here, since every fixture is linked into one assembly).
//
// AotRefGen replays these declarations onto the reference model through the public
// MetaType.AddSubType, so this is differentially covered like any other hierarchy.
namespace AotFixtures.OutOfBandSubType;

// note there is no PBN0013 here, and that is the analyzer working: it treats a [ProtoSubType]
// declaration at any of the three sites - including this model - as declaring the linkage.

// no [ProtoInclude] here at all: this type is standing in for one in a base library that has never
// heard of anything below it
[ProtoContract]
public class Shape
{
    [ProtoMember(1)] public string Label { get; set; }
}

[ProtoContract]
public class Circle : Shape
{
    [ProtoMember(1)] public int Radius { get; set; }
}

// the shape from the ticket: a closed generic the base library could not have named
[ProtoContract]
public class Tagged<T> : Shape
{
    [ProtoMember(1)] public T Value { get; set; }
}

// group framing, which is the only thing there is to choose for a sub-message - hence the bool
// overload rather than a DataFormat, most of whose values would have nothing to select
[ProtoContract]
public class Square : Shape
{
    [ProtoMember(1)] public int Side { get; set; }
}

// the two surfaces mixed on one base: the include the base *can* declare, plus one it cannot. The
// merged set has to behave as though both had been written as [ProtoInclude]
[ProtoContract]
[ProtoInclude(10, typeof(Sedan))]
public class Vehicle
{
    [ProtoMember(1)] public int Wheels { get; set; }
}

[ProtoContract]
public class Sedan : Vehicle
{
    [ProtoMember(1)] public int Doors { get; set; }
}

[ProtoContract]
public class Tractor : Vehicle
{
    [ProtoMember(1)] public bool HasPlough { get; set; }
}

// neither end of either hierarchy is seeded: the base is reached through this member, and the
// sub-types are then reached as sub-types of it, exactly as declared [ProtoInclude]s would be
[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public Shape Shape { get; set; }
    [ProtoMember(2)] public Vehicle Vehicle { get; set; }
}

public static class OutOfBandSubTypeSamples
{
    // one branch per sample, deliberately: the differential suite manufactures repeated fields by
    // concatenating every sample of a type, and merging incompatible siblings sends protobuf-net
    // itself into unbounded recursion - see the note in Inherit.input.cs
    public static object[] Values =>
    [
        new Holder { Shape = new Circle { Label = "c", Radius = 3 } },
        new Holder { Vehicle = new Tractor { Wheels = 4, HasPlough = true } },
        new Circle { Label = "lone", Radius = 7 },
        new Tagged<int> { Label = "tagged", Value = 42 },
        new Square { Label = "sq", Side = 5 },
        new Sedan { Wheels = 4, Doors = 5 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Holder))]
[ProtoSubType(typeof(Shape), typeof(Circle), 100)]
[ProtoSubType(typeof(Shape), typeof(Tagged<int>), 101)]
[ProtoSubType(typeof(Shape), typeof(Square), 102, true)]
[ProtoSubType(typeof(Vehicle), typeof(Tractor), 11)]
public partial class OutOfBandSubTypeModel : TypeModel
{
}
