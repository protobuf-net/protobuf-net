using NodaTime;
using ProtoBuf.Serializers;
using System;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// Provides custom serialization capabilities for protobuf-net; this type is not intended to be used directly and should be
    /// considered an implementation detail
    /// </summary>
    public sealed class NodaTimeSerializers // note: it needs to be public to be usable from fully compiled models
        : ISerializer<LocalTime>, ISerializer<LocalTime?> // treated as Google.Type.TimeOfDay
        , ISerializer<LocalDate>, ISerializer<LocalDate?> // treated as Google.Type.Date
    {
        private const SerializerFeatures Message = SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;
        SerializerFeatures ISerializer<LocalDate>.Features => Message;
        SerializerFeatures ISerializer<LocalDate?>.Features => Message;
        SerializerFeatures ISerializer<LocalTime>.Features => Message;
        SerializerFeatures ISerializer<LocalTime?>.Features => Message;

        LocalDate ISerializer<LocalDate>.Read(ref ProtoReader.State state, LocalDate value)
        {
            int field;
            int year = value.Year, month = value.Month, day = value.Day;
            while ((field = state.ReadFieldHeader()) > 0)
            {
                switch (field)
                {
                    case 1:
                        year = state.ReadInt32();
                        break;
                    case 2:
                        month = state.ReadInt32();
                        break;
                    case 3:
                        day = state.ReadInt32();
                        break;
                    default:
                        state.SkipField();
                        break;
                }
            }
            return new LocalDate(year, month, day); // ISO calendar is implicit
        }

        LocalTime ISerializer<LocalTime>.Read(ref ProtoReader.State state, LocalTime value)
        {
            int field;
            int hours = value.Hour, minutes = value.Minute, seconds = value.Second, nanos = value.NanosecondOfSecond;
            while ((field = state.ReadFieldHeader()) > 0)
            {
                switch(field)
                {
                    case 1:
                        hours = state.ReadInt32();
                        break;
                    case 2:
                        minutes = state.ReadInt32();
                        break;
                    case 3:
                        seconds = state.ReadInt32();
                        break;
                    case 4:
                        nanos = state.ReadInt32();
                        break;
                    default:
                        state.SkipField();
                        break;
                }
            }
            return new LocalTime(hours, minutes, seconds).PlusNanoseconds(nanos);
        }

        void ISerializer<LocalDate>.Write(ref ProtoWriter.State state, LocalDate value)
        {
            if (value.Calendar != CalendarSystem.Iso)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Non-ISO dates cannot be converted to Protobuf Date messages. Actual calendar ID: {value.Calendar.Id}");
            }
            if (value.Year < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value),
                    $"Dates earlier than 1AD cannot be converted to Protobuf Date messages. Year: {value.Year}");
            }
            WriteNonZeroInt32(ref state, 1, value.Year);
            WriteNonZeroInt32(ref state, 2, value.Month);
            WriteNonZeroInt32(ref state, 3, value.Day);
        }

        void ISerializer<LocalTime>.Write(ref ProtoWriter.State state, LocalTime value)
        {
            WriteNonZeroInt32(ref state, 1, value.Hour);
            WriteNonZeroInt32(ref state, 2, value.Minute);
            WriteNonZeroInt32(ref state, 3, value.Second);
            WriteNonZeroInt32(ref state, 4, value.NanosecondOfSecond);
        }

        static void WriteNonZeroInt32(ref ProtoWriter.State state, int field, int value)
        {   // since Proto3 doesn't write zeros by default, neither will we
            if (value != 0)
            {
                state.WriteFieldHeader(field, WireType.Varint);
                state.WriteInt32(value);
            }
        }

        LocalDate? ISerializer<LocalDate?>.Read(ref ProtoReader.State state, LocalDate? value)
            => ((ISerializer<LocalDate>)this).Read(ref state, value.GetValueOrDefault());

        void ISerializer<LocalDate?>.Write(ref ProtoWriter.State state, LocalDate? value)
            => ((ISerializer<LocalDate>)this).Write(ref state, value.Value);

        LocalTime? ISerializer<LocalTime?>.Read(ref ProtoReader.State state, LocalTime? value)
            => ((ISerializer<LocalTime>)this).Read(ref state, value.GetValueOrDefault());

        void ISerializer<LocalTime?>.Write(ref ProtoWriter.State state, LocalTime? value)
            => ((ISerializer<LocalTime>)this).Write(ref state, value.Value);
    }
}
