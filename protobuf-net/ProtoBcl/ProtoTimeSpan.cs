using System;
using System.Collections.Generic;
using System.Text;

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

        private static readonly DateTime epoch = new DateTime(1970, 1, 1);
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
                delta = value - epoch;
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
                default: return epoch.AddTicks(ticks);
            }
        }
        private static long DeserializeTicks(SerializationContext context)
        {
            long value = 0;
            TimeSpanScale scale = TimeSpanScale.Days;

            uint prefix;
            bool keepRunning = true;
            while (keepRunning && (prefix = context.TryReadFieldPrefix()) > 0)
            {
                switch (prefix)
                {
                    case (FieldTimeSpanScale << 3) | (int)WireType.Variant:
                        scale = (TimeSpanScale)Base128Variant.DecodeInt32(context);
                        break;
                    case (FieldTimeSpanValue << 3) | (int)WireType.Variant:
                        value = Base128Variant.Zag(Base128Variant.DecodeUInt64(context));
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
                case TimeSpanScale.Milliseconds:
                    return value * TimeSpan.TicksPerMillisecond;
                case TimeSpanScale.Minutes:
                    return value * TimeSpan.TicksPerMinute;
                case TimeSpanScale.Seconds:
                    return value * TimeSpan.TicksPerSecond;
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

            MinMax = 15
        }
        internal static int SerializeTimeSpan(TimeSpan timeSpan, SerializationContext context, bool lengthPrefixed)
        {
            TimeSpanScale scale;
            long value;

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
            else if (timeSpan.Milliseconds != 0)
            {
                scale = TimeSpanScale.Milliseconds;
                value = timeSpan.Ticks / TimeSpan.TicksPerMillisecond;
            }
            else if (timeSpan.Seconds != 0)
            {
                scale = TimeSpanScale.Seconds;
                value = timeSpan.Ticks / TimeSpan.TicksPerSecond;
            }
            else if (timeSpan.Minutes != 0)
            {
                scale = TimeSpanScale.Minutes;
                value = timeSpan.Ticks / TimeSpan.TicksPerMinute;
            }
            else if (timeSpan.Hours != 0)
            {
                scale = TimeSpanScale.Hours;
                value = timeSpan.Ticks / TimeSpan.TicksPerHour;
            }
            else
            {
                scale = TimeSpanScale.Days;
                value = timeSpan.Ticks / TimeSpan.TicksPerDay;
            }

            int len = 0;
            ulong zig = Base128Variant.Zig(value);
            if (lengthPrefixed)
            {
                if (scale != TimeSpanScale.Days)
                {
                    len += 2;
                }
                if (zig != 0)
                {
                    len += 1 + Base128Variant.GetLength(zig);
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
