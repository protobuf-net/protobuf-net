using ProtoBuf;

// The compile-time equivalent of what AddNodaTime does to a RuntimeTypeModel. Because these are
// declared at *assembly* level, any generated model in a project that references this package picks
// them up without restating anything - which is the point: the NodaTime types live in one package,
// this helper is a second, and the consumer is a third that only wants its own contracts to work.
//
// The conversions are named rather than cast, because NodaTime's types carry no operators to our
// well-known types - the same reason SetSurrogate is given method pairs below.
[assembly: ProtoSurrogate(typeof(NodaTime.Duration), typeof(ProtoBuf.WellKnownTypes.Duration),
    Converter = typeof(ProtoBuf.Meta.NodaTimeExtensions),
    ToSurrogate = nameof(ProtoBuf.Meta.NodaTimeExtensions.ToProtoBufDuration),
    ToType = nameof(ProtoBuf.Meta.NodaTimeExtensions.ToNodaTimeDuration))]

[assembly: ProtoSurrogate(typeof(NodaTime.Instant), typeof(ProtoBuf.WellKnownTypes.Timestamp),
    Converter = typeof(ProtoBuf.Meta.NodaTimeExtensions),
    ToSurrogate = nameof(ProtoBuf.Meta.NodaTimeExtensions.ToProtoBufTimestamp),
    ToType = nameof(ProtoBuf.Meta.NodaTimeExtensions.ToNodaTimeInstant))]

// LocalDate and LocalTime have no well-known-type equivalent to pair with, so the shapes live in
// this package too - see NodaTimeSurrogates. No Converter: the surrogates carry conversion
// operators, which is what ProtoSurrogate uses when none is named.
//
// IsoDayOfWeek deliberately has no entry: it is an enum, which needs no help - AddNodaTime only
// names it, and naming is a schema concern.
[assembly: ProtoSurrogate(typeof(NodaTime.LocalDate), typeof(ProtoBuf.Meta.NodaTimeSurrogates.Date))]

[assembly: ProtoSurrogate(typeof(NodaTime.LocalTime), typeof(ProtoBuf.Meta.NodaTimeSurrogates.TimeOfDay))]
