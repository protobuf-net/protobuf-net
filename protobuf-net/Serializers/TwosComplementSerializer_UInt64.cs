using System;

namespace ProtoBuf
{
    internal partial class TwosComplementSerializer : ISerializer<ulong>
    {
        public static ulong ReadUInt64(SerializationContext context)
        {
            long val = Base128Variant.DecodeInt64(context);
            unchecked
            {
                return (ulong)val;
            }
        }

        public static int WriteToStream(ulong value, SerializationContext context)
        {
            long int64Val;
            unchecked
            {
                int64Val = (long)value;
            }
            return Base128Variant.EncodeInt64(int64Val, context);
        }
        string ISerializer<ulong>.DefinedType { get { return ProtoFormat.UINT64; } }
        
        public ulong Deserialize(ulong value, SerializationContext context)
        {
            return ReadUInt64(context);
        }

        public int Serialize(ulong value, SerializationContext context)
        {
            return WriteToStream(value, context);
        }

        public int GetLength(ulong value, SerializationContext context)
        {
            return GetLength(value);
        }

        public static int GetLength(ulong value)
        {
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
    }
}
