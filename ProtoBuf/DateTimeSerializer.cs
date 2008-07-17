using System;

namespace ProtoBuf
{
    /// <summary>
    /// Serializes DateTime as unix time
    /// </summary>
    sealed class DateTimeSerializer : ISerializer<DateTime>
    {
        public string DefinedType { get { return "uint32"; } }
        public WireType WireType { get { return WireType.Variant; } }

        static readonly DateTime epoch = new DateTime(1970, 1, 1);

        public DateTime Deserialize(DateTime value, SerializationContext context)
        {
            uint unixTime = UInt32VariantSerializer.ReadFromStream(context);
            return epoch.AddSeconds(unixTime);
        }
        private static uint GetUnixTime(DateTime value)
        {
            return (uint)((value - epoch).TotalSeconds);
        }
        public int GetLength(DateTime value, SerializationContext context)
        {
            return UInt32VariantSerializer.GetLength(
                GetUnixTime(value));
        }
        public int Serialize(DateTime value, SerializationContext context)
        {
            return UInt32VariantSerializer.WriteToStream(GetUnixTime(value), context);
        }
    }
}
