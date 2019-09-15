using System.Runtime.InteropServices;

namespace ProtoBuf.WellKnownTypes
{
    partial class WellKnownSerializer : IBasicSerializer<decimal>, IBasicDeserializer<decimal>
    {
        private const int FieldDecimalLow = 0x01, FieldDecimalHigh = 0x02, FieldDecimalSignScale = 0x03;

        decimal IBasicDeserializer<decimal>.Deserialize(ProtoReader reader, ref ProtoReader.State state, decimal value)
        {
            ulong low = 0;
            uint high = 0;
            uint signScale = 0;
            int fieldNumber;
            while ((fieldNumber = reader.ReadFieldHeader(ref state)) > 0)
            {
                switch (fieldNumber)
                {
                    case FieldDecimalLow: low = reader.ReadUInt64(ref state); break;
                    case FieldDecimalHigh: high = reader.ReadUInt32(ref state); break;
                    case FieldDecimalSignScale: signScale = reader.ReadUInt32(ref state); break;
                    default: reader.SkipField(ref state); break;
                }
            }
            int lo = (int)(low & 0xFFFFFFFFL),
               mid = (int)((low >> 32) & 0xFFFFFFFFL),
               hi = (int)high;
            bool isNeg = (signScale & 0x0001) == 0x0001;
            byte scale = (byte)((signScale & 0x01FE) >> 1);
            return new decimal(lo, mid, hi, isNeg, scale);
        }

        void IBasicSerializer<decimal>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, decimal value)
        {
            ulong low;
            uint high, signScale;
            if (s_decimalOptimized) // the JIT should remove the non-preferred implementation, at least on modern runtimes
            {
                var dec = new DecimalAccessor(value);
                ulong a = ((ulong)dec.Mid) << 32, b = ((ulong)dec.Lo) & 0xFFFFFFFFL;
                low = a | b;
                high = (uint)dec.Hi;
                signScale = (uint)(((dec.Flags >> 15) & 0x01FE) | ((dec.Flags >> 31) & 0x0001));
            }
            else
            {
                int[] bits = decimal.GetBits(value);
                ulong a = ((ulong)bits[1]) << 32, b = ((ulong)bits[0]) & 0xFFFFFFFFL;
                low = a | b;
                high = (uint)bits[2];
                signScale = (uint)(((bits[3] >> 15) & 0x01FE) | ((bits[3] >> 31) & 0x0001));
            }

            if (low != 0)
            {
                ProtoWriter.WriteFieldHeader(FieldDecimalLow, WireType.Variant, writer, ref state);
                ProtoWriter.WriteUInt64(low, writer, ref state);
            }
            if (high != 0)
            {
                ProtoWriter.WriteFieldHeader(FieldDecimalHigh, WireType.Variant, writer, ref state);
                ProtoWriter.WriteUInt32(high, writer, ref state);
            }
            if (signScale != 0)
            {
                ProtoWriter.WriteFieldHeader(FieldDecimalSignScale, WireType.Variant, writer, ref state);
                ProtoWriter.WriteUInt32(signScale, writer, ref state);
            }
        }

        private static
#if !DEBUG
            readonly
#endif
            bool s_decimalOptimized = VerifyDecimalLayout();

        internal static bool DecimalOptimized
        {
            get => s_decimalOptimized;
#if DEBUG
            set => s_decimalOptimized = value && VerifyDecimalLayout();
#endif
        }

        private static bool VerifyDecimalLayout()
        {
            try
            {
                // test against example taken from https://docs.microsoft.com/en-us/dotnet/api/system.decimal.getbits?view=netframework-4.8
                //     1.0000000000000000000000000000    001C0000  204FCE5E  3E250261  10000000
                var value = 1.0000000000000000000000000000M;
                var layout = new DecimalAccessor(value);
                if (layout.Lo == 0x10000000
                    & layout.Mid == 0x3E250261
                    & layout.Hi == 0x204FCE5E
                    & layout.Flags == 0x001C0000)
                {
                    // and double-check against GetBits itself
                    var bits = decimal.GetBits(value);
                    if (bits.Length == 4)
                    {
                        return layout.Lo == bits[0]
                            & layout.Mid == bits[1]
                            & layout.Hi == bits[2]
                            & layout.Flags == bits[3];
                    }
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Provides access to the inner fields of a decimal.
        /// Similar to decimal.GetBits(), but faster and avoids the int[] allocation
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private readonly struct DecimalAccessor
        {
            [FieldOffset(0)]
            public readonly int Flags;
            [FieldOffset(4)]
            public readonly int Hi;
            [FieldOffset(8)]
            public readonly int Lo;
            [FieldOffset(12)]
            public readonly int Mid;

            [FieldOffset(0)]
            public readonly decimal Decimal;

            public DecimalAccessor(decimal value)
            {
                this = default;
                Decimal = value;
            }
        }
    }
}
