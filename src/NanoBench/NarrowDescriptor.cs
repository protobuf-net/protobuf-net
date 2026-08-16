using ProtoBuf;
using System.Collections.Generic;

namespace ProtoBuf.Nano.Bench.DescriptorModel;

// The extension-data oracle fixture: a FileDescriptorProto narrowed to NOTHING - every field is
// unknown, so nano's capture must land each file's entire body in the extension bag, byte-
// preserving and in original order. That emptiness is what makes the round-trip oracle valid:
// legacy's writer emits declared members first and then the extension blob verbatim, so with no
// declared members the re-serialized document must be BYTE-IDENTICAL to the original payload.
// Deriving ProtoBuf.Extensible is the documented way in (and is exempt from the "derives from a
// type without [ProtoInclude]" refusal).

[ProtoContract]
public sealed class NarrowFileDescriptorSet
{
    [ProtoMember(1)] public List<NarrowFileDescriptorProto> Files { get; } = [];
}

[ProtoContract]
public sealed class NarrowFileDescriptorProto : Extensible
{
    // deliberately empty: the bag IS the message
}

[ProtoSerializable(typeof(NarrowFileDescriptorSet))]
public partial class NanoDescriptorModel
{
}
