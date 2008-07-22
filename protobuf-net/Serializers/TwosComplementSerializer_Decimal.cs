using System;

namespace ProtoBuf
{
    /// Serializes Decimal similar to an OLE Automation currency; 4 digits
    /// after the decimal point
    partial class TwosComplementSerializer : ISerializer<decimal>
    {
        public static long DecimalToLong(decimal value)
        {
            return (long)(value * FACTOR);
        }
        const int FACTOR = 10000;
        public static decimal LongToDecimal(long value)
        {
            return new decimal(value) / FACTOR;
        }
        string ISerializer<decimal>.DefinedType { get { return ProtoFormat.INT64; } }

        public decimal Deserialize(decimal value, SerializationContext context)
        {
            long lVal = TwosComplementSerializer.ReadInt64(context);
            return LongToDecimal(lVal);
        }
        public int GetLength(decimal value, SerializationContext context)
        {
            long lVal = DecimalToLong(value);
            return TwosComplementSerializer.GetLength(lVal);
        }
        public int Serialize(decimal value, SerializationContext context)
        {
            long lVal = DecimalToLong(value);
            return TwosComplementSerializer.WriteToStream(lVal, context);
        }
    }
}
