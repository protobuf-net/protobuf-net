using ProtoBuf;
using ProtoBuf.Meta;

// pinned below the generator's floor by the sidecar LowLangVer.langver; the generator should
// report PBN3000 and emit no model. Fixtures under Diagnostics/ are golden-tested only - they are
// deliberately not linked into AotRefGen or AotConformanceTests.
namespace AotFixtures.LowLangVer;

[ProtoContract]
public class Thing
{
    [ProtoMember(1)]
    public int Value { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Thing))]
public partial class LowLangVerModel : TypeModel
{
}
