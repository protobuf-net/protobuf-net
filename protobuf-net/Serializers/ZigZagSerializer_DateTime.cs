//using System;

//namespace ProtoBuf
//{
//    /// <summary>
//    /// Serializes DateTime as milliseconds into the unix epoch
//    /// </summary>
//    internal partial class ZigZagSerializer : ISerializer<DateTime>
//    {
//        string ISerializer<DateTime>.DefinedType { get { return ProtoFormat.SINT64; } }

//        // origin for unix-time
//        private static readonly DateTime epoch = new DateTime(1970, 1, 1);
//        const long DaysInEpoch = 2932897, TicksInEpoch = DaysInEpoch * TimeSpan.TicksPerDay;


//        DateTime ISerializer<DateTime>.Deserialize(DateTime oldValue, SerializationContext context)
//        {
//            long ticks = DecodeTimeSpanTicks(context);
//            return ticks == TicksInEpoch ? DateTime.MaxValue : epoch.AddTicks(ticks);
//        }
        
//        static long EncodeDateTime(DateTime value)
//        {
//            if (value == DateTime.MaxValue)
//            {
//                return (DaysInEpoch << 2) | (long)DateTimeScale.Days;
//            }
//            return EncodeTimeSpan(value - epoch);
//        }
//        int ISerializer<DateTime>.GetLength(DateTime value, SerializationContext context)
//        {
//            return ZigZagSerializer.GetLength(EncodeDateTime(value));
//        }
//        int ISerializer<DateTime>.Serialize(DateTime value, SerializationContext context)
//        {
//            return ZigZagSerializer.WriteToStream(EncodeDateTime(value), context);
//        }

        
//    }
//}
