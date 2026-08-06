using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.InheritAccessor;

// A member needing [UnsafeAccessor] *inside a hierarchy*. Nothing covered this combination, and it
// did not compile: ReadSubType hoists the instance into a per-case local because reading value.Value
// is what constructs it, but the accessor call was passing the literal `value` - which there is the
// SubTypeState<T> wrapper, not the contract.
//
// Only reachable from the corpus once the harness stopped filtering non-public members out of
// metadata; see docs/aot-findings.md item 8.

[ProtoContract]
[ProtoInclude(10, typeof(Derived))]
public class Base
{
    // a private field, so both directions go through the accessor
    [ProtoMember(1)] private int _count;
    public int Count { get => _count; set => _count = value; }

    // a getter-only auto-property, assigned through its backing field
    [ProtoMember(2)] public string Label { get; }

    // an init-only setter, the third accessor shape
    [ProtoMember(3)] public int Ordinal { get; init; }

    // a collection reached the same way, since that is what turned it up in the corpus
    [ProtoMember(4)] private List<int> _values;
    public List<int> Values { get => _values; set => _values = value; }

    public Base() { }
    public Base(string label) => Label = label;
}

[ProtoContract]
public class Derived : Base
{
    [ProtoMember(5)] private string _extra;
    public string Extra { get => _extra; set => _extra = value; }

    public Derived() { }
    public Derived(string label, string extra) : base(label) => _extra = extra;
}

// the same again on a struct, where the accessor takes `ref` - a struct cannot be in a hierarchy,
// so this is the contrast that keeps the `ref` path covered
[ProtoContract]
public struct Holder
{
    [ProtoMember(1)] private int _n;
    public int N { get => _n; set => _n = value; }
}

public static class InheritAccessorSamples
{
    public static object[] Values =>
    [
        new Base(),
        new Base("root") { Count = 1, Ordinal = 2, Values = [3, 4] },
        new Derived(),
        new Derived("leaf", "x") { Count = 5, Ordinal = 6, Values = [7] },
        new Holder(),
        new Holder { N = 8 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Base))]
[ProtoSerializable(typeof(Holder))]
public partial class InheritAccessorModel : TypeModel
{
}
