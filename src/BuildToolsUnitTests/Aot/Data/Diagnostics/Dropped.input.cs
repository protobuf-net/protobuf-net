using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

// every contract here is expected to be dropped, each for a different reason; the point of the
// fixture is the .txt golden, which is the user-visible explanation of why
namespace AotFixtures.Dropped;

[ProtoContract]
public class HasUnsupportedMember
{
    [ProtoMember(1)]
    public int Id { get; set; }

    // collections are not handled yet -> PBN3001
    [ProtoMember(2)]
    public List<string> Tags { get; set; }
}

[ProtoContract]
public class ReferencesDropped
{
    // fine in itself, but its member type is dropped -> PBN3004
    [ProtoMember(1)]
    public HasUnsupportedMember Child { get; set; }
}

[ProtoContract]
public class NoParameterlessConstructor
{
    public NoParameterlessConstructor(int id) => Id = id;

    // -> PBN3002
    [ProtoMember(1)]
    public int Id { get; set; }
}

[ProtoContract]
public class UsesMemberOptions
{
    // named arguments change the wire format -> PBN3003
    [ProtoMember(1, IsRequired = true)]
    public int Value { get; set; }
}

[ProtoContract]
public class UnrenderableDefault
{
    // the (Type, string) form defers to a TypeConverter at runtime, which cannot be evaluated here
    [ProtoMember(1), DefaultValue(typeof(int), "5")]
    public int Value { get; set; }
}

[ProtoContract]
public class HasCallback
{
    [ProtoMember(1)]
    public int Value { get; set; }

    [OnDeserialized]
    public void AfterRead(StreamingContext context) { }
}

// a *closed* generic is an ordinary contract; only an open one is refused, since the services type
// is a single non-generic class with nowhere to put the type parameter -> PBN3002
[ProtoContract]
public class OpenGeneric<T>
{
    [ProtoMember(1)] public T Value { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(OpenGeneric<>))]
[ProtoSerializable(typeof(UnrenderableDefault))]
[ProtoSerializable(typeof(HasCallback))]
[ProtoSerializable(typeof(ReferencesDropped))]
[ProtoSerializable(typeof(NoParameterlessConstructor))]
[ProtoSerializable(typeof(UsesMemberOptions))]
public partial class DroppedModel : TypeModel
{
}
