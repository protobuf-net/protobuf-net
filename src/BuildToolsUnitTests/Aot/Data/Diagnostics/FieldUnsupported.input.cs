using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.FieldUnsupported;

// ref-emit reaches these through reflection; generated code cannot. A readonly field has the same
// problem an init-only property does - it cannot be assigned after construction.
[ProtoContract]
public class ReadOnlyField
{
    [ProtoMember(1)] public readonly int Value;
}

[ProtoContract]
public class ConstField
{
    [ProtoMember(1)] public const int Value = 1;
}

[ProtoContract]
public class StaticField
{
    [ProtoMember(1)] public static int Value;
}

[ProtoContract]
public class PrivateField
{
    [ProtoMember(1)] private int Value;
}

[ProtoModel]
[ProtoSerializable(typeof(ReadOnlyField))]
[ProtoSerializable(typeof(ConstField))]
[ProtoSerializable(typeof(StaticField))]
[ProtoSerializable(typeof(PrivateField))]
public partial class FieldUnsupportedModel : TypeModel
{
}
