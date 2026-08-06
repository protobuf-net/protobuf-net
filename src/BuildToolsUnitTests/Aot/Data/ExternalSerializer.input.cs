using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

namespace AotFixtures.ExternalSerializer;

public sealed class ThingSerializer : ISerializer<Thing>
{
    SerializerFeatures ISerializer<Thing>.Features
        => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;

    Thing ISerializer<Thing>.Read(ref ProtoReader.State state, Thing value)
    {
        value ??= new Thing();
        int field;
        while ((field = state.ReadFieldHeader()) > 0)
        {
            if (field == 1) value.Value = state.ReadInt32();
            else state.SkipField();
        }
        return value;
    }

    void ISerializer<Thing>.Write(ref ProtoWriter.State state, Thing value)
        => state.WriteInt32Varint(1, value.Value);
}

[ProtoContract(Serializer = typeof(ThingSerializer))]
public class Thing
{
    public int Value { get; set; }
}

// ...and one whose serializer declares CategoryScalar, which is a different emitted shape: the
// member is framed by the serializer's own wire type rather than as a sub-message. The category is
// not knowable by inspection - Features is a property a generator would have to *execute* - so it is
// read from the declaration when that is in source, and from [ProtoContract(IsScalar)] when it is
// not. Assuming "message" wrote a length prefix over a bare varint, which threw at runtime.
public sealed class TicketSerializer : ISerializer<Ticket>
{
    SerializerFeatures ISerializer<Ticket>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;

    Ticket ISerializer<Ticket>.Read(ref ProtoReader.State state, Ticket value)
        => new Ticket(state.ReadInt64());

    void ISerializer<Ticket>.Write(ref ProtoWriter.State state, Ticket value)
        => state.WriteInt64(value.Value);
}

[ProtoContract(Serializer = typeof(TicketSerializer))]
public readonly struct Ticket
{
    public Ticket(long value) => Value = value;
    public long Value { get; }
}

// the same again, but stating the category outright - which is the only route that survives into
// metadata, and so the only one available when the serializer lives in a compiled reference
public sealed class StampSerializer : ISerializer<Stamp>
{
    SerializerFeatures ISerializer<Stamp>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeFixed32;

    Stamp ISerializer<Stamp>.Read(ref ProtoReader.State state, Stamp value)
        => new Stamp(state.ReadInt32());

    void ISerializer<Stamp>.Write(ref ProtoWriter.State state, Stamp value)
        => state.WriteInt32(value.Value);
}

[ProtoContract(Serializer = typeof(StampSerializer), IsScalar = true)]
public readonly struct Stamp
{
    public Stamp(int value) => Value = value;
    public int Value { get; }
}

[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public Thing Thing { get; set; }
    [ProtoMember(2)] public Ticket Ticket { get; set; }
    [ProtoMember(3)] public Stamp Stamp { get; set; }
}

public static class ExternalSerializerSamples
{
    public static object[] Values =>
    [
        new Holder(),
        new Holder { Thing = new Thing { Value = 1 } },
        new Holder { Ticket = new Ticket(2), Stamp = new Stamp(3) },
        new Holder { Thing = new Thing { Value = 4 }, Ticket = new Ticket(5), Stamp = new Stamp(6) },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Holder))]
public partial class ExternalSerializerModel : TypeModel
{
}
