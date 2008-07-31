using System;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf.Serializers
{
    internal sealed partial class BclSerializer : ISerializer<decimal>, IGroupSerializer<decimal>
    {
        decimal ISerializer<decimal>.Deserialize(decimal value, SerializationContext context)
        {
            ProtoDecimal pd = context.DecimalTemplate;
            pd.Reset();
            ProtoDecimal.Serializer.Deserialize(pd, context);
            return DeserializeCore(pd);
        }
        decimal IGroupSerializer<decimal>.DeserializeGroup(decimal value, SerializationContext context)
        {
            ProtoDecimal pd = context.DecimalTemplate;
            pd.Reset();
            ProtoDecimal.Serializer.DeserializeGroup(pd, context);
            return DeserializeCore(pd);
        }

        static decimal DeserializeCore(ProtoDecimal pd) {
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
            int expected = GetLengthCore(pd);
            // write message-length prefix (expect single-byte!)
            context.Stream.WriteByte((byte)expected);
            int actual = SerializeCore(pd, context);
            Serializer.VerifyBytesWritten(expected, actual);
            return 1 + actual;
        }
        int IGroupSerializer<decimal>.SerializeGroup(decimal value, SerializationContext context) {
            if (value == 0) return 0;
            PrepareDecimal(value, context.DecimalTemplate);
            return SerializeCore(context.DecimalTemplate, context);
        }



        int ISerializer<decimal>.GetLength(decimal value, SerializationContext context)
        {
            return 1 + GetLengthGroup(value, context);
        }

        public int GetLengthGroup(decimal value, SerializationContext context)
        {
            if (value == 0) return 0;
            ProtoDecimal pd = context.DecimalTemplate;
            PrepareDecimal(value, pd);
            return GetLengthCore(pd);
        }

        static int GetLengthCore(ProtoDecimal value)
        {
            int len = 0;
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



        static int SerializeCore(ProtoDecimal value, SerializationContext context)
        {
            int actual = 0;
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
            return actual;
        }

        string ISerializer<decimal>.DefinedType
        {
            get { return ProtoDecimal.Serializer.DefinedType; }
        }
    }
}
