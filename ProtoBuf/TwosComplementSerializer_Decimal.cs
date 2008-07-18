using System;

namespace ProtoBuf
{
    /// Serializes Decimal as an OLE Automation currency
    partial class TwosComplementSerializer : ISerializer<decimal>
    {
        string ISerializer<decimal>.DefinedType { get { return ProtoFormat.INT64; } }

        public decimal Deserialize(decimal value, SerializationContext context)
        {
            long lVal = TwosComplementSerializer.ReadInt64(context);
            return decimal.FromOACurrency(lVal);
        }
        public int GetLength(decimal value, SerializationContext context)
        {
            long lVal = decimal.ToOACurrency(value);
            return TwosComplementSerializer.GetLength(lVal);
        }
        public int Serialize(decimal value, SerializationContext context)
        {
            long lVal = decimal.ToOACurrency(value);
            return TwosComplementSerializer.WriteToStream(lVal, context);
        }
    }
}
