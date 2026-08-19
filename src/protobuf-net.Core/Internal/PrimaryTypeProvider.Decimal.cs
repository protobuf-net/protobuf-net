using ProtoBuf.Serializers;
using System.Runtime.InteropServices;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider : ISerializer<decimal>, ISerializer<decimal?>
    {
        SerializerFeatures ISerializer<decimal>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessageWrappedAtRoot;
        SerializerFeatures ISerializer<decimal?>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessageWrappedAtRoot;
        private const int FieldDecimalLow = 0x01, FieldDecimalHigh = 0x02, FieldDecimalSignScale = 0x03;

        decimal? ISerializer<decimal?>.Read(ref ProtoReader.State state, decimal? value)
            => ((ISerializer<decimal>)this).Read(ref state, value.GetValueOrDefault());
        void ISerializer<decimal?>.Write(ref ProtoWriter.State state, decimal? value)
            => ((ISerializer<decimal>)this).Write(ref state, value.Value);

        decimal ISerializer<decimal>.Read(ref ProtoReader.State state, decimal value)
            => ReadRawDecimalBody(ref state);

        /// <summary>
        /// The bcl.Decimal loop over the raw surface, reading within the CURRENT scope (the
        /// caller frames it - ReadMessage on the stateful path, a self-framing raw wrapper on
        /// the generated path). Wire tolerance per field mirrors the stateful
        /// ReadUInt64/ReadUInt32 this replaced: varint, fixed64 and fixed32 all accepted.
        /// </summary>
        internal static decimal ReadRawDecimalBody(ref ProtoReader.State state)
        {
            ulong low = 0;
            uint high = 0;
            uint signScale = 0;
            uint tag = state.ReadRawTag();
            while (tag != 0)
            {
                switch (tag)
                {
                    case (FieldDecimalLow << 3) | 0: low = state.ReadRawVarint64(); break;
                    case (FieldDecimalLow << 3) | 1: low = state.ReadRawFixed64(); break;
                    case (FieldDecimalLow << 3) | 5: low = state.ReadRawFixed32(); break;
                    case (FieldDecimalHigh << 3) | 0: high = state.ReadRawVarint32(); break;
                    case (FieldDecimalHigh << 3) | 5: high = state.ReadRawFixed32(); break;
                    case (FieldDecimalHigh << 3) | 1: high = checked((uint)state.ReadRawFixed64()); break;
                    case (FieldDecimalSignScale << 3) | 0: signScale = state.ReadRawVarint32(); break;
                    case (FieldDecimalSignScale << 3) | 5: signScale = state.ReadRawFixed32(); break;
                    case (FieldDecimalSignScale << 3) | 1: signScale = checked((uint)state.ReadRawFixed64()); break;
                    default:
                        if (state.IsScopeEnd(tag)) goto done;
                        if ((tag >> 3) is FieldDecimalLow or FieldDecimalHigh or FieldDecimalSignScale)
                        {
                            state.ThrowUnexpectedWireType(tag);
                        }
                        state.SkipTag(tag);
                        break;
                }
                tag = state.ReadRawTag();
            }
            done:
            int lo = (int)(low & 0xFFFFFFFFL),
               mid = (int)((low >> 32) & 0xFFFFFFFFL),
               hi = (int)high;
            bool isNeg = (signScale & 0x0001) == 0x0001;
            byte scale = (byte)((signScale & 0x01FE) >> 1);
            return new decimal(lo, mid, hi, isNeg, scale);
        }

        /// <summary>
        /// The body length the <c>Write</c> below produces — the same three conditional fields,
        /// sized rather than written. Kept adjacent to the writer for the usual reason.
        /// </summary>
        /// <remarks>
        /// Value-dependent, because each field is omitted when zero: a <c>0m</c> has an entirely
        /// empty body. The bit-twiddling is duplicated from the writer rather than factored out,
        /// which is the one wart here — the alternative was reshaping a hot write path to hand back
        /// its intermediate values.
        /// </remarks>
        internal static int MeasureDecimalBody(decimal value)
        {
            ulong low;
            uint high, signScale;
            if (s_decimalOptimized)
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

            int len = 0;
            if (low != 0) len += 1 + ProtoWriter.MeasureUInt64(low);
            if (high != 0) len += 1 + ProtoWriter.MeasureUInt32(high);
            if (signScale != 0) len += 1 + ProtoWriter.MeasureUInt32(signScale);
            return len;
        }

        void ISerializer<decimal>.Write(ref ProtoWriter.State state, decimal value)
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
                state.WriteFieldHeader(FieldDecimalLow, WireType.Varint);
                state.WriteUInt64(low);
            }
            if (high != 0)
            {
                state.WriteFieldHeader(FieldDecimalHigh, WireType.Varint);
                state.WriteUInt32(high);
            }
            if (signScale != 0)
            {
                state.WriteFieldHeader(FieldDecimalSignScale, WireType.Varint);
                state.WriteUInt32(signScale);
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
