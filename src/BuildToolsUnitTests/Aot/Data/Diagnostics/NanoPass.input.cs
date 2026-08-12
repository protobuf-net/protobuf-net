// Not a diagnostics fixture: it lives here because this folder is golden-only (excluded from
// AotRefGen and AotConformanceTests), and this fixture's stub ReaderState must not leak into the
// linked fixture assemblies - the nano pass is symbol-gated, and this stub is the symbol. The
// golden output is the point: it shows the second emission pass, side by side with the first.
using ProtoBuf;
using System.Collections.Generic;

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
        public ReadScope PushScope(uint tag) => throw new System.NotImplementedException();
        public ReadScope PushLengthPrefix() => throw new System.NotImplementedException();
        public void PopScope(in ReadScope prior) => throw new System.NotImplementedException();
        public bool IsScopeEnd(uint tag) => throw new System.NotImplementedException();
        public bool AtScopeEnd => throw new System.NotImplementedException();
        public void ReadPackedVarint32(List<int> values) => throw new System.NotImplementedException();
    }

    public readonly struct ReadScope
    {
    }
}

namespace AotFixtures.NanoPass
{
    public enum Status
    {
        Unknown = 0,
        Open = 1,
        Closed = 2,
    }

    [ProtoContract]
    public class Order
    {
        [ProtoMember(1)] public int Id { get; set; }
        [ProtoMember(2)] public string Name { get; set; }
        [ProtoMember(3)] public bool Active { get; set; }
        [ProtoMember(4)] public long Total { get; set; }
        [ProtoMember(5)] public Status Status { get; set; }
        [ProtoMember(6)] public int? Priority { get; set; }
        [ProtoMember(7)] public List<string> Tags { get; } = new List<string>();
        [ProtoMember(8)] public List<int> Codes { get; } = new List<int>();
        [ProtoMember(9)] public List<Child> Items { get; } = new List<Child>();
        [ProtoMember(10)] public Child Favourite { get; set; }
    }

    [ProtoContract]
    public class Child
    {
        [ProtoMember(1)] public int Value { get; set; }
    }

    // ineligible for the nano pass (map member), and the CASCADE is the point: Chain references
    // it through a message member, so Chain falls out of the nano set too even though Chain is
    // eligible alone - emitting a call to a NanoRead_ method that does not exist would be a
    // compile error in the consumer's build
    [ProtoContract]
    public class Holder
    {
        [ProtoMember(1)] public Dictionary<int, string> Lookup { get; } = new Dictionary<int, string>();
    }

    [ProtoContract]
    public class Chain
    {
        [ProtoMember(1)] public Holder Inner { get; set; }
    }

    [ProtoModel]
    [ProtoSerializable(typeof(Order))]
    [ProtoSerializable(typeof(Holder))]
    [ProtoSerializable(typeof(Chain))]
    public partial class NanoPassModel : ProtoBuf.Meta.TypeModel
    {
    }
}
