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
            value >>= 7;
            if (value == 0) return 1;
            value >>= 7;
            if (value == 0) return 2;
            value >>= 7;
            if (value == 0) return 3;
            value >>= 7;
            if (value == 0) return 4;
            value >>= 7;
            if (value == 0) return 5;
            value >>= 7;
            if (value == 0) return 6;
            value >>= 7;
            if (value == 0) return 7;
            value >>= 7;
            if (value == 0) return 8;
            value >>= 7;
            return value == 0 ? 9 : 10;
        }
        public int GetLength(long value, SerializationContext context)
        {
            return GetLength(value);
        }
    }
}
