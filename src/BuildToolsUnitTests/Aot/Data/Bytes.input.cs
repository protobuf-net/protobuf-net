using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.Bytes;

[ProtoContract]
public class Blob
{
    [ProtoMember(1)]
    public byte[] Payload { get; set; }

    [ProtoMember(2)]
    public byte[] Other { get; set; }

    // for contrast: a single byte is a varint scalar, not a bytes field
    [ProtoMember(3)]
    public byte Single { get; set; }
}

public static class BytesSamples
{
    public static object[] Values =>
    [
        new Blob(),                                             // both null
        new Blob { Payload = [] },                              // empty is not null
        new Blob { Payload = [1, 2, 3] },
        new Blob { Payload = [0] },                             // a single zero byte
        new Blob { Payload = [1], Other = [], Single = 7 },
        new Blob { Single = 0 },                                // implicit default on the scalar
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Blob))]
public partial class BytesModel : TypeModel
{
}
