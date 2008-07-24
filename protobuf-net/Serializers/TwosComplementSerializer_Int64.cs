using System;

namespace ProtoBuf
{
    internal partial class TwosComplementSerializer : ISerializer<long>
    {
        string ISerializer<long>.DefinedType { get { return ProtoFormat.INT64; } }

        public static long ReadInt64(SerializationContext context)
        {
            return Base128Variant.DecodeInt64(context);
        }

        public long Deserialize(long value, SerializationContext context)
        {
            return ReadInt64(context);
        }

        public static int WriteToStream(long value, SerializationContext context)
        {
            return Base128Variant.EncodeInt64(value, context);
        }
        public int Serialize(long value, SerializationContext context)
        {
            return WriteToStream(value, context);
        }
        public static int GetLength(long value)
        {
            if (value < 0) return 10;
            unchecked
            {
                return TwosComplementSerializer.GetLength((ulong)value);
            }
        }
        public int GetLength(long value, SerializationContext context)
        {
            return GetLength(value);
        }
    }
}
