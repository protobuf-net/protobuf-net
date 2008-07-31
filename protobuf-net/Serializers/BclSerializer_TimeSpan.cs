using System;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf.Serializers
{
    internal sealed partial class BclSerializer : ISerializer<TimeSpan>, IGroupSerializer<TimeSpan>
    {
        long ReadTimeSpanTicks(SerializationContext context)
        {
            ProtoTimeSpan span = context.TimeSpanTemplate;
            span.Reset();
            ProtoTimeSpan.Serializer.Deserialize(span, context);
            return ReadTimeSpanTicks(span);
        }
        long ReadTimeSpanTicksGroup(SerializationContext context)
        {
            ProtoTimeSpan span = context.TimeSpanTemplate;
            span.Reset();
            ProtoTimeSpan.Serializer.DeserializeGroup(span, context);
            return ReadTimeSpanTicks(span);
        }
        long ReadTimeSpanTicks(ProtoTimeSpan span) {
            if (span.Value == 0) return 0;
            switch (span.Scale)
            {
                case ProtoTimeSpan.ProtoTimeSpanScale.Days:
                    return span.Value * TimeSpan.TicksPerDay;
                case ProtoTimeSpan.ProtoTimeSpanScale.Hours:
                    return span.Value * TimeSpan.TicksPerHour;
                case ProtoTimeSpan.ProtoTimeSpanScale.Milliseconds:
                    return span.Value * TimeSpan.TicksPerMillisecond;
                case ProtoTimeSpan.ProtoTimeSpanScale.Minutes:
                    return span.Value * TimeSpan.TicksPerMinute;
                case ProtoTimeSpan.ProtoTimeSpanScale.Seconds:
                    return span.Value * TimeSpan.TicksPerSecond;
                case ProtoTimeSpan.ProtoTimeSpanScale.MinMax:
                    switch (span.Value)
                    {
                        case 1: return long.MaxValue;
                        case -1: return long.MinValue;
                        default: throw new ProtoException("Unknown min/max value: " + span.Value.ToString());
                    }
                default:
                    throw new ProtoException("Unknown timescale: " + span.Scale.ToString());
            }
        }

        TimeSpan ISerializer<TimeSpan>.Deserialize(TimeSpan value, SerializationContext context)
        {
            long ticks = ReadTimeSpanTicks(context);
            switch (ticks)
            {
                case long.MaxValue: return TimeSpan.MaxValue;
                case long.MinValue: return TimeSpan.MinValue;
                default: return TimeSpan.FromTicks(ticks);
            }
        }
        TimeSpan IGroupSerializer<TimeSpan>.DeserializeGroup(TimeSpan value, SerializationContext context)
        {
            long ticks = ReadTimeSpanTicksGroup(context);
            switch (ticks)
            {
                case long.MaxValue: return TimeSpan.MaxValue;
                case long.MinValue: return TimeSpan.MinValue;
                default: return TimeSpan.FromTicks(ticks);
            }
        }

        static void PrepareTimeSpan(TimeSpan value, ProtoTimeSpan span)
        {
            if (value == TimeSpan.Zero)
            {
                span.Reset();
            }
            else if (value == TimeSpan.MaxValue)
            {
                span.Value = 1;
                span.Scale = ProtoTimeSpan.ProtoTimeSpanScale.MinMax;
            }
            else if (value == TimeSpan.MinValue)
            {
                span.Value = -1;
                span.Scale = ProtoTimeSpan.ProtoTimeSpanScale.MinMax;
            }
            else if (value.Milliseconds != 0)
            {
                span.Scale = ProtoTimeSpan.ProtoTimeSpanScale.Milliseconds;
                span.Value = value.Ticks / TimeSpan.TicksPerMillisecond;
            }
            else if (value.Seconds != 0)
            {
                span.Scale = ProtoTimeSpan.ProtoTimeSpanScale.Seconds;
                span.Value = value.Ticks / TimeSpan.TicksPerSecond;
            }
            else if (value.Minutes != 0)
            {
                span.Scale = ProtoTimeSpan.ProtoTimeSpanScale.Minutes;
                span.Value = value.Ticks / TimeSpan.TicksPerMinute;
            }
            else if (value.Hours != 0)
            {
                span.Scale = ProtoTimeSpan.ProtoTimeSpanScale.Hours;
                span.Value = value.Ticks / TimeSpan.TicksPerHour;
            }
            else
            {
                span.Scale = ProtoTimeSpan.ProtoTimeSpanScale.Days;
                span.Value = value.Ticks / TimeSpan.TicksPerDay;
            }
        }

        int ISerializer<TimeSpan>.Serialize(TimeSpan value, SerializationContext context)
        {
            if (value == TimeSpan.Zero)
            {
                context.Stream.WriteByte(0);
                return 1;
            }
            ProtoTimeSpan ts = context.TimeSpanTemplate;
            PrepareTimeSpan(value, ts);
            return Serialize(ts, context);
        }
        public static int Serialize(ProtoTimeSpan ts, SerializationContext context) 
        {
            int expected = GetLengthCore(ts);
            // write message-length prefix (expect single-byte!)
            context.Stream.WriteByte((byte)(expected));
            int actual = SerializeCore(ts, context);
            Serializer.VerifyBytesWritten(expected, actual);
            return actual + 1;
        }
        int IGroupSerializer<TimeSpan>.SerializeGroup(TimeSpan value, SerializationContext context)
        {
            if (value == TimeSpan.Zero) return 0;
            PrepareTimeSpan(value, context.TimeSpanTemplate);
            return SerializeCore(context.TimeSpanTemplate, context);
        }
        int ISerializer<TimeSpan>.GetLength(TimeSpan value, SerializationContext context)
        {
            return 1 + GetLengthGroup(value, context);
        }
        public int GetLengthGroup(TimeSpan value, SerializationContext context)
        {
            if (value == TimeSpan.Zero)
            {
                return 0;
            }
            PrepareTimeSpan(value, context.TimeSpanTemplate);
            return GetLengthCore(context.TimeSpanTemplate);
        }

        string ISerializer<TimeSpan>.DefinedType
        {
            get { return ProtoTimeSpan.Serializer.DefinedType; }
        }

        static int GetLengthCore(ProtoTimeSpan value)
        {
            int len = 0;
            if (value.Value != 0)
            {
                len += 1 + ZigZagSerializer.GetLength(value.Value);
            }
            if (value.Scale != ProtoTimeSpan.ProtoTimeSpanScale.Days)
            {
                len += 2; // assume scale always single-byte
            }
            return len;
        }

        static int SerializeCore(ProtoTimeSpan value, SerializationContext context) {
            int actual = 0;
            if (value.Value != 0)
            {
                context.Stream.WriteByte((0x01 << 3) | (int)WireType.Variant);
                actual += 1 + ZigZagSerializer.WriteToStream(value.Value, context);
            }
            if (value.Scale != ProtoTimeSpan.ProtoTimeSpanScale.Days)
            {
                context.Stream.WriteByte((0x02 << 3) | (int)WireType.Variant);
                actual += 1 + TwosComplementSerializer.WriteToStream((int)value.Scale, context);
            }            
            return actual;
        }


    }
}
