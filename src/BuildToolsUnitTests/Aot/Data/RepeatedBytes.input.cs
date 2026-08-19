using ProtoBuf;
using System.Collections.Generic;

namespace AotFixtures.RepeatedBytes;

// `repeated bytes` — which protogen emits as List<byte[]> — had NO fixture at all before this,
// which is why the shape was never on the raw path: nothing would have caught it either way.
//
// Both collection shapes, because they reach the span differently (an array is one natively, a
// List<T> only through CollectionsMarshal), and a nullable-free element type because protobuf-net
// REJECTS null elements inside a collection (ThrowNullRepeatedContents) - so the samples must not
// contain one, and the generated loop has to raise exactly that.

[ProtoContract]
public class BytesHolder
{
    [ProtoMember(1)] public List<byte[]> Chunks { get; set; }
    [ProtoMember(2)] public byte[][] Blocks { get; set; }

    // a plain bytes member alongside, so the golden shows the unary and repeated forms together -
    // they share MeasureRawVarint32(len) + len and should stay recognisably the same shape
    [ProtoMember(3)] public byte[] Single { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(BytesHolder))]
public partial class RepeatedBytesModel : ProtoBuf.Meta.TypeModel { }

public static class RepeatedBytesSamples
{
    public static object[] Values =>
    [
        new BytesHolder(),
        new BytesHolder
        {
            // deliberately including an EMPTY element: a zero-length bytes value is legal and
            // still writes its tag and a zero length, which a "skip if empty" slip would drop
            Chunks = [[1, 2, 3], [], [255]],
            Blocks = [[7], [8, 9]],
            Single = [10, 11],
        },
    ];
}
