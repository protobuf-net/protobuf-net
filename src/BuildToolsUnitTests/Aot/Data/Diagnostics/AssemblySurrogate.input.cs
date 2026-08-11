using ProtoBuf;
using ProtoBuf.Meta;
using System;

// [ProtoSurrogate] can be declared on an *assembly* as well as on a model, which is what lets a
// package ship surrogates for the types it supports - protobuf-net.NodaTime could carry these for
// NodaTime.Duration and NodaTime.Instant, and a consumer would get them without restating anything.
//
// Scanning assembly attributes of referenced assemblies is cheap and bounded; scanning every type in
// every reference would not be. Gathering runs least-to-most specific, so a model's own declaration
// wins over one it merely references.
//
// Under Diagnostics/ because ref-emit has nothing to compare against: AotRefGen would have to replay
// the declarations against a RuntimeTypeModel first.
[assembly: ProtoSurrogate(typeof(Version), typeof(AotFixtures.AssemblySurrogate.VersionSurrogate))]

namespace AotFixtures.AssemblySurrogate;

[ProtoContract]
public class VersionSurrogate
{
    [ProtoMember(1)] public string Value { get; set; }

    public static implicit operator VersionSurrogate(Version value)
        => value is null ? null : new VersionSurrogate { Value = value.ToString() };
    public static implicit operator Version(VersionSurrogate value)
        => value?.Value is null ? null : Version.Parse(value.Value);
}

[ProtoContract]
public class Holder
{
    // no declaration on this model at all: the pairing comes from the assembly
    [ProtoMember(1)] public Version Version { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Holder))]
public partial class AssemblySurrogateModel : TypeModel
{
}
