using System;

namespace ProtoBuf
{
    sealed class UInt32VariantSerializer : ISerializer<uint>
    {
        public static uint ReadFromStream(SerializationContext context)
        {
            return (uint)Base128Variant.DecodeInt64(context);
        }
        public string DefinedType { get { return "uint32"; } }
        public WireType WireType { get { return WireType.Variant; } }
        public uint Deserialize(uint value, SerializationContext context)
        {
            return ReadFromStream(context);
        }
        public static int WriteToStream(uint value, SerializationContext context)
        {
            return context.Write(Base128Variant.EncodeInt64(value, context));
        }
        public int Serialize(uint value, SerializationContext context)
        {
            return WriteToStream(value, context);
        }
        public int GetLength(uint value, SerializationContext context)
        {
            return GetLength(value);
        }
        public static int GetLength(uint value)
        {
            value >>= 7; if (value == 0) return 1;
            value >>= 7; if (value == 0) return 2;
            value >>= 7; if (value == 0) return 3;
            value >>= 7; if (value == 0) return 4;
            return 5;
        }
    }
}
