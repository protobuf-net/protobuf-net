using System;

namespace ProtoBuf
{
    /// <summary>
    /// Serializes DateTime as milliseconds into the unix epoch
    /// </summary>
    partial class ZigZagSerializer : ISerializer<DateTime>
    {
        public string DefinedType { get { return ProtoFormat.SINT64; } }

        // origin for unix-time
        static readonly DateTime epoch = new DateTime(1970, 1, 1);

        public DateTime Deserialize(DateTime value, SerializationContext context)
        {
            long ms = ZigZagSerializer.ReadInt64(context),
                ticks = ms * TimeSpan.TicksPerMillisecond;
            return epoch.Add(TimeSpan.FromTicks(ticks));
        }
        private static long GetOffset(DateTime value)
        {
            return (value - epoch).Ticks / TimeSpan.TicksPerMillisecond;
        }
        public int GetLength(DateTime value, SerializationContext context)
        {
            return ZigZagSerializer.GetLength(GetOffset(value));
        }
        public int Serialize(DateTime value, SerializationContext context)
        {
            long offset = GetOffset(value);
            return ZigZagSerializer.WriteToStream(offset, context);
        }
    }
}
