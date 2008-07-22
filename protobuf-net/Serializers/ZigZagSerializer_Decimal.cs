
namespace ProtoBuf
{
    partial class ZigZagSerializer : ISerializer<decimal>
    {
        string ISerializer<decimal>.DefinedType { get { return ProtoFormat.SINT64; } }

        public decimal Deserialize(decimal value, SerializationContext context)
        {
            long lVal = ZigZagSerializer.ReadInt64(context);
            return TwosComplementSerializer.LongToDecimal(lVal);
        }
        public int GetLength(decimal value, SerializationContext context)
        {
            long lVal = TwosComplementSerializer.DecimalToLong(value);
            return ZigZagSerializer.GetLength(lVal);
        }
        public int Serialize(decimal value, SerializationContext context)
        {
            long lVal = TwosComplementSerializer.DecimalToLong(value);
            return ZigZagSerializer.WriteToStream(lVal, context);
        }
    }
}
