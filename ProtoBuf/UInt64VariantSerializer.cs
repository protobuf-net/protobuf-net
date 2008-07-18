using System;

namespace ProtoBuf
{
    sealed class UInt64VariantSerializer : ISerializer<ulong>
    {
        public static ulong ReadFromStream(SerializationContext context)
        {
            long val = Base128Variant.DecodeInt64(context);
            unchecked
            {
                return (ulong)val;
            }
        }
        public static int WriteToStream(ulong value, SerializationContext context)
        {
            long lVal;
            unchecked
            {
                lVal = (long)value;
            }
            return context.Write(Base128Variant.EncodeInt64(lVal, context));
        }
        public string DefinedType { get { return "uint64"; } }
        public WireType WireType { get { return WireType.Variant; } }
        public ulong Deserialize(ulong value, SerializationContext context)
        {
            return ReadFromStream(context);
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
            value >>= 7; if (value == 0) return 1;
            value >>= 7; if (value == 0) return 2;
            value >>= 7; if (value == 0) return 3;
            value >>= 7; if (value == 0) return 4;
            value >>= 7; if (value == 0) return 5;
            value >>= 7; if (value == 0) return 6;
            value >>= 7; if (value == 0) return 7;
            value >>= 7; if (value == 0) return 8;
            value >>= 7; if (value == 0) return 9;
            return 10;
        }
    }
}
