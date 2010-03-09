
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

            SubItemToken token = ProtoWriter.StartSubItem(null, dest);
            
            if(value != 0) {
                ProtoWriter.WriteFieldHeader(FieldTimeSpanValue, WireType.SignedVariant, dest);
                ProtoWriter.WriteInt64(value, dest);
            }
            if(scale != TimeSpanScale.Days) {
                ProtoWriter.WriteFieldHeader(FieldTimeSpanScale, WireType.Variant, dest);
                ProtoWriter.WriteInt32((int)scale, dest);
            }
            ProtoWriter.EndSubItem(token, dest);
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
            SubItemToken token = ProtoReader.StartSubItem(source);
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
            ProtoReader.EndSubItem(token,source);
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
            SubItemToken token = ProtoReader.StartSubItem(reader);
            while ((fieldNumber = reader.ReadFieldHeader()) > 0)
            {
                switch (fieldNumber)
                {
                    case FieldDecimalLow: low = reader.ReadUInt64(); break;
                    case FieldDecimalHigh: high = reader.ReadUInt32(); break;
                    case FieldDecimalSignScale: signScale = reader.ReadUInt32(); break;
                    default: reader.SkipField(); break;
                }
                
            }
            ProtoReader.EndSubItem(token, reader);

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

            SubItemToken token = ProtoWriter.StartSubItem(null, writer);
            if (low != 0) {
                ProtoWriter.WriteFieldHeader(FieldDecimalLow, WireType.Variant, writer);
                ProtoWriter.WriteUInt64(low, writer);
            }
            if (high != 0)
            {
                ProtoWriter.WriteFieldHeader(FieldDecimalHigh, WireType.Variant, writer);
                ProtoWriter.WriteUInt32(high, writer);
            }
            if (signScale != 0)
            {
                ProtoWriter.WriteFieldHeader(FieldDecimalSignScale, WireType.Variant, writer);
                ProtoWriter.WriteUInt32(signScale, writer);
            }
            ProtoWriter.EndSubItem(token, writer);
        }

        const int FieldGuidLow = 1, FieldGuidHigh = 2;
        public static void WriteGuid(Guid value, ProtoWriter dest)
        {
            byte[] blob = value.ToByteArray();

            SubItemToken token = ProtoWriter.StartSubItem(null, dest);
            if (value != Guid.Empty)
            {
                ProtoWriter.WriteFieldHeader(FieldGuidLow, WireType.Fixed64, dest);
                ProtoWriter.WriteBytes(blob, 0, 8, dest);
                ProtoWriter.WriteFieldHeader(FieldGuidHigh, WireType.Fixed64, dest);
                ProtoWriter.WriteBytes(blob, 8, 8, dest);
            }
            ProtoWriter.EndSubItem(token, dest);
        }
        public static Guid ReadGuid(ProtoReader source)
        {
            ulong low = 0, high = 0;
            int fieldNumber;
            SubItemToken token = ProtoReader.StartSubItem(source);
            while ((fieldNumber = source.ReadFieldHeader()) > 0)
            {
                switch (fieldNumber)
                {
                    case FieldGuidLow: low = source.ReadUInt64(); break;
                    case FieldGuidHigh: high = source.ReadUInt64(); break;
                    default: source.SkipField(); break;
                }
            }
            ProtoReader.EndSubItem(token, source);
            if(low == 0 && high == 0) return Guid.Empty;
            uint a = (uint)(low >> 32), b = (uint)low, c = (uint)(high >> 32), d= (uint)high;
            return new Guid(b, (ushort)a, (ushort)(a >> 16), 
                (byte)d, (byte)(d >> 8), (byte)(d >> 16), (byte)(d >> 24),
                (byte)c, (byte)(c >> 8), (byte)(c >> 16), (byte)(c >> 24));
            
        }
    }
}
