using System;

namespace ProtoBuf
{
    /// <summary>
    /// Serializes Decimal as an OLE Automation currency
    /// </summary>
    sealed class DecimalSerializer : ISerializer<decimal>
    {
        public WireType WireType { get { return WireType.Variant; } }
        public string DefinedType { get { return "int64"; } }

        public decimal Deserialize(decimal value, SerializationContext context)
        {
            long lVal = Int64VariantSerializer.ReadFromStream(context);
            return decimal.FromOACurrency(lVal);
        }
        public int GetLength(decimal value, SerializationContext context)
        {
            long lVal = decimal.ToOACurrency(value);
            return Int64VariantSerializer.GetLength(lVal);
        }
        public int Serialize(decimal value, SerializationContext context)
        {
            long lVal = decimal.ToOACurrency(value);
            return Int64VariantSerializer.WriteToStream(lVal, context);
        }
    }
}
