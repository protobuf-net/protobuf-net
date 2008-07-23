using System;

namespace ProtoBuf
{
    internal partial class TwosComplementSerializer : ISerializer<uint>
    {
        public static uint ReadUInt32(SerializationContext context)
        {
            return (uint)Base128Variant.DecodeInt64(context);
        }
        string ISerializer<uint>.DefinedType { get { return ProtoFormat.UINT32; } }
        public uint Deserialize(uint value, SerializationContext context)
        {
            return ReadUInt32(context);
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
            value >>= 7;
            if (value == 0) return 1;
            value >>= 7;
            if (value == 0) return 2;
            value >>= 7;
            if (value == 0) return 3;
            value >>= 7;
            if (value == 0) return 4;
            return 5;
        }
    }
}
