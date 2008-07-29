using System;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf.Serializers
{
    internal sealed partial class BclSerializer : ISerializer<decimal>
    {
        decimal ISerializer<decimal>.Deserialize(decimal value, SerializationContext context)
        {
            ProtoDecimal pd = context.DecimalTemplate;
            pd.Reset();
            ProtoDecimal.Serializer.Deserialize(pd, context);
            if (pd.Low == 0 && pd.High == 0) return decimal.Zero;

            int[] bits = new int[4];
            bits[0] = (int)(pd.Low & 0xFFFFFFFFL);
            bits[1] = (int)((pd.Low >> 32) & 0xFFFFFFFFL);
            bits[2] = (int)pd.High;
            uint scale = pd.SignScale;
            bits[3] = (int)(((scale & 0x01FE) << 15) | ((scale & 0x0001) << 31));
            return new decimal(bits);
        }

        static void PrepareDecimal(decimal value, ProtoDecimal proto)
        {

            int[] bits = decimal.GetBits(value);
            ulong low = (ulong)bits[0], mid = (ulong)bits[1];
            proto.Low = (mid << 32) | low;
            proto.High = (uint)bits[2];
            proto.SignScale = (uint)(((bits[3] >> 15) & 0x01FE) | ((bits[3] >> 31) & 0x0001));
        }

        int ISerializer<decimal>.Serialize(decimal value, SerializationContext context)
        {
            if (value == 0)
            {
                context.Stream.WriteByte(0); // sub-msg-length
                return 1;
            }
            ProtoDecimal pd = context.DecimalTemplate;
            PrepareDecimal(value, pd);
            return Serialize(pd, context);
        }

        int ISerializer<decimal>.GetLength(decimal value, SerializationContext context)
        {
            if (value == 0) return 1; // sub-msg-length
            ProtoDecimal pd = context.DecimalTemplate;
            PrepareDecimal(value, pd);
            return GetLength(pd);
        }

        static int GetLength(ProtoDecimal value)
        {
            int len = 1;
            if (value.Low != 0)
            {
                len += 1 + TwosComplementSerializer.GetLength(value.Low);
            }
            if (value.High != 0)
            {
                len += 1 + TwosComplementSerializer.GetLength(value.High);
            }
            if (value.SignScale != 0)
            {
                len += 1 + TwosComplementSerializer.GetLength(value.SignScale);
            }
            return len;
        }



        static int Serialize(ProtoDecimal value, SerializationContext context)
        {
            int expected = GetLength(value);
            // write message-length prefix (expect single-byte!)
            context.Stream.WriteByte((byte)(expected - 1));
            int actual = 1;
            if (value.Low != 0)
            {
                context.Stream.WriteByte((0x01 << 3) | (int)WireType.Variant);
                actual += 1 + TwosComplementSerializer.WriteToStream(value.Low, context);
            }
            if (value.High != 0)
            {
                context.Stream.WriteByte((0x02 << 3) | (int)WireType.Variant);
                actual += 1 + TwosComplementSerializer.WriteToStream(value.High, context);
            }
            if (value.SignScale != 0)
            {
                context.Stream.WriteByte((0x03 << 3) | (int)WireType.Variant);
                actual += 1 + TwosComplementSerializer.WriteToStream(value.SignScale, context);
            }
            Serializer.VerifyBytesWritten(expected, actual);
            return actual;
        }

        string ISerializer<decimal>.DefinedType
        {
            get { return ProtoDecimal.Serializer.DefinedType; }
        }
    }
}
