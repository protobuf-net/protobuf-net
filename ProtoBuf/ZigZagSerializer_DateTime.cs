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
            return epoch.AddMilliseconds(ZigZagSerializer.ReadInt64(context));
        }
        private static long GetOffset(DateTime value)
        {
            return (long)((value - epoch).TotalMilliseconds);
        }
        public int GetLength(DateTime value, SerializationContext context)
        {
            return ZigZagSerializer.GetLength(GetOffset(value));
        }
        public int Serialize(DateTime value, SerializationContext context)
        {
            return ZigZagSerializer.WriteToStream(GetOffset(value), context);
        }
    }
}
