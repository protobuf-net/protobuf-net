using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System.Runtime.Serialization;

// Differentially covered by AotConformanceTests, which replays the [ProtoSerializer]
// declarations onto the reference model. The .reference.cs shows ref-emit closing the
// OPEN-GENERIC mapping per use site - ISerializerProxy<Wrapped<byte|int|long|string>> -
// which is the part of this feature with no prior art to copy.
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

    // Nullable<T> of a declaration-served struct: probed against ref-emit (RuntimeTypeModel), which
    // supports it - a nullable message member whose type is served by a hand-written serializer.
    // Presence (HasValue) decides whether anything is written at all, exactly as for an ordinary
    // nullable struct message; the wire form when present is byte-identical to the non-nullable case,
    // since the underlying serializer is CategoryScalar (see AotFixtures.ExternalSerializer.Stamp for
    // the same fact pinned against the [ProtoContract(Serializer = ...)] form).
    [DataMember(Order = 5)] public Wrapped<long>? Optional { get; set; }
}

public static class ModelSerializerSamples
{
    public static object[] Values =>
    [
        new Request(),
        new Request { Special = new Wrapped<byte>(4) },
        new Request { Id = new Wrapped<int>(11), Label = new Wrapped<string>(12) },
        new Request { Special = new Wrapped<byte>(200), Plain = 7, Id = new Wrapped<int>(-13) },
        new Request { Optional = new Wrapped<long>(21) },
        new Request { Special = new Wrapped<byte>(9), Optional = null },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Request))]
[ProtoSerializer(typeof(Wrapped<>), typeof(WrappedSerializer<>), IsScalar = true)]
[ProtoSerializer(typeof(Wrapped<byte>), typeof(WrappedByteSerializer), IsScalar = true)]
public partial class ModelSerializerModel : TypeModel
{
}
