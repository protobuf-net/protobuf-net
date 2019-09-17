using System;

namespace ProtoBuf.WellKnownTypes
{
    [ProtoContract(Name = ".bcl.TimeSpan")]
    internal readonly struct ScaledTicks
    {
        [ProtoMember(1, DataFormat = DataFormat.ZigZag, Name = "value")]
        public long Value { get; }
        [ProtoMember(2, Name = "scale")]
        public TimeSpanScale Scale { get; }
        [ProtoMember(3, Name = "kind")]
        public DateTimeKind Kind { get; }
        public ScaledTicks(long value, TimeSpanScale scale, DateTimeKind kind)
        {
            Value = value;
            Scale = scale;
            Kind = kind;
        }

        public ScaledTicks(TimeSpan timeSpan, DateTimeKind kind)
        {
            TimeSpanScale scale;
            long value = timeSpan.Ticks;
            if (timeSpan == TimeSpan.MaxValue)
            {
                value = 1;
                scale = TimeSpanScale.MinMax;
            }
            else if (timeSpan == TimeSpan.MinValue)
            {
                value = -1;
                scale = TimeSpanScale.MinMax;
            }
            else if (value % TimeSpan.TicksPerDay == 0)
            {
                scale = TimeSpanScale.Days;
                value /= TimeSpan.TicksPerDay;
            }
            else if (value % TimeSpan.TicksPerHour == 0)
            {
                scale = TimeSpanScale.Hours;
                value /= TimeSpan.TicksPerHour;
            }
            else if (value % TimeSpan.TicksPerMinute == 0)
            {
                scale = TimeSpanScale.Minutes;
                value /= TimeSpan.TicksPerMinute;
            }
            else if (value % TimeSpan.TicksPerSecond == 0)
            {
                scale = TimeSpanScale.Seconds;
                value /= TimeSpan.TicksPerSecond;
            }
            else if (value % TimeSpan.TicksPerMillisecond == 0)
            {
                scale = TimeSpanScale.Milliseconds;
                value /= TimeSpan.TicksPerMillisecond;
            }
            else
            {
                scale = TimeSpanScale.Ticks;
            }

            Kind = kind;
            Value = value;
            Scale = scale;
        }

        public long ToTicks()
        {
            switch (Scale)
            {
                case TimeSpanScale.Days:
                    return Value * TimeSpan.TicksPerDay;
                case TimeSpanScale.Hours:
                    return Value * TimeSpan.TicksPerHour;
                case TimeSpanScale.Minutes:
                    return Value * TimeSpan.TicksPerMinute;
                case TimeSpanScale.Seconds:
                    return Value * TimeSpan.TicksPerSecond;
                case TimeSpanScale.Milliseconds:
                    return Value * TimeSpan.TicksPerMillisecond;
                case TimeSpanScale.Ticks:
                    return Value;
                case TimeSpanScale.MinMax:
                    switch (Value)
                    {
                        case 1: return long.MaxValue;
                        case -1: return long.MinValue;
                        default: throw new ProtoException("Unknown min/max value: " + Value.ToString());
                    }
                default:
                    throw new ProtoException("Unknown timescale: " + Scale.ToString());
            }
        }
    }

    partial class WellKnownSerializer : IProtoSerializer<ScaledTicks>
    {
        ScaledTicks IProtoSerializer<ScaledTicks>.Read(ProtoReader reader, ref ProtoReader.State state, ScaledTicks _)
        {
            int fieldNumber;
            TimeSpanScale scale = TimeSpanScale.Days;
            long value = 0;
            var kind = DateTimeKind.Unspecified;
            while ((fieldNumber = reader.ReadFieldHeader(ref state)) > 0)
            {
                switch (fieldNumber)
                {
                    case FieldTimeSpanScale:
                        scale = (TimeSpanScale)reader.ReadInt32(ref state);
                        break;
                    case FieldTimeSpanValue:
                        reader.Assert(ref state, WireType.SignedVariant);
                        value = reader.ReadInt64(ref state);
                        break;
                    case FieldTimeSpanKind:
                        kind = (DateTimeKind)reader.ReadInt32(ref state);
                        switch (kind)
                        {
                            case DateTimeKind.Unspecified:
                            case DateTimeKind.Utc:
                            case DateTimeKind.Local:
                                break; // fine
                            default:
                                throw new ProtoException("Invalid date/time kind: " + kind.ToString());
                        }
                        break;
                    default:
                        reader.SkipField(ref state);
                        break;
                }
            }
            return new ScaledTicks(value, scale, kind);
        }

        private const int FieldTimeSpanValue = 0x01, FieldTimeSpanScale = 0x02, FieldTimeSpanKind = 0x03;


        void IProtoSerializer<ScaledTicks>.Write(ProtoWriter writer, ref ProtoWriter.State state, ScaledTicks value)
        {
            if (value.Value != 0)
            {
                ProtoWriter.WriteFieldHeader(FieldTimeSpanValue, WireType.SignedVariant, writer, ref state);
                ProtoWriter.WriteInt64(value.Value, writer, ref state);
            }
            if (value.Scale != TimeSpanScale.Days)
            {
                ProtoWriter.WriteFieldHeader(FieldTimeSpanScale, WireType.Variant, writer, ref state);
                ProtoWriter.WriteInt32((int)value.Scale, writer, ref state);
            }
            if (value.Kind != DateTimeKind.Unspecified)
            {
                ProtoWriter.WriteFieldHeader(FieldTimeSpanKind, WireType.Variant, writer, ref state);
                ProtoWriter.WriteInt32((int)value.Kind, writer, ref state);
            }
        }
    }
}
