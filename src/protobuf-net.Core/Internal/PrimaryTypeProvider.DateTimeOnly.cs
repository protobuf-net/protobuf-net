using ProtoBuf.Serializers;
using System;

#if NET6_0_OR_GREATER
namespace ProtoBuf.Internal
{
    // map DateOnly as "int32" (days since January 1, 0001 in the Proleptic Gregorian calendar),
    // and TimeOnly as "int64" (ticks into day, where a tick is 100ns)
    //
    // it was tempting to map to Date and TimeOfDay respectively, but this has problems:
    // - Date allows dates a date without a year to be expressed, which DateOnly does not
    // - TimeOfDay allows 24:00 to be expressed, which TimeOnly does not
    // likewise, there is Timestamp. but that is ... awkward and heavy for pure dates,
    // and Duration has larger range which will explode TimeOnly - it would be artificial
    // to pretend that they can interop with either of these
    //
    // either way, there's also a minor precision issue, since the google types go to
    // nanosecond precision, and TimeOfDay only allows ticks (100ns), but in reality this
    // is unlikely to be a problem; anyone requiring better accuracy should probably handle
    // it manually, or (simpler) use WellKnownTypes.Duration / WellKnownTypes.Timestamp directly
    //
    // refs:
    // https://github.com/protocolbuffers/protobuf/blob/main/src/google/protobuf/timestamp.proto
    // https://github.com/protocolbuffers/protobuf/blob/main/src/google/protobuf/duration.proto
    // https://github.com/googleapis/googleapis/blob/master/google/type/timeofday.proto
    // https://github.com/googleapis/googleapis/blob/master/google/type/date.proto

    partial class PrimaryTypeProvider :
        ISerializer<DateOnly>, ISerializer<DateOnly?>,
        IValueChecker<DateOnly>, IValueChecker<DateOnly?>,
        IValueChecker<TimeOnly>, IValueChecker<TimeOnly?>,
        IMeasuringSerializer<DateOnly>, IMeasuringSerializer<DateOnly?>,
        IMeasuringSerializer<TimeOnly>, IMeasuringSerializer<TimeOnly?>,

        ISerializer<TimeOnly>, ISerializer<TimeOnly?> 
    {
        SerializerFeatures ISerializer<DateOnly>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
        SerializerFeatures ISerializer<DateOnly?>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;

        SerializerFeatures ISerializer<TimeOnly>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
        SerializerFeatures ISerializer<TimeOnly?>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;

        TimeOnly ISerializer<TimeOnly>.Read(ref ProtoReader.State state, TimeOnly value)
            => new TimeOnly(state.ReadInt64());

        void ISerializer<TimeOnly>.Write(ref ProtoWriter.State state, TimeOnly value)
            => state.WriteInt64(value.Ticks);

        TimeOnly? ISerializer<TimeOnly?>.Read(ref ProtoReader.State state, TimeOnly? value)
            => new TimeOnly(state.ReadInt64());

        void ISerializer<TimeOnly?>.Write(ref ProtoWriter.State state, TimeOnly? value)
            => state.WriteInt64(value.GetValueOrDefault().Ticks);

        DateOnly ISerializer<DateOnly>.Read(ref ProtoReader.State state, DateOnly value)
            => DateOnly.FromDayNumber(state.ReadInt32());

        void ISerializer<DateOnly>.Write(ref ProtoWriter.State state, DateOnly value)
            => state.WriteInt32(value.DayNumber);

        DateOnly? ISerializer<DateOnly?>.Read(ref ProtoReader.State state, DateOnly? value)
            => DateOnly.FromDayNumber(state.ReadInt32());

        void ISerializer<DateOnly?>.Write(ref ProtoWriter.State state, DateOnly? value)
            => state.WriteInt32(value.GetValueOrDefault().DayNumber);


        bool IValueChecker<DateOnly>.HasNonTrivialValue(DateOnly value) => value.DayNumber != 0;
        bool IValueChecker<DateOnly>.IsNull(DateOnly value) => false;

        bool IValueChecker<DateOnly?>.HasNonTrivialValue(DateOnly? value) => value.GetValueOrDefault().DayNumber != 0;
        bool IValueChecker<DateOnly?>.IsNull(DateOnly? value) => value is null;
        int IMeasuringSerializer<DateOnly>.Measure(ISerializationContext context, WireType wireType, DateOnly value)
            => ProtoWriter.MeasureInt32(value.DayNumber);

        int IMeasuringSerializer<DateOnly?>.Measure(ISerializationContext context, WireType wireType, DateOnly? value)
            => ProtoWriter.MeasureInt32(value.Value.DayNumber);

        bool IValueChecker<TimeOnly>.HasNonTrivialValue(TimeOnly value) => value.Ticks != 0;
        bool IValueChecker<TimeOnly>.IsNull(TimeOnly value) => false;

        bool IValueChecker<TimeOnly?>.HasNonTrivialValue(TimeOnly? value) => value.GetValueOrDefault().Ticks != 0;
        bool IValueChecker<TimeOnly?>.IsNull(TimeOnly? value) => value is null;
        int IMeasuringSerializer<TimeOnly>.Measure(ISerializationContext context, WireType wireType, TimeOnly value)
            => ProtoWriter.MeasureInt64(value.Ticks);

        int IMeasuringSerializer<TimeOnly?>.Measure(ISerializationContext context, WireType wireType, TimeOnly? value)
            => ProtoWriter.MeasureInt64(value.Value.Ticks);
    }
}
#endif