using System;
using System.IO;

namespace ProtoBuf
{
    internal partial class TwosComplementSerializer : ISerializer<int>
    {
        public static int ReadInt32(SerializationContext context)
        {
            return Base128Variant.DecodeInt32(context);
        }
        internal static bool TryReadInt32(SerializationContext context, out int value)
        {
            Eof oldEof = context.Eof;
            try
            {
                context.Eof = Eof.Expected;
                value = Base128Variant.DecodeInt32(context);
                return context.Eof != Eof.Ended;
            }
            finally
            {
                context.Eof = oldEof;
            }
        }

        public static int WriteToStream(int value, SerializationContext context)
        {
            return Base128Variant.EncodeInt32(value, context);
        }

        string ISerializer<int>.DefinedType { get { return ProtoFormat.INT32; } }
        
        public int Deserialize(int value, SerializationContext context)
        {
            return ReadInt32(context);
        }
        public int Serialize(int value, SerializationContext context)
        {
            return WriteToStream(value, context);
        }

        public static int GetLength(int value)
        {
            if ((value & ~0x0000007F) == 0) return 1; // 7 bits
            if ((value & ~0x00003FFF) == 0) return 2; // 14 bits
            if ((value & ~0x001FFFFF) == 0) return 3; // 21 bits
            if ((value & ~0x0FFFFFFF) == 0) return 4; // 28 bits

            if ((value & Base128Variant.Int32Msb) == Base128Variant.Int32Msb) return 10; // -ve
            return 5;
        }
        public int GetLength(int value, SerializationContext context)
        {
            return GetLength(value);
        }
    }
}
