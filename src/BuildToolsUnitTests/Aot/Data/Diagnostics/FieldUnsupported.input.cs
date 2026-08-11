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

// note a *private* field is no longer here: [UnsafeAccessor] reaches one by name, which is the same
// mechanism a non-public setter uses and the same divergence from ref-emit's compiled path.
// ImplicitFields.AllFields depends on it, and Implicit.input.cs covers it.

[ProtoModel]
[ProtoSerializable(typeof(ReadOnlyField))]
[ProtoSerializable(typeof(ConstField))]
[ProtoSerializable(typeof(StaticField))]
public partial class FieldUnsupportedModel : TypeModel
{
}
