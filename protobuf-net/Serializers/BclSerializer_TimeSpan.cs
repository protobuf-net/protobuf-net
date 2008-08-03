using System;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf.Serializers
{
    internal sealed partial class BclSerializer : ISerializer<TimeSpan>, ILengthSerializer<TimeSpan>
    {

        const int FieldTimeSpanValue = 0x01, FieldTimeSpanScale = 0x02;
        long DeserializeTicks(SerializationContext context)
        {
            long value = 0;
            TimeSpanScale scale = TimeSpanScale.Days;

            int prefix;
            bool keepRunning = true;
            while (keepRunning && context.IsDataAvailable && TwosComplementSerializer.TryReadInt32(context, out prefix))
            {
                switch (prefix)
                {
                    case (FieldTimeSpanScale << 3) | (int)WireType.Variant:
                        scale = (TimeSpanScale)TwosComplementSerializer.ReadInt32(context);
                        break;
                    case (FieldTimeSpanValue << 3) | (int)WireType.Variant:
                        value = ZigZagSerializer.ReadInt64(context);
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

        TimeSpan ISerializer<TimeSpan>.Deserialize(TimeSpan value, SerializationContext context)
        {
            long ticks = DeserializeTicks(context);
            switch (ticks)
            {
                case long.MaxValue: return TimeSpan.MaxValue;
                case long.MinValue: return TimeSpan.MinValue;
                default: return TimeSpan.FromTicks(ticks);
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
        static int SerializeTicks(TimeSpan timeSpan, SerializationContext context)
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
            if (value != 0)
            {
                context.WriteByte((FieldTimeSpanValue << 3) | (int)WireType.Variant);
                len += 1 + ZigZagSerializer.WriteToStream(value, context);
            }
            if (scale != TimeSpanScale.Days)
            {
                context.WriteByte((FieldTimeSpanScale << 3) | (int)WireType.Variant);
                len += 1 + TwosComplementSerializer.WriteToStream((int)scale, context);
            }
            return len;
        }

        int ISerializer<TimeSpan>.Serialize(TimeSpan value, SerializationContext context)
        {
            return value == TimeSpan.Zero ? 0 : SerializeTicks(value, context);
        }
        int ILengthSerializer<TimeSpan>.UnderestimateLength(TimeSpan value)
        {
            return 0;
        }
        
        string ISerializer<TimeSpan>.DefinedType
        {
            get { return "Bcl.TimeSpan"; }
        }
    }
}
