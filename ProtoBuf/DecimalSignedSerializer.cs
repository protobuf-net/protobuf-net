
namespace ProtoBuf
{
    sealed class DecimalSignedSerializer : ISerializer<decimal>
    {
        public WireType WireType { get { return WireType.Variant; } }
        public string DefinedType { get { return "sint64"; } }

        public decimal Deserialize(decimal value, SerializationContext context)
        {
            long lVal = Int64SignedVariantSerializer.ReadFromStream(context);
            return decimal.FromOACurrency(lVal);
        }
        public int GetLength(decimal value, SerializationContext context)
        {
            long lVal = decimal.ToOACurrency(value);
            return Int64SignedVariantSerializer.GetLength(lVal);
        }
        public int Serialize(decimal value, SerializationContext context)
        {
            long lVal = decimal.ToOACurrency(value);
            return Int64SignedVariantSerializer.WriteToStream(lVal, context);
        }
    }
}
