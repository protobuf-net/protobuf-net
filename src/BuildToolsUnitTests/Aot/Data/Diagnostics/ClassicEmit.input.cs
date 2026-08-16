// Not a diagnostics fixture: it lives here because this folder is golden-only (excluded from
// AotRefGen and AotConformanceTests). [ProtoModel(ClassicEmit = true)] is the escape hatch:
// it suppresses the optimized read emission entirely - no RawRead_ statics, no proxy reads,
// no breadcrumbs - leaving exactly the classic bodies. The golden is the proof that the
// switch reverts the whole pass; the contract here is deliberately raw-eligible, so any
// RawRead_ appearing in the output means the hatch has stopped working.
using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.ClassicEmit
{
    [ProtoContract]
    public class Plain
    {
        [ProtoMember(1)] public int Id { get; set; }
        [ProtoMember(2)] public string Name { get; set; }
    }

    [ProtoModel(ClassicEmit = true)]
    [ProtoSerializable(typeof(Plain))]
    public partial class ClassicEmitModel : TypeModel
    {
    }
}
