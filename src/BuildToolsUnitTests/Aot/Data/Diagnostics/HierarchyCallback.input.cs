// A serialize callback on a HIERARCHY layer, which is SILENTLY DROPPED - see notes/gaps.md B46.
// EmitSubTypeContract has never called EmitCallback, so neither WriteSubType nor (deliberately, so
// the two cannot disagree about a length) MeasureSub_ fires one. This fixture pins that: the golden
// beside it contains no call to any of the three callbacks declared below, and a fix will move it.
//
// It is under Diagnostics/ so it can be INSPECTED without being linked into AotRefGen or
// AotConformanceTests, not because it reports a diagnostic - it reports none. Once B46 is fixed it
// belongs in Data/ proper, where the conformance suite compares bytes against ref-emit (which does
// fire these) and would therefore catch a fix that got the sequence wrong.
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
