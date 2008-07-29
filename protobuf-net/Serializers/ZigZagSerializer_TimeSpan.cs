//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.IO;

//namespace ProtoBuf
//{
//    internal partial class ZigZagSerializer : ISerializer<TimeSpan>
//    {

//        string ISerializer<TimeSpan>.DefinedType { get { return ProtoFormat.SINT64; } }

//        int ISerializer<TimeSpan>.GetLength(TimeSpan value, SerializationContext context)
//        {
//            return ZigZagSerializer.GetLength(EncodeTimeSpan(value));
//        }
//        int ISerializer<TimeSpan>.Serialize(TimeSpan value, SerializationContext context)
//        {
//            return ZigZagSerializer.WriteToStream(EncodeTimeSpan(value), context);
//        }
//        TimeSpan ISerializer<TimeSpan>.Deserialize(TimeSpan oldValue, SerializationContext context)
//        {
//            long ticks = DecodeTimeSpanTicks(context);
//            return TimeSpan.FromTicks(ticks);
//        }

//        internal enum DateTimeScale
//        {
//            Days = 0,
//            Seconds = 1,
//            Milliseconds = 2
//        }

//        public static long DecodeTimeSpanTicks(SerializationContext context)
//        {
//            long value = ZigZagSerializer.ReadInt64(context);
//            DateTimeScale scale = (DateTimeScale)(value & (long)0x03);
//            value >>= 2;
//            switch (scale)
//            {
//                case DateTimeScale.Days:
//                    value *= TimeSpan.TicksPerDay;
//                    break;
//                case DateTimeScale.Seconds:
//                    value *= TimeSpan.TicksPerSecond;
//                    break;
//                case DateTimeScale.Milliseconds:
//                    value *= TimeSpan.TicksPerMillisecond;
//                    break;
//                default:
//                    throw new NotSupportedException("Unknown DateTime scale: " + scale.ToString());
//            }
//            return value;
//        }

//        private static long EncodeTimeSpan(TimeSpan value)
//        {
//            DateTimeScale scale;
//            if (value == TimeSpan.Zero)
//            {
//                return 0;
//            }
//            long result = value.Ticks;
//            if (value.Milliseconds != 0)
//            {
//                scale = DateTimeScale.Milliseconds;
//                result /= TimeSpan.TicksPerMillisecond;
//            }
//            else if (value.Seconds != 0 || value.Minutes != 0 || value.Hours != 0)
//            {
//                scale = DateTimeScale.Seconds;
//                result /= TimeSpan.TicksPerSecond;
//            }
//            else
//            {
//                scale = DateTimeScale.Days;
//                result /= TimeSpan.TicksPerDay;
//            }
//            result <<= 2;
//            long actualScale = (long)scale;
//            return result | actualScale;
//        }
//    }
//}
