using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.SchemaOnly;

// Several protobuf-net options exist only to shape the generated .proto and never reach the wire.
// Refusing a contract over one of those loses a serializer for no reason - and by the coverage sweep
// they are common, so each is accepted and ignored.

// an empty message is entirely legal protobuf, and .proto-generated DTOs are full of them; there is
// nothing to switch on, so the read is a bare skip loop
[ProtoContract]
public class Empty
{
}

// the baseline the annotated ones below match byte-for-byte
[ProtoContract]
public class Plain
{
    [ProtoMember(1)] public int Value { get; set; }
    [ProtoMember(2)] public string Text { get; set; }
}

// Name and Origin are schema naming; [ProtoReserved] holds field numbers back in the .proto
[ProtoContract(Name = "renamed", Origin = "somewhere.proto")]
[ProtoReserved(50, 60, "held back")]
public class SchemaOnly
{
    [ProtoMember(1, Name = "value")] public int Value { get; set; }
    [ProtoMember(2, Name = "text")] public string Text { get; set; }
}

// [ProtoIgnore] excludes the member, rather than being a reason to drop the contract
[ProtoContract]
public class Ignoring
{
    [ProtoMember(1)] public int Value { get; set; }
    [ProtoIgnore] public int Skipped { get; set; }
    [ProtoMember(2)] public string Text { get; set; }
}

// an empty message that still has to carry unknown fields
[ProtoContract]
public class EmptyExtensible : ProtoBuf.Extensible
{
}

public static class SchemaOnlySamples
{
    public static object[] Values =>
    [
        new Empty(),
        new Plain(),
        new Plain { Value = 1, Text = "a" },
        new SchemaOnly(),
        new SchemaOnly { Value = 2, Text = "b" },
        new Ignoring(),
        new Ignoring { Value = 3, Skipped = 99, Text = "c" },
        new EmptyExtensible(),
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Empty))]
[ProtoSerializable(typeof(Plain))]
[ProtoSerializable(typeof(SchemaOnly))]
[ProtoSerializable(typeof(Ignoring))]
[ProtoSerializable(typeof(EmptyExtensible))]
public partial class SchemaOnlyModel : TypeModel
{
}
