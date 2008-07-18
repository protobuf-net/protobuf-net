using System;

namespace ProtoBuf
{
    /// <summary>
    /// Serializes DateTime as milliseconds into the unix epoch
    /// </summary>
    sealed class DateTimeSerializer : ISerializer<DateTime>
    {
        public string DefinedType { get { return "sint64"; } }
        public WireType WireType { get { return WireType.Variant; } }

        // origin for unix-time
        static readonly DateTime epoch = new DateTime(1970, 1, 1);

        public DateTime Deserialize(DateTime value, SerializationContext context)
        {
            return epoch.AddMilliseconds(Int64SignedVariantSerializer.ReadFromStream(context));
        }
        private static long GetOffset(DateTime value)
        {
            return (long)((value - epoch).TotalMilliseconds);
        }
        public int GetLength(DateTime value, SerializationContext context)
        {
            return Int64SignedVariantSerializer.GetLength(GetOffset(value));
        }
        public int Serialize(DateTime value, SerializationContext context)
        {
            return Int64SignedVariantSerializer.WriteToStream(GetOffset(value), context);
        }
    }
}
