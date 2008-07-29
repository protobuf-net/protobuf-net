
//namespace ProtoBuf
//{
//    internal partial class ZigZagSerializer : ISerializer<decimal>
//    {
//        string ISerializer<decimal>.DefinedType { get { return ProtoFormat.SINT64; } }

//        public decimal Deserialize(decimal value, SerializationContext context)
//        {
//            long int64Val = ZigZagSerializer.ReadInt64(context);
//            return TwosComplementSerializer.LongToDecimal(int64Val);
//        }
//        public int GetLength(decimal value, SerializationContext context)
//        {
//            long int64Val = TwosComplementSerializer.DecimalToLong(value);
//            return ZigZagSerializer.GetLength(int64Val);
//        }

//        public int Serialize(decimal value, SerializationContext context)
//        {
//            long int64Val = TwosComplementSerializer.DecimalToLong(value);
//            return ZigZagSerializer.WriteToStream(int64Val, context);
//        }
//    }
//}
