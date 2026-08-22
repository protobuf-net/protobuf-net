using ProtoBuf;
using ProtoBuf.Meta;
using System.Runtime.Serialization;

namespace AotFixtures.Callbacks;

// The serialization callback families: protobuf-net's own [ProtoBeforeSerialization] etc, and the
// System.Runtime.Serialization [OnSerializing] family, which MetaType honours identically.
[ProtoContract]
public class Hooked
{
    [ProtoMember(1)] public int Value { get; set; }

    // not serialized; the callbacks are the only thing that sets it, so it is how the tests observe
    // that they ran at all
    public string Trace { get; set; } = "";

    [ProtoBeforeSerialization] public void BeforeSer() => Trace += "bs;";
    [ProtoAfterSerialization] public void AfterSer() => Trace += "as;";
    [ProtoBeforeDeserialization] public void BeforeDes() => Trace += "bd;";
    [ProtoAfterDeserialization] public void AfterDes() => Trace += "ad;";
}

// the System.Runtime.Serialization spelling, which takes a StreamingContext
[ProtoContract]
public class Standard
{
    [ProtoMember(1)] public int Value { get; set; }

    public string Trace { get; set; } = "";

    [OnSerializing] public void OnSer(StreamingContext context) => Trace += "os;";
    [OnSerialized] public void OnSerd(StreamingContext context) => Trace += "od;";
    [OnDeserializing] public void OnDes(StreamingContext context) => Trace += "ds;";
    [OnDeserialized] public void OnDesd(StreamingContext context) => Trace += "dd;";
}

// only some of them, which is the common case
[ProtoContract]
public class AfterOnly
{
    [ProtoMember(1)] public int Value { get; set; }
    public string Trace { get; set; } = "";

    [ProtoAfterDeserialization] public void AfterDes() => Trace += "ad;";
}

// Nests a callback-bearing contract, so something above it needs a length and the measure pass
// actually runs. Without this the fixture only ever serialized callback contracts as ROOTS, where
// nothing needs a length and so the before-serialization hook fires exactly once - which is correct
// but says nothing about the two-pass behaviour that gap B42 turns on.
[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public Hooked Inner { get; set; }
    [ProtoMember(2)] public int Tag { get; set; }
}

// Takes the ISerializationContext flavour, which is the only one carrying the context OBJECT and
// so the only one ProtoWriter.IsMeasuring can be asked about. That matters because a measure-first
// contract fires before-serialization in BOTH passes; a callback that cannot tell them apart sees
// its side-effects doubled with no way to notice.
[ProtoContract]
public class Watched
{
    [ProtoMember(1)] public int Value { get; set; }

    public string Trace { get; set; } = "";

    [ProtoBeforeSerialization]
    public void BeforeSer(ISerializationContext context)
        => Trace += ProtoWriter.IsMeasuring(context) ? "bs*;" : "bs;";

    [ProtoAfterSerialization] public void AfterSer() => Trace += "as;";
}

// ...and the same nested, which is where the measure pass actually runs
[ProtoContract]
public class WatchedHolder
{
    [ProtoMember(1)] public Watched Inner { get; set; }
}

// ONE contract whose four callbacks take three different shapes, which gap B9 asked for: the
// validator (CallbackSet.CheckCallbackParameters), the reflection invoker, ref-emit and this
// generator all have to agree on the accepted set, and they demonstrably disagreed once before.
// The differential comparing this against RuntimeTypeModel is that cross-check.
[ProtoContract]
public class Mixed
{
    [ProtoMember(1)] public int Value { get; set; }

    public string Trace { get; set; } = "";

    [ProtoBeforeSerialization]
    public void BeforeSer(ISerializationContext context)
        => Trace += ProtoWriter.IsMeasuring(context) ? "bs*;" : "bs;";

    [ProtoAfterSerialization] public void AfterSer(StreamingContext context) => Trace += "as;";
    [ProtoBeforeDeserialization] public void BeforeDes() => Trace += "bd;";
    [ProtoAfterDeserialization] public void AfterDes(ISerializationContext context) => Trace += "ad;";
}

// gap B46: a HIERARCHY fires the ROOT's serialize callbacks and NOTHING ELSE. That was probed
// against ref-emit rather than reasoned about, and the probe is the only reason to believe it: the
// obvious guess - one pair per layer, the way members work - is wrong. With a distinct callback on
// each of three layers, RuntimeTypeModel fires the root's and no other, whatever the runtime type
// and whatever declared type it is serialized as.
//
// Trace is not a serialized member, so none of this moves the wire bytes and the differential still
// compares cleanly; the sequence itself is asserted by CallbackHierarchyTests.
[ProtoContract]
[ProtoInclude(10, typeof(HookedDerived))]
public class HookedBase
{
    [ProtoMember(1)] public int Value { get; set; }

    public string Trace { get; set; } = "";

    [ProtoBeforeSerialization] public void BeforeSer() => Trace += "base-bs;";
    [ProtoAfterSerialization] public void AfterSer() => Trace += "base-as;";
    // the DESERIALIZE pair too: ref-emit fires the root's through
    // SubTypeState<T>.OnBeforeDeserialize, which runs at MATERIALISATION rather than at a fixed
    // point in the field loop - probed, and the reason this needed its own mechanism
    [ProtoBeforeDeserialization] public void BeforeDes() => Trace += "base-bd;";
    [ProtoAfterDeserialization] public void AfterDes() => Trace += "base-ad;";
}

[ProtoContract]
public class HookedDerived : HookedBase
{
    [ProtoMember(2)] public int Extra { get; set; }

    // these must NEVER fire - see above. They are here precisely so that "the root's callbacks run"
    // is distinguishable from "every layer's callbacks run", which an empty derived layer would not
    // have told apart.
    [ProtoBeforeSerialization] public void DerivedBeforeSer() => Trace += "derived-bs;";
    [ProtoAfterSerialization] public void DerivedAfterSer() => Trace += "derived-as;";
    [ProtoBeforeDeserialization] public void DerivedBeforeDes() => Trace += "derived-bd;";
    [ProtoAfterDeserialization] public void DerivedAfterDes() => Trace += "derived-ad;";
}

// ...and nested, so something above needs a length and the measure pass genuinely runs over a
// hierarchy rather than only over a flat contract
[ProtoContract]
public class HookedHolder
{
    [ProtoMember(1)] public HookedBase Inner { get; set; }
}

public static class CallbacksSamples
{
    public static object[] Values =>
    [
        new Hooked(),
        new Hooked { Value = 7 },
        new Standard { Value = 8 },
        new AfterOnly { Value = 9 },
        new Holder(),
        new Holder { Inner = new Hooked { Value = 3 }, Tag = 4 },
        new Watched { Value = 5 },
        new WatchedHolder { Inner = new Watched { Value = 6 } },
        new Mixed { Value = 7 },
        new Holder { Inner = new Hooked { Value = 8 }, Tag = 9 },
        new HookedBase { Value = 10 },
        new HookedDerived { Value = 11, Extra = 12 },
        new HookedHolder { Inner = new HookedDerived { Value = 13, Extra = 14 } },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Hooked))]
[ProtoSerializable(typeof(Standard))]
[ProtoSerializable(typeof(AfterOnly))]
[ProtoSerializable(typeof(Holder))]
[ProtoSerializable(typeof(Watched))]
[ProtoSerializable(typeof(WatchedHolder))]
[ProtoSerializable(typeof(Mixed))]
[ProtoSerializable(typeof(HookedBase))]
[ProtoSerializable(typeof(HookedHolder))]
public partial class CallbacksModel : TypeModel
{
}
