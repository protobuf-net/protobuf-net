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

[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public Thing Thing { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Holder))]
public partial class ExternalSerializerModel : TypeModel
{
}

public static class ExternalSerializerSamples
{
    public static object[] Values =>
    [
        new Holder(),
        new Holder { Thing = new Thing { Value = 1 } },
        new Holder { Thing = new Thing { Value = 2 } },
    ];
}
