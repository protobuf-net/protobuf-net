using System;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf.Serializers
{
    internal sealed partial class BclSerializer : ISerializer<decimal>, ILengthSerializer<decimal>
    {

        const int FieldDecimalLow = 0x01, FieldDecimalHigh = 0x02, FieldDecimalSignScale = 0x03;

        decimal ISerializer<decimal>.Deserialize(decimal value, SerializationContext context)
        {
            return DeserializeDecimal(context);
        }
        
        static decimal DeserializeDecimal(SerializationContext context) {
            ulong low = 0;
            uint high = 0;
            uint signScale = 0;

            int prefix;
            bool keepRunning = true;
            while (keepRunning && context.IsDataAvailable && TwosComplementSerializer.TryReadInt32(context, out prefix))
            {
                switch (prefix)
                {
                    case (FieldDecimalLow << 3) | (int)WireType.Variant:
                        low = TwosComplementSerializer.ReadUInt64(context);
                        break;
                    case (FieldDecimalHigh << 3) | (int)WireType.Variant:
                        high = TwosComplementSerializer.ReadUInt32(context);
                        break;
                    case (FieldDecimalSignScale << 3) | (int)WireType.Variant:
                        signScale = TwosComplementSerializer.ReadUInt32(context);
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
                                throw new ProtoException("Incorrect wire-type deserializing Deciaml");
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

        int ISerializer<decimal>.Serialize(decimal value, SerializationContext context)
        {
            if (value == 0) return 0;
            return SerializeDecimal(value, context);
        }
        
        int ILengthSerializer<decimal>.UnderestimateLength(decimal value)
        {
            return 0;
        }

        static int SerializeDecimal(decimal value, SerializationContext context)
        {
            int[] bits = decimal.GetBits(value);
            ulong a = ((ulong)bits[1]) << 32, b = ((ulong)bits[0]) & 0xFFFFFFFFL;
            ulong low = a | b;
            uint high = (uint) bits[2];
            uint signScale = (uint)(((bits[3] >> 15) & 0x01FE) | ((bits[3] >> 31) & 0x0001));

            int len = 0;
            if (low != 0)
            {
                context.WriteByte((FieldDecimalLow << 3) | (int)WireType.Variant);
                len += 1 + TwosComplementSerializer.WriteToStream(low, context);
            }
            if (high != 0)
            {
                context.WriteByte((FieldDecimalHigh << 3) | (int)WireType.Variant);
                len += 1 + TwosComplementSerializer.WriteToStream(high, context);
            }
            if (signScale != 0)
            {
                context.WriteByte((FieldDecimalSignScale << 3) | (int)WireType.Variant);
                len += 1 + TwosComplementSerializer.WriteToStream(signScale, context);
            }
            return len;
        }

        string ISerializer<decimal>.DefinedType
        {
            get { return "Bcl.Decimal"; }
        }
    }
}
