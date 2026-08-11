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

    // the other three "bytes" shapes. ArraySegment<byte> is the one worth having: it satisfies the
    // auto-tuple predicate exactly - a (T[], int, int) constructor with matching read-only
    // Array/Offset/Count - so before the bytes test was moved above the tuple test it went out as a
    // three-member message, with Offset and Count written unconditionally. Found by the corpus
    // differential on Examples.Issues.SO16838287.
    [ProtoMember(4)]
    public System.ArraySegment<byte> Segment { get; set; }

    [ProtoMember(5)]
    public System.Memory<byte> Memory { get; set; }

    [ProtoMember(6)]
    public System.ReadOnlyMemory<byte> ReadOnly { get; set; }
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

        new Blob { Segment = new System.ArraySegment<byte>([1, 2, 3], 1, 2) },
        new Blob { Segment = default },                          // a null-backed segment
        new Blob { Memory = new byte[] { 4, 5 }, ReadOnly = new byte[] { 6 } },
        new Blob { Memory = default, ReadOnly = default },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Blob))]
public partial class BytesModel : TypeModel
{
}
