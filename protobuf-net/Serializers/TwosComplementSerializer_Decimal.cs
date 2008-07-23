using System;

namespace ProtoBuf
{
    // Serializes Decimal similar to an OLE Automation currency; 4 digits
    // after the decimal point
    internal partial class TwosComplementSerializer : ISerializer<decimal>
    {
        public static long DecimalToLong(decimal value)
        {
            return (long)(value * FACTOR);
        }

        private const int FACTOR = 10000;
        public static decimal LongToDecimal(long value)
        {
            return new decimal(value) / FACTOR;
        }

        string ISerializer<decimal>.DefinedType { get { return ProtoFormat.INT64; } }

        public decimal Deserialize(decimal value, SerializationContext context)
        {
            return LongToDecimal(TwosComplementSerializer.ReadInt64(context));
        }
        public int GetLength(decimal value, SerializationContext context)
        {
            return TwosComplementSerializer.GetLength(DecimalToLong(value));
        }

        public int Serialize(decimal value, SerializationContext context)
        {
            long int64Val = DecimalToLong(value);
            return TwosComplementSerializer.WriteToStream(int64Val, context);
        }
    }
}
