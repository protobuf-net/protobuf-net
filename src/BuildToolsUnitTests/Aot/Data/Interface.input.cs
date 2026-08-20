using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.Interface;

// An interface contract is an inheritance root exactly as an abstract base class is: all the traffic
// goes through the root's ISubTypeSerializer, which writes each layer's own members and nests the
// next inside a sub-type marker. Probed against ref-emit rather than assumed.
//
// Note the trap this shape carries, which is why PBN0023 exists: the interface's *own* declared
// members are written in addition to the implementation's, so a property declared on both goes on
// the wire twice. IAnimal deliberately declares none, so the fixture does not bake that in; Named
// below is the shape that does.
[ProtoContract]
[ProtoInclude(10, typeof(Dog))]
[ProtoInclude(11, typeof(Cat))]
public interface IAnimal
{
}

[ProtoContract]
public class Dog : IAnimal
{
    [ProtoMember(1)] public string Name { get; set; }
    [ProtoMember(2)] public int Fetches { get; set; }
}

[ProtoContract]
public class Cat : IAnimal
{
    [ProtoMember(1)] public string Name { get; set; }
    [ProtoMember(3)] public bool Aloof { get; set; }
}

// an interface that *does* declare a member: the root layer writes it as well as the implementation
[ProtoContract]
[ProtoInclude(10, typeof(Tagged))]
public interface INamed
{
    [ProtoMember(1)] string Label { get; set; }
}

[ProtoContract]
public class Tagged : INamed
{
    [ProtoMember(1)] public string Label { get; set; }
    [ProtoMember(2)] public int Order { get; set; }
}

[ProtoContract]
public class Zoo
{
    [ProtoMember(1)] public IAnimal Star { get; set; }
    [ProtoMember(2)] public List<IAnimal> All { get; set; }
    [ProtoMember(3)] public INamed Tag { get; set; }

    // Cat gets its own field rather than sharing Star: the differential suite manufactures repeated
    // fields by concatenating every sample, and merging two *different* sub-types into one field
    // overflows the stack - see notes/aot/findings.md, and Inherit.input.cs for the same workaround
    [ProtoMember(4)] public IAnimal Backup { get; set; }
}

public static class InterfaceSamples
{
    public static object[] Values =>
    [
        new Zoo(),
        new Zoo { Star = new Dog { Name = "rex", Fetches = 3 } },
        new Zoo { Backup = new Cat { Name = "tom", Aloof = true } },
        new Zoo { All = [new Dog { Name = "a" }, new Cat { Name = "b", Aloof = true }] },
        new Zoo { Tag = new Tagged { Label = "x", Order = 2 } },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Zoo))]
public partial class InterfaceModel : TypeModel
{
}
