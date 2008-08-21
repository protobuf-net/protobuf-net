
namespace ProtoBuf.ProtoBcl
{
    internal static class ProtoDecimal
    {
        const int FieldDecimalLow = 0x01, FieldDecimalHigh = 0x02, FieldDecimalSignScale = 0x03;
    

        public static decimal DeserializeDecimal(SerializationContext context) {
            ulong low = 0;
            uint high = 0;
            uint signScale = 0;

            uint prefix;
            bool keepRunning = true;
            while (keepRunning && (prefix = context.TryReadFieldPrefix()) > 0)
            {
                switch (prefix)
                {
                    case (FieldDecimalLow << 3) | (int)WireType.Variant:
                        low = (ulong)context.DecodeInt64();
                        break;
                    case (FieldDecimalHigh << 3) | (int)WireType.Variant:
                        high = (uint)context.DecodeInt32();
                        break;
                    case (FieldDecimalSignScale << 3) | (int)WireType.Variant:
                        signScale = (uint)context.DecodeInt32();
                        break;
                    default:
                        WireType wireType;
                        int fieldTag;
                        Serializer.ParseFieldToken(prefix, out wireType, out fieldTag);
                        if (wireType == WireType.EndGroup)
                        {
                            context.EndGroup(fieldTag);
                            keepRunning = false;
                            continue;
                        }
                        switch (fieldTag)
                        {
                            case FieldDecimalHigh:
                            case FieldDecimalLow:
                            case FieldDecimalSignScale:
                                throw new ProtoException("Incorrect wire-type deserializing Decimal");
                            default:
                                Serializer.SkipData(context, fieldTag, wireType);
                                break;
                        }
                        break;
                }
            }

            if (low == 0 && high == 0) return decimal.Zero;

            int lo = (int)(low & 0xFFFFFFFFL),
                mid = (int)((low >> 32) & 0xFFFFFFFFL),
                hi = (int)high;
            bool isNeg = (signScale & 0x0001) == 0x0001;
            byte scale = (byte)((signScale & 0x01FE) >> 1);
            return new decimal(lo, mid, hi, isNeg, scale);
        }

        public static int SerializeDecimal(decimal value, SerializationContext context, bool lengthPrefixed)
        {
            int[] bits = decimal.GetBits(value);
            ulong a = ((ulong)bits[1]) << 32, b = ((ulong)bits[0]) & 0xFFFFFFFFL;
            ulong low = a | b;
            uint high = (uint) bits[2];
            uint signScale = (uint)(((bits[3] >> 15) & 0x01FE) | ((bits[3] >> 31) & 0x0001));

            int len = 0;
            if (lengthPrefixed)
            {
                if (low != 0) len += 1 + SerializationContext.GetLength((long)low);
                if (high != 0) len += 1 + SerializationContext.GetLength((long)high);
                if (signScale!=0) len += 2;
                len = context.EncodeInt32(len);
            }
            if (low != 0)
            {
                context.WriteByte((FieldDecimalLow << 3) | (int)WireType.Variant);
                len += 1 + context.EncodeInt64((long)low);
            }
            if (high != 0)
            { // note encode as long to avoid high sign issues
                context.WriteByte((FieldDecimalHigh << 3) | (int)WireType.Variant);
                len += 1 + context.EncodeInt64((long)high);
            }
            if (signScale != 0)
            {
                context.WriteByte((FieldDecimalSignScale << 3) | (int)WireType.Variant);
                context.WriteByte((byte)signScale);
                len += 2;
            }
            return len;
        }
    }
}
