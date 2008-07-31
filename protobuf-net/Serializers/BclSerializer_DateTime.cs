using System;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf.Serializers
{
    internal sealed partial class BclSerializer : ISerializer<DateTime>, IGroupSerializer<DateTime>
    {
        static void PrepareTimeSpan(DateTime value, ProtoTimeSpan span)
        {
            TimeSpan ts;
            if (value == DateTime.MaxValue)
            {
                ts = TimeSpan.MaxValue;
            }
            else if (value == DateTime.MinValue)
            {
                ts = TimeSpan.MinValue;
            }
            else
            {
                ts = value - Epoch;
            }
            PrepareTimeSpan(ts, span);
        }


        DateTime ISerializer<DateTime>.Deserialize(DateTime value, SerializationContext context)
        {
            long ticks = ReadTimeSpanTicks(context);
            switch (ticks)
            {
                case long.MaxValue:
                    return DateTime.MaxValue;
                case long.MinValue:
                    return DateTime.MinValue;
                default:
                    return Epoch.AddTicks(ticks);
            }
        }
        DateTime IGroupSerializer<DateTime>.DeserializeGroup(DateTime value, SerializationContext context)
        {
            long ticks = ReadTimeSpanTicksGroup(context);
            switch (ticks)
            {
                case long.MaxValue:
                    return DateTime.MaxValue;
                case long.MinValue:
                    return DateTime.MinValue;
                default:
                    return Epoch.AddTicks(ticks);
            }
        }

        int ISerializer<DateTime>.Serialize(DateTime value, SerializationContext context)
        {
            if (value == Epoch)
            {
                context.Stream.WriteByte(0);
                return 1;
            }
            ProtoTimeSpan span = context.TimeSpanTemplate;
            PrepareTimeSpan(value, span);
            return Serialize(span, context);
        }
        int IGroupSerializer<DateTime>.SerializeGroup(DateTime value, SerializationContext context)
        {
            if(value == Epoch) return 0;
            ProtoTimeSpan span = context.TimeSpanTemplate;
            PrepareTimeSpan(value, span);
            return SerializeCore(span, context);
        }


        int ISerializer<DateTime>.GetLength(DateTime value, SerializationContext context)
        {
            return 1 + GetLengthGroup(value, context);
        }
        public int GetLengthGroup(DateTime value, SerializationContext context)
        {
            if (value == Epoch) return 0;
            ProtoTimeSpan span = context.TimeSpanTemplate;
            PrepareTimeSpan(value, span);
            return GetLengthCore(span);
        }
        static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        string ISerializer<DateTime>.DefinedType
        {
            get { return ProtoTimeSpan.Serializer.DefinedType; }
        }

    }
}
