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

public static class CallbacksSamples
{
    public static object[] Values =>
    [
        new Hooked(),
        new Hooked { Value = 7 },
        new Standard { Value = 8 },
        new AfterOnly { Value = 9 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Hooked))]
[ProtoSerializable(typeof(Standard))]
[ProtoSerializable(typeof(AfterOnly))]
public partial class CallbacksModel : TypeModel
{
}
