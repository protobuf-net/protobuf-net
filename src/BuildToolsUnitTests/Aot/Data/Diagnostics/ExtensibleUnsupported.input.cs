using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.ExtensibleUnsupported;

// ref-emit refuses both of these while *building the model* rather than emitting anything, so there
// is nothing to reproduce - we refuse them up front instead.
[ProtoContract]
public struct ExtensibleStruct : IExtensible
{
    private IExtension _extension;
    IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        => Extensible.GetExtensionObject(ref _extension, createIfMissing);

    [ProtoMember(1)] public int Value { get; set; }
}

// untyped extensions cannot survive inheritance: one bag would have to serve every layer, and the
// same field number means different things at each
[ProtoContract]
[ProtoInclude(100, typeof(UntypedDerived))]
public class UntypedBase : IExtensible
{
    private IExtension _extension;
    IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        => Extensible.GetExtensionObject(ref _extension, createIfMissing);

    [ProtoMember(1)] public int Shared { get; set; }
}

[ProtoContract]
public class UntypedDerived : UntypedBase
{
    [ProtoMember(2)] public int Extra { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(ExtensibleStruct))]
[ProtoSerializable(typeof(UntypedBase))]
public partial class ExtensibleUnsupportedModel : TypeModel
{
}
