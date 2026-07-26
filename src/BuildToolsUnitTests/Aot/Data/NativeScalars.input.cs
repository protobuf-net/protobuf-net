using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.NativeScalars;

// nint/nuint have dedicated built-in serializers that the generator did not emit. They are ordinary
// varints, and ref-emit asks for width 64 regardless of the platform - so FixedSize is Fixed64 on
// both, and the wire form does not vary by architecture.
//
// DateOnly/TimeOnly are the same class of gap but live in DateOnly.input.cs; the helpers they need
// exist only in the net6.0+ build of the library, which the golden tests do not reference.
[ProtoContract]
public class Natives
{
    [ProtoMember(1)] public nint Handle { get; set; }
    [ProtoMember(2)] public nuint Size { get; set; }

    [ProtoMember(3)] public nint? MaybeHandle { get; set; }
    [ProtoMember(4)] public nuint? MaybeSize { get; set; }

    [ProtoMember(5, DataFormat = DataFormat.FixedSize)] public nint Fixed { get; set; }
    [ProtoMember(6, DataFormat = DataFormat.ZigZag)] public nint Zigzag { get; set; }

    [ProtoMember(7)] public List<nint> Handles { get; set; }
    [ProtoMember(8)] public nint[] More { get; set; }
}

public static class NativeScalarsSamples
{
    public static object[] Values =>
    [
        new Natives(),
        new Natives { Handle = 42, Size = 7 },
        new Natives { MaybeHandle = 0, MaybeSize = 0 },
        new Natives { MaybeHandle = -3 },
        new Natives { Fixed = 5, Zigzag = -5 },
        new Natives { Handles = [1, -1, 0] },
        new Natives { More = [7, 8] },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Natives))]
public partial class NativeScalarsModel : TypeModel
{
}
