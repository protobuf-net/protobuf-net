// Not a diagnostics fixture: it lives here because this folder is golden-only (excluded from
// AotRefGen and AotConformanceTests), and this fixture's stub ReaderState must not leak into the
// linked fixture assemblies - the nano pass is symbol-gated, and this stub is the symbol. The
// golden output is the point: it shows the second emission pass, side by side with the first.
using ProtoBuf;

namespace ProtoBuf.Nano
{
    // the experimental raw reader, stubbed: the generator gates the nano pass on this type being
    // visible, and matches it by full metadata name
    public ref struct ReaderState
    {
        public uint ReadRawTag() => throw new System.NotImplementedException();
        public void SkipTag(uint tag) => throw new System.NotImplementedException();
        public uint ReadRawVarint32() => throw new System.NotImplementedException();
        public ulong ReadRawVarint64() => throw new System.NotImplementedException();
        public uint ReadRawFixed32() => throw new System.NotImplementedException();
        public ulong ReadRawFixed64() => throw new System.NotImplementedException();
        public string ReadRawString() => throw new System.NotImplementedException();
    }
}

namespace AotFixtures.NanoPass
{
    [ProtoContract]
    public class Order
    {
        [ProtoMember(1)] public int Id { get; set; }
        [ProtoMember(2)] public string Name { get; set; }
        [ProtoMember(3)] public bool Active { get; set; }
        [ProtoMember(4)] public long Total { get; set; }
    }

    // ineligible for the nano pass (sub-message member): gets the legacy emission only, which is
    // the eligibility filter visibly doing its job in the golden
    [ProtoContract]
    public class Holder
    {
        [ProtoMember(1)] public Order Inner { get; set; }
    }

    [ProtoModel]
    [ProtoSerializable(typeof(Order))]
    [ProtoSerializable(typeof(Holder))]
    public partial class NanoPassModel : ProtoBuf.Meta.TypeModel
    {
    }
}
