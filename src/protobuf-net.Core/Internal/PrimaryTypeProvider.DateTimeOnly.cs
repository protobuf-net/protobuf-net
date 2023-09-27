using ProtoBuf.Serializers;
using ProtoBuf.WellKnownTypes;
using System;

#if NET6_0_OR_GREATER
namespace ProtoBuf.Internal
{
    // map DateOnly as "int32" (days since January 1, 0001 in the Proleptic Gregorian calendar), and TimeOnly as Duration
    // it was tempting to map to Date and TimeOfDay respectively, but this has problems:
    // - Date allows dates a date without a year to be expressed, which DateOnly does not
    // - TimeOfDay allows 24:00 to be expressed, which TimeOnly does not
    // likewise, there is Timestamp. but that is ... awkward and heavy for pure dates
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
        IMeasuringSerializer<DateOnly>, IMeasuringSerializer<DateOnly?>,

        ISerializer<TimeOnly>, ISerializer<TimeOnly?> 
    {
        SerializerFeatures ISerializer<DateOnly>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
        SerializerFeatures ISerializer<DateOnly?>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;

        SerializerFeatures ISerializer<TimeOnly>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessageWrappedAtRoot;
        SerializerFeatures ISerializer<TimeOnly?>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessageWrappedAtRoot;

        TimeOnly? ISerializer<TimeOnly?>.Read(ref ProtoReader.State state, TimeOnly? value)
            => ((ISerializer<TimeOnly>)this).Read(ref state, value.GetValueOrDefault());
        void ISerializer<TimeOnly?>.Write(ref ProtoWriter.State state, TimeOnly? value)
            => ((ISerializer<TimeOnly>)this).Write(ref state, value.Value);

        TimeOnly ISerializer<TimeOnly>.Read(ref ProtoReader.State state, TimeOnly value)
            => new TimeOnly(ReadDuration(ref state, new Duration(value.Ticks)).ToTicks());

        void ISerializer<TimeOnly>.Write(ref ProtoWriter.State state, TimeOnly value)
            => WriteDuration(ref state, new Duration(value.Ticks));

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

        static int MeasureDays(int dayNumber) => dayNumber < 0 ? 10 : ProtoWriter.MeasureUInt32((uint)dayNumber);
        int IMeasuringSerializer<DateOnly>.Measure(ISerializationContext context, WireType wireType, DateOnly value)
            => MeasureDays(value.DayNumber);

        int IMeasuringSerializer<DateOnly?>.Measure(ISerializationContext context, WireType wireType, DateOnly? value)
            => MeasureDays(value.Value.DayNumber);
    }
}
#endif