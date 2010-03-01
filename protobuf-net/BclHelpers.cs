
using System;

namespace ProtoBuf
{
    internal enum TimeSpanScale
    {
        Days = 0,
        Hours = 1,
        Minutes = 2,
        Seconds = 3,
        Milliseconds = 4,
        Ticks = 5,

        MinMax = 15
    }

    public class BclHelpers
    {

        private BclHelpers() { } // not a static class for C# 1.2 reasons
        const int FieldTimeSpanValue = 0x01, FieldTimeSpanScale = 0x02;
        
        internal static readonly DateTime EpochOrigin = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        static readonly object dummy = new object();

        public static void WriteTimeSpan(TimeSpan timeSpan, ProtoWriter dest)
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

            int token = dest.StartSubItem(dummy);
            
            if(value != 0) {
                dest.WriteFieldHeader(FieldTimeSpanValue, WireType.SignedVariant);
                dest.WriteInt64(value);            
            }
            if(scale != TimeSpanScale.Days) {
                dest.WriteFieldHeader(FieldTimeSpanScale, WireType.Variant);
                dest.WriteInt32((int)scale);
            }
            dest.EndSubItem(token);
        }
        public static TimeSpan ReadTimeSpan(ProtoReader source)
        {
            long ticks = ReadTimeSpanTicks(source);
            if (ticks == long.MinValue) { return TimeSpan.MinValue; }
            if (ticks == long.MaxValue) { return TimeSpan.MaxValue; }
            return TimeSpan.FromTicks(ticks);
        }
        public static DateTime ReadDateTime(ProtoReader source)
        {
            long ticks = ReadTimeSpanTicks(source);
            if (ticks == long.MinValue) { return DateTime.MinValue; }
            if (ticks == long.MaxValue) { return DateTime.MaxValue; }
            return EpochOrigin.AddTicks(ticks);
        }

        public static void WriteDateTime(DateTime value, ProtoWriter dest)
        {
            TimeSpan delta;
            if (value == DateTime.MaxValue) {
                delta = TimeSpan.MaxValue;
            } else if (value == DateTime.MinValue) {
                delta = TimeSpan.MinValue;
            } else {
                delta = value - EpochOrigin;
            }
            WriteTimeSpan(delta, dest);
        }

        private static long ReadTimeSpanTicks(ProtoReader source) {
            int token = source.StartSubItem();
            int fieldNumber;
            TimeSpanScale scale = TimeSpanScale.Days;
            long value = 0;
            while((fieldNumber = source.ReadFieldHeader()) > 0) {
                switch(fieldNumber) {
                    case FieldTimeSpanScale:
                        scale = (TimeSpanScale)source.ReadInt32();
                        break;
                    case FieldTimeSpanValue:
                        source.SetSignedVariant();
                        value = source.ReadInt64();
                        break;
                    default:
                        source.SkipField();
                        break;
                }
            }
            source.EndSubItem(token);
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

        const int FieldDecimalLow = 0x01, FieldDecimalHigh = 0x02, FieldDecimalSignScale = 0x03;

        public static decimal ReadDecimal(ProtoReader reader)
        {
            ulong low = 0;
            uint high = 0;
            uint signScale = 0;

            int fieldNumber;
            int token = reader.StartSubItem();
            while ((fieldNumber = reader.ReadFieldHeader()) > 0)
            {
                switch (fieldNumber)
                {
                    case FieldDecimalLow:
                        low = reader.ReadUInt64();
                        break;
                    case FieldDecimalHigh:
                        high = reader.ReadUInt32();
                        break;
                    case FieldDecimalSignScale:
                        signScale = reader.ReadUInt32();
                        break;
                    default:
                        reader.SkipField();
                        break;
                }
                
            }
            reader.EndSubItem(token);

            if (low == 0 && high == 0) return decimal.Zero;

            int lo = (int)(low & 0xFFFFFFFFL),
                mid = (int)((low >> 32) & 0xFFFFFFFFL),
                hi = (int)high;
            bool isNeg = (signScale & 0x0001) == 0x0001;
            byte scale = (byte)((signScale & 0x01FE) >> 1);
            return new decimal(lo, mid, hi, isNeg, scale);
        }

        public static void WriteDecimal(decimal value, ProtoWriter writer)
        {
            int[] bits = decimal.GetBits(value);
            ulong a = ((ulong)bits[1]) << 32, b = ((ulong)bits[0]) & 0xFFFFFFFFL;
            ulong low = a | b;
            uint high = (uint)bits[2];
            uint signScale = (uint)(((bits[3] >> 15) & 0x01FE) | ((bits[3] >> 31) & 0x0001));

            int token = writer.StartSubItem(dummy);
            if (low != 0) {
                writer.WriteFieldHeader(FieldDecimalLow, WireType.Variant);
                writer.WriteUInt64(low);
            }
            if (high != 0)
            {
                writer.WriteFieldHeader(FieldDecimalHigh, WireType.Variant);
                writer.WriteUInt32(high);
            }
            if (signScale != 0)
            {
                writer.WriteFieldHeader(FieldDecimalSignScale, WireType.Variant);
                writer.WriteUInt32(signScale);
            }
            writer.EndSubItem(token);
        }
    }
}
