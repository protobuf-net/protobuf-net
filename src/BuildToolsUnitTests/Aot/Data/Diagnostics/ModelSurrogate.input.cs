using ProtoBuf;
using ProtoBuf.Meta;
using System;

// [ProtoSurrogate] on the *model* is the compile-time equivalent of RuntimeTypeModel.SetSurrogate:
// it serializes a type that could never carry [ProtoContract(Surrogate = ...)] itself, because you
// do not own it. That is what the coverage sweep's member-type tail is waiting on - System.Uri,
// System.Type, IPAddress, NodaTime and so on.
//
// This lives under Diagnostics/ because ref-emit has no equivalent to compare against: AotRefGen
// would have to replay the declarations against a RuntimeTypeModel, which it does not yet do.
namespace AotFixtures.ModelSurrogate;

[ProtoContract]
public class UriSurrogate
{
    [ProtoMember(1)] public string Value { get; set; }

    public static implicit operator UriSurrogate(Uri value)
        => value is null ? null : new UriSurrogate { Value = value.OriginalString };
    public static implicit operator Uri(UriSurrogate value)
        => value?.Value is null ? null : new Uri(value.Value);
}

// the NodaTime shape: a struct with no usable operators, converted by named static methods
public readonly struct Ticks
{
    public Ticks(long value) => Value = value;
    public long Value { get; }
}

[ProtoContract]
public class TicksSurrogate
{
    [ProtoMember(1)] public long Value { get; set; }
}

public static class TicksConverter
{
    public static TicksSurrogate ToSurrogate(Ticks value) => new() { Value = value.Value };
    public static Ticks FromSurrogate(TicksSurrogate value)
        => value is null ? default : new Ticks(value.Value);
}

[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public Uri Link { get; set; }
    [ProtoMember(2)] public Ticks Elapsed { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Holder))]
[ProtoSurrogate(typeof(Uri), typeof(UriSurrogate))]
[ProtoSurrogate(typeof(Ticks), typeof(TicksSurrogate),
    Converter = typeof(TicksConverter),
    ToSurrogate = nameof(TicksConverter.ToSurrogate),
    ToType = nameof(TicksConverter.FromSurrogate))]
public partial class ModelSurrogateModel : TypeModel
{
}
