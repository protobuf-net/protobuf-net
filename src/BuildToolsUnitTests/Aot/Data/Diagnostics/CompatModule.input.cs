using ProtoBuf;
using ProtoBuf.Meta;
using System;

// A module-level default, which is the recommended way to opt a whole project into newer
// conventions. It lives here rather than under Data/ because a module attribute applies to the whole
// *assembly*: AotRefGen and AotConformanceTests link every fixture into one, so putting it beside
// them would silently re-level all of them. The golden tests compile each input in isolation, which
// is exactly what is needed to prove the resolution order without side effects.
//
// Nothing here produces a diagnostic - the fixture is here purely for the isolation.
[module: CompatibilityLevel(CompatibilityLevel.Level300)]

namespace AotFixtures.CompatModule;

// no attribute of its own, so it picks up the module's level
[ProtoContract]
public class FromModule
{
    [ProtoMember(1)] public DateTime When { get; set; }
    [ProtoMember(2)] public Guid Id { get; set; }
    [ProtoMember(3)] public decimal Amount { get; set; }
}

// a type-level attribute still wins over the module
[ProtoContract, CompatibilityLevel(CompatibilityLevel.Level200)]
public class OverridesModule
{
    [ProtoMember(1)] public DateTime When { get; set; }
    [ProtoMember(2)] public Guid Id { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(FromModule))]
[ProtoSerializable(typeof(OverridesModule))]
public partial class CompatModuleModel : TypeModel
{
}
