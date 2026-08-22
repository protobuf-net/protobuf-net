// A serialize callback on a HIERARCHY layer. EmitSubTypeContract never calls EmitCallback, so the
// question this fixture answers is whether such a callback reaches the emitted code at all - it is
// under Diagnostics/ purely so it can be inspected without being linked into AotRefGen or
// AotConformanceTests, not because it is expected to produce a diagnostic.
using ProtoBuf;
using ProtoBuf.Meta;
using System.Runtime.Serialization;

namespace AotFixtures.Diagnostics.HierarchyCallback;

[ProtoContract]
[ProtoInclude(10, typeof(Derived))]
public class Base
{
    [ProtoMember(1)] public int Id { get; set; }

    [ProtoBeforeSerialization] public void BeforeSer() { }
    [ProtoAfterSerialization] public void AfterSer() { }
}

[ProtoContract]
public class Derived : Base
{
    [ProtoMember(2)] public string Name { get; set; }

    [OnSerializing] public void OnSer(StreamingContext context) { }
}

[ProtoModel]
[ProtoSerializable(typeof(Base))]
public partial class HierarchyCallbackModel : TypeModel { }
