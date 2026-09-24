using ProtoBuf;
using ProtoBuf.Meta;
using System;

// [ProtoSurrogate] on the *model* is the compile-time equivalent of RuntimeTypeModel.SetSurrogate:
// it serializes a type that could never carry [ProtoContract(Surrogate = ...)] itself, because you
// do not own it. NodaTime is the motivating case.
//
// Note System.Uri is *not* an example, despite looking like one: protobuf-net has inbuilt behaviour
// for it (ProtoTypeCode.Uri, written as OriginalString), and SetSurrogate throws outright -
// "Data of this type has inbuilt behaviour, and cannot be added to a model in this way".
//
// AotRefGen replays these declarations onto the reference model via RuntimeTypeModel.SetSurrogate,
// so this *is* differentially covered - the cast form through the public MetaType.SetSurrogate(Type),
// and the named-method form through the generic overload that takes conversion delegates.
namespace AotFixtures.ModelSurrogate;

// the cast form, on a BCL type we do not own and protobuf-net has no inbuilt handling for
[ProtoContract]
public class VersionSurrogate
{
    [ProtoMember(1)] public string Value { get; set; }

    public static implicit operator VersionSurrogate(Version value)
        => value is null ? null : new VersionSurrogate { Value = value.ToString() };
    public static implicit operator Version(VersionSurrogate value)
        => value?.Value is null ? null : Version.Parse(value.Value);
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

// System.DateTimeOffset looks inbuilt and is not - protobuf-net has no serializer for it at all,
// which is why Examples/Issues/Issue222.cs registers a surrogate by hand. So a bare DateTimeOffset
// member is a refusal that *matches* ref-emit, and this is the whole fix for it.
[ProtoContract]
public class DateTimeOffsetSurrogate
{
    [ProtoMember(1)] public long Ticks { get; set; }
    [ProtoMember(2)] public short OffsetMinutes { get; set; }

    public static implicit operator DateTimeOffsetSurrogate(DateTimeOffset value)
        => new() { Ticks = value.Ticks, OffsetMinutes = (short)value.Offset.TotalMinutes };
    public static implicit operator DateTimeOffset(DateTimeOffsetSurrogate value)
        => value is null ? default
            : new DateTimeOffset(value.Ticks, TimeSpan.FromMinutes(value.OffsetMinutes));
}

[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public Version Release { get; set; }
    [ProtoMember(2)] public Ticks Elapsed { get; set; }
    [ProtoMember(3)] public DateTimeOffset When { get; set; }
}

public static class ModelSurrogateSamples
{
    public static object[] Values =>
    [
        new Holder(),
        new Holder { Release = new Version(1, 2, 3) },
        new Holder { Elapsed = new Ticks(1234567) },
        new Holder { Release = new Version(4, 5), Elapsed = new Ticks(-1) },
        new Holder { When = new DateTimeOffset(2026, 8, 5, 12, 30, 0, TimeSpan.FromHours(1)) },
        new Holder { When = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero) },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Holder))]
[ProtoSurrogate(typeof(Version), typeof(VersionSurrogate))]
[ProtoSurrogate(typeof(DateTimeOffset), typeof(DateTimeOffsetSurrogate))]
[ProtoSurrogate(typeof(Ticks), typeof(TicksSurrogate),
    Converter = typeof(TicksConverter),
    ToSurrogate = nameof(TicksConverter.ToSurrogate),
    ToType = nameof(TicksConverter.FromSurrogate))]
public partial class ModelSurrogateModel : TypeModel
{
}
