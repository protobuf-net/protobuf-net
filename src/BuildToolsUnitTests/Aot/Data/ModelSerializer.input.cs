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

// WCF-style contract: [DataContract]/[DataMember(Order)] supply the family and the field numbers
[DataContract]
public class Request
{
    [DataMember(Order = 1)] public Wrapped<byte> Special { get; set; }
    [DataMember(Order = 2)] public int Plain { get; set; }
}

public static class ModelSerializerSamples
{
    public static object[] Values =>
    [
        new Request(),
        new Request { Special = new Wrapped<byte>(4) },
        new Request { Special = new Wrapped<byte>(200), Plain = 7 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Request))]
[ProtoSerializer(typeof(Wrapped<byte>), typeof(WrappedByteSerializer), IsScalar = true)]
public partial class ModelSerializerModel : TypeModel
{
}
