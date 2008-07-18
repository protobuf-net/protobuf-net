using System;
using System.IO;

namespace ProtoBuf
{
    sealed class Int32VariantSerializer : ISerializer<int>
    {
        
        public static int ReadFromStream(SerializationContext context)
        {
            return Base128Variant.DecodeInt32(context);
        }
        internal static bool TryReadFromStream(SerializationContext context, out int value)
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
            return context.Write(Base128Variant.EncodeInt32(value, context));
        }
        public string DefinedType { get { return "int32"; } }
        public WireType WireType { get { return WireType.Variant; } }
        public int Deserialize(int value, SerializationContext context)
        {
            return ReadFromStream(context);
        }
        public int Serialize(int value, SerializationContext context)
        {
            return WriteToStream(value, context);
        }
        
        public static int GetLength(int value)
        {
            if (value < 0) return 10;
            unchecked
            {
                return UInt32VariantSerializer.GetLength((uint)value);
            }
        }
        public int GetLength(int value, SerializationContext context)
        {
            return GetLength(value);
        }
    }
}
