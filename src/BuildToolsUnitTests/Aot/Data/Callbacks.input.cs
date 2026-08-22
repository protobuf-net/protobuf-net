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
public partial class CallbacksModel : TypeModel
{
}
