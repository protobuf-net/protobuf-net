using System;

namespace ProtoBuf.ProtoBcl
{
    internal static class ProtoTimeSpan
    {
        const int FieldTimeSpanValue = 0x01, FieldTimeSpanScale = 0x02;

        public static TimeSpan DeserializeTimeSpan(SerializationContext context)
        {
            long ticks = DeserializeTicks(context);
            switch (ticks)
            {
                case long.MaxValue: return TimeSpan.MaxValue;
                case long.MinValue: return TimeSpan.MinValue;
                default: return TimeSpan.FromTicks(ticks);
            }
        }

        internal static readonly DateTime EpochOrigin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);
        public static int SerializeDateTime(DateTime value, SerializationContext context, bool lengthPrefixed)
        {
            TimeSpan delta;
            if (value == DateTime.MaxValue)
            {
                delta = TimeSpan.MaxValue;
            }
            else if (value == DateTime.MinValue)
            {
                delta = TimeSpan.MinValue;
            }
            else
            {
                delta = value - EpochOrigin;
            }

            return SerializeTimeSpan(delta, context, lengthPrefixed);
        }
        public static DateTime DeserializeDateTime(SerializationContext context)
        {
            long ticks = DeserializeTicks(context);
            switch (ticks)
            {
                case long.MaxValue: return DateTime.MaxValue;
                case long.MinValue: return DateTime.MinValue;
                default: return EpochOrigin.AddTicks(ticks);
            }
        }
        private static long DeserializeTicks(SerializationContext context)
        {
            long value = 0;
            TimeSpanScale scale = TimeSpanScale.Days;

            uint prefix;
            bool keepRunning = true;
            while (keepRunning && context.TryReadFieldPrefix(out prefix))
            {
                switch (prefix)
                {
                    case (FieldTimeSpanScale << 3) | (int)WireType.Variant:
                        scale = (TimeSpanScale)context.DecodeInt32();
                        break;
                    case (FieldTimeSpanValue << 3) | (int)WireType.Variant:
                        value = SerializationContext.Zag(context.DecodeUInt64());
                        break;
                    default:
                        WireType wireType;
                        int fieldTag;
                        Serializer.ParseFieldToken(prefix, out wireType, out fieldTag);
                        if (wireType == WireType.EndGroup)
                        {
                            context.EndGroup(fieldTag);
                            keepRunning = false;
                            continue;
                        }
                        switch (fieldTag)
                        {
                            case FieldTimeSpanScale:
                            case FieldTimeSpanValue:
                                throw new ProtoException("Incorrect wire-type deserializing TimeSpan");
                            default:
                                Serializer.SkipData(context, fieldTag, wireType);
                                break;
                        }
                        break;
                }
            }

            switch (scale)
            {
                case TimeSpanScale.Days:
                    return value * TimeSpan.TicksPerDay;
                case TimeSpanScale.Hours:
                    return value * TimeSpan.TicksPerHour;
                case TimeSpanScale.Minutes:
                    return value * TimeSpan.TicksPerMinute;
                case TimeSpanScale.Seconds:
                    return value * TimeSpan.TicksPerSecond;
                case TimeSpanScale.Milliseconds:
                    return value * TimeSpan.TicksPerMillisecond;
                case TimeSpanScale.Ticks:
                    return value;
                case TimeSpanScale.MinMax:
                    switch (value)
                    {
                        case 1: return long.MaxValue;
                        case -1: return long.MinValue;
                        default: throw new ProtoException("Unknown min/max value: " + value.ToString());
                    }
                default:
                    throw new ProtoException("Unknown timescale: " + scale.ToString());
            }
        }
        private enum TimeSpanScale
        {
            Days = 0,
            Hours = 1,
            Minutes = 2,
            Seconds = 3,
            Milliseconds = 4,
            Ticks = 5,

            MinMax = 15
        }
        internal static int SerializeTimeSpan(TimeSpan timeSpan, SerializationContext context, bool lengthPrefixed)
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

            int len = 0;
            ulong zig = SerializationContext.Zig(value);
            if (lengthPrefixed)
            {
                if (scale != TimeSpanScale.Days)
                {
                    len += 2;
                }
                if (zig != 0)
                {
                    len += 1 + SerializationContext.GetLength(zig);
                }
                context.WriteByte((byte)len);
                len = 1;
            }
            if (value != 0)
            {
                context.WriteByte((FieldTimeSpanValue << 3) | (int)WireType.Variant);
                len += 1 + context.EncodeUInt64(zig);
            }
            if (scale != TimeSpanScale.Days)
            {
                context.WriteByte((FieldTimeSpanScale << 3) | (int)WireType.Variant);
                len += 1 + context.EncodeInt32((int)scale);
            }
            return len;
        }
    }
}
