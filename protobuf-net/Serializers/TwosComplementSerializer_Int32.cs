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
            if (value < 0) return 10;
            value >>= 7;
            if (value == 0) return 1;
            value >>= 7;
            if (value == 0) return 2;
            value >>= 7;
            if (value == 0) return 3;
            value >>= 7;
            return value == 0 ? 4 : 5;
        }
        public int GetLength(int value, SerializationContext context)
        {
            return GetLength(value);
        }
    }
}
