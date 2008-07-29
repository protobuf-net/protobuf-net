//using System;

//namespace ProtoBuf
//{
//    internal partial class TwosComplementSerializer : ISerializer<decimal>
//    {
//        public static long DecimalToLong(decimal value)
//        {
//            int[] raw = decimal.GetBits(value);
//            long scale = (raw[3] >> 16) & 0xFF;
//            bool neg = (raw[3] & Base128Variant.Int32Msb) == Base128Variant.Int32Msb;
//                //sign = ((long)(raw[3] & Base128Variant.Int32Msb)) << 32;
//            if (scale > 15) throw new OverflowException("Only a decimal scale of 0-15 decimal places is supported");
//            // only 27 useful bits in the high byte
//            if (raw[2] != 0 || (raw[1] & 0xF8000000) != 0) throw new OverflowException("Decimal value exceeds supported size");

//            long result = raw[1], low = raw[0];
//            result = (result << 32) | low;
//            if(result == 0) {
//                scale = 0;
//            } else {
//                while(scale > 0 && (result % 10) == 0) {
//                    result /= 10;
//                    scale--;
//                }
//            }
//            if (neg)
//            {
//                result = -result;
//            }
//            return (result << 4) | scale;
//        }

//        public static decimal LongToDecimal(long value)
//        {
//            byte scale = (byte)(value & (long)0x0F);
//            bool neg = false;
//            value >>= 4;
//            if (value < 0)
//            {
//                neg = true;
//                value = -value;
//            }
//            int lo = (int)(value & (long)0xFFFFFFFF);
//            value >>= 32; // only 27 useful bits in the high byte
//            int hi = (int)(value & (long)0x07FFFFFF);
//            return new decimal(lo, hi, 0, neg, scale);
//        }

//        string ISerializer<decimal>.DefinedType { get { return ProtoFormat.INT64; } }

//        public decimal Deserialize(decimal value, SerializationContext context)
//        {
//            return LongToDecimal(TwosComplementSerializer.ReadInt64(context));
//        }
//        public int GetLength(decimal value, SerializationContext context)
//        {
//            return TwosComplementSerializer.GetLength(DecimalToLong(value));
//        }

//        public int Serialize(decimal value, SerializationContext context)
//        {
//            long int64Val = DecimalToLong(value);
//            return TwosComplementSerializer.WriteToStream(int64Val, context);
//        }
//    }
//}
