using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System.Runtime.Serialization;

// NOTE: no .reference.cs yet - added on Linux, and AotRefGen is net472 so it could not be run.
// Nothing here is refused by ref-emit once the harness replays the declarations, so this fixture
// *should* have one. Differentially covered in the meantime by AotConformanceTests, which replays
// [ProtoSerializer] onto the reference model through MetaType.SerializerType. Run AotRefGen on
// Windows and commit the result.
//
// [ProtoSerializer] on the model is the compile-time equivalent of MetaType.SerializerType: a
// hand-written serializer for a type that cannot carry [ProtoContract(Serializer = ...)] itself -
// because you do not own it, or because the serializer lives in an assembly the type cannot
// reference back (a domain type whose serializer ships in an infrastructure assembly).
namespace AotFixtures.ModelSerializer;

// a scalar union shape: the wire form is the payload's own, with no message wrapper. The type
// carries no protobuf-net attribute at all - the declaration stands in for the contract.
public readonly struct Wrapped<T>
{
    public Wrapped(long tag) => Tag = tag;
    public long Tag { get; }
}

// the closed pairing: Wrapped<byte> is framed fixed32 where the generic form (Task 4) is varint,
// so the two are distinguishable on the wire
public sealed class WrappedByteSerializer : ISerializer<Wrapped<byte>>
{
    SerializerFeatures ISerializer<Wrapped<byte>>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeFixed32;

    Wrapped<byte> ISerializer<Wrapped<byte>>.Read(ref ProtoReader.State state, Wrapped<byte> value)
        => new Wrapped<byte>(state.ReadInt32());

    void ISerializer<Wrapped<byte>>.Write(ref ProtoWriter.State state, Wrapped<byte> value)
        => state.WriteInt32((int)value.Tag);
}

// the open mapping: one declaration serves every instantiation the model meets. Varint framing,
// distinguishable from the closed override's fixed32
public sealed class WrappedSerializer<T> : ISerializer<Wrapped<T>>
{
    SerializerFeatures ISerializer<Wrapped<T>>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;

    Wrapped<T> ISerializer<Wrapped<T>>.Read(ref ProtoReader.State state, Wrapped<T> value)
        => new Wrapped<T>(state.ReadInt64());

    void ISerializer<Wrapped<T>>.Write(ref ProtoWriter.State state, Wrapped<T> value)
        => state.WriteInt64(value.Tag);
}

// WCF-style contract: [DataContract]/[DataMember(Order)] supply the family and the field numbers
[DataContract]
public class Request
{
    [DataMember(Order = 1)] public Wrapped<byte> Special { get; set; }
    [DataMember(Order = 2)] public int Plain { get; set; }
    [DataMember(Order = 3)] public Wrapped<int> Id { get; set; }
    [DataMember(Order = 4)] public Wrapped<string> Label { get; set; }
}

public static class ModelSerializerSamples
{
    public static object[] Values =>
    [
        new Request(),
        new Request { Special = new Wrapped<byte>(4) },
        new Request { Id = new Wrapped<int>(11), Label = new Wrapped<string>(12) },
        new Request { Special = new Wrapped<byte>(200), Plain = 7, Id = new Wrapped<int>(-13) },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Request))]
[ProtoSerializer(typeof(Wrapped<>), typeof(WrappedSerializer<>), IsScalar = true)]
[ProtoSerializer(typeof(Wrapped<byte>), typeof(WrappedByteSerializer), IsScalar = true)]
public partial class ModelSerializerModel : TypeModel
{
}
