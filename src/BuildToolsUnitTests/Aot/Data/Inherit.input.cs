using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.Inherit;

// every type in a [ProtoInclude] hierarchy reads and writes through the *root's* ISubTypeSerializer,
// which walks down the chain writing each layer's own members and nesting the next inside a sub-type
// marker. ISerializer<T> is then a pair of one-line delegations, for the root as much as the leaves.
[ProtoContract]
[ProtoInclude(100, typeof(Dog))]
[ProtoInclude(101, typeof(Cat))]
public abstract class Animal
{
    [ProtoMember(1)] public string Name { get; set; }
}

// a middle layer: it has both a base and sub-types of its own
[ProtoContract]
[ProtoInclude(200, typeof(Puppy))]
public class Dog : Animal
{
    [ProtoMember(1)] public int Bark { get; set; }
}

[ProtoContract]
public class Puppy : Dog
{
    [ProtoMember(1)] public int Age { get; set; }
}

// a sealed leaf: no sub-type is possible, so ThrowUnexpectedSubtype is omitted entirely
[ProtoContract]
public sealed class Cat : Animal
{
    [ProtoMember(1)] public bool Purrs { get; set; }
}

// the base-typed member is an ordinary sub-message; the dispatch is inside the root's serializer
[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public Animal Animal { get; set; }
}

// sealed, and outside any hierarchy: ref-emit omits the guard here too
[ProtoContract]
public sealed class Standalone
{
    [ProtoMember(1)] public int Value { get; set; }
}

public static class InheritSamples
{
    // NOTE: the Holder samples deliberately stay on one branch of the hierarchy. The differential
    // suite manufactures repeated fields by concatenating every sample of a type, and merging a Cat
    // onto an existing Dog sends protobuf-net itself into unbounded recursion (SubTypeState.Cast ->
    // Merge -> Model.Serialize<object> -> ...), which kills the test process. That reproduces with
    // RuntimeTypeModel alone, with no generated code involved.
    public static object[] Values =>
    [
        new Dog { Name = "rex", Bark = 1 },
        new Cat { Name = "tom", Purrs = true },
        new Puppy { Name = "spot", Bark = 2, Age = 3 },
        new Holder(),
        new Holder { Animal = new Dog { Name = "fido", Bark = 4 } },
        new Holder { Animal = new Puppy { Name = "rover", Bark = 5, Age = 6 } },
        new Standalone(),
        new Standalone { Value = 7 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Animal))]
[ProtoSerializable(typeof(Holder))]
[ProtoSerializable(typeof(Standalone))]
public partial class InheritModel : TypeModel
{
}
