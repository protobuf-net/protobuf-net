using ProtoBuf.Internal;
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
                        default:
                            ThrowHelper.ThrowProtoException("Unknown min/max value: " + Value.ToString());
                            return default;
                    }
                default:
                    ThrowHelper.ThrowProtoException("Unknown timescale: " + Scale.ToString());
                    return default;
            }
        }
    }

    partial class WellKnownSerializer : IProtoSerializer<ScaledTicks>
    {
        ScaledTicks IProtoSerializer<ScaledTicks>.Read(ref ProtoReader.State state, ScaledTicks _)
        {
            int fieldNumber;
            TimeSpanScale scale = TimeSpanScale.Days;
            long value = 0;
            var kind = DateTimeKind.Unspecified;
            while ((fieldNumber = state.ReadFieldHeader()) > 0)
            {
                switch (fieldNumber)
                {
                    case FieldTimeSpanScale:
                        scale = (TimeSpanScale)state.ReadInt32();
                        break;
                    case FieldTimeSpanValue:
                        state.Assert(WireType.SignedVarint);
                        value = state.ReadInt64();
                        break;
                    case FieldTimeSpanKind:
                        kind = (DateTimeKind)state.ReadInt32();
                        switch (kind)
                        {
                            case DateTimeKind.Unspecified:
                            case DateTimeKind.Utc:
                            case DateTimeKind.Local:
                                break; // fine
                            default:
                                ThrowHelper.ThrowProtoException("Invalid date/time kind: " + kind.ToString());
                                break;
                        }
                        break;
                    default:
                        state.SkipField();
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
                state.WriteFieldHeader(FieldTimeSpanValue, WireType.SignedVarint);
                ProtoWriter.WriteInt64(value.Value, writer, ref state);
            }
            if (value.Scale != TimeSpanScale.Days)
            {
                state.WriteFieldHeader(FieldTimeSpanScale, WireType.Varint);
                ProtoWriter.WriteInt32((int)value.Scale, writer, ref state);
            }
            if (value.Kind != DateTimeKind.Unspecified)
            {
                state.WriteFieldHeader(FieldTimeSpanKind, WireType.Varint);
                ProtoWriter.WriteInt32((int)value.Kind, writer, ref state);
            }
        }
    }
}
