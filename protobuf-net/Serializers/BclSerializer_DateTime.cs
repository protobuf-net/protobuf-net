using System;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf.Serializers
{
    internal sealed partial class BclSerializer : ISerializer<DateTime>, ILengthSerializer<DateTime>
    {
        DateTime ISerializer<DateTime>.Deserialize(DateTime value, SerializationContext context)
        {
            long ticks = DeserializeTicks(context);
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
            if (value == Epoch) return 0;
            TimeSpan timeSpan;
            if (value == DateTime.MaxValue)
            {
                timeSpan = TimeSpan.MaxValue;
            }
            else if (value == DateTime.MinValue)
            {
                timeSpan = TimeSpan.MinValue;
            }
            else
            {
                timeSpan = value - Epoch;
            }
            return SerializeTicks(timeSpan, context);
        }

        int ILengthSerializer<DateTime>.UnderestimateLength(DateTime value)
        {
            return 0;
        }

        static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        string ISerializer<DateTime>.DefinedType
        {
            get { return "bcl.DateTime"; }
        }

    }
}
