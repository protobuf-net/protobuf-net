// gap B30 item 2: a refusal caused by an AMBIENT [ProtoDataFormat] used to read as though the
// member had asked for it - "DataFormat.ZigZag on a BCL type" reported against a member carrying no
// format attribute at all, and costing the whole contract plus everything referencing it.
//
// Contract-scoped rather than assembly- or module-scoped deliberately: the golden tests compile
// each input in isolation, but AotRefGen and AotConformanceTests link every fixture into ONE
// assembly, so a wider declaration would silently re-format every other fixture's members.
using ProtoBuf;
using ProtoBuf.Meta;
using System;

namespace AotFixtures.AmbientFormatRefusal;

[ProtoContract]
[ProtoDataFormat(typeof(DateTime), DataFormat.ZigZag)]
public class Stamped
{
    // no format attribute here: the ZigZag comes from the declaration above, and the diagnostic
    // has to say so or it is unactionable
    [ProtoMember(1)] public DateTime When { get; set; }
    [ProtoMember(2)] public int Id { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Stamped))]
public partial class AmbientFormatRefusalModel : TypeModel { }
