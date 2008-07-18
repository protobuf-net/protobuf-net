using System;

namespace ProtoBuf
{
    sealed class Int64VariantSerializer : ISerializer<long>
    {
        public string DefinedType { get { return "int64"; } }
        public WireType WireType { get { return WireType.Variant; } }

        public static long ReadFromStream(SerializationContext context)
        {
            return Base128Variant.DecodeInt64(context);
        }

        public long Deserialize(long value, SerializationContext context)
        {
            return ReadFromStream(context);
        }

        public static int WriteToStream(long value, SerializationContext context)
        {
            return context.Write(Base128Variant.EncodeInt64(value, context));
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
                return UInt64VariantSerializer.GetLength((ulong)value);
            }
        }
        public  int GetLength(long value, SerializationContext context)
        {
            return GetLength(value);
        }
    }
}
