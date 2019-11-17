using ProtoBuf.Serializers;
using System;
using System.Runtime.InteropServices;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider : ISerializer<Guid>, ISerializer<Guid?>
    {
        SerializerFeatures ISerializer<Guid>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessageWrappedAtRoot;
        SerializerFeatures ISerializer<Guid?>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessageWrappedAtRoot;
        private static
#if !DEBUG
            readonly
#endif
            bool s_guidOptimized = VerifyGuidLayout();

        internal static bool GuidOptimized
        {
            get => s_guidOptimized;
#if DEBUG
            set => s_guidOptimized = value && VerifyGuidLayout();
#endif
        }

        private const int FieldGuidLow = 1, FieldGuidHigh = 2;
        Guid? ISerializer<Guid?>.Read(ref ProtoReader.State state, Guid? value)
            => ((ISerializer<Guid>)this).Read(ref state, value.GetValueOrDefault());
        void ISerializer<Guid?>.Write(ref ProtoWriter.State state, Guid? value)
            => ((ISerializer<Guid>)this).Write(ref state, value.Value);

        Guid ISerializer<Guid>.Read(ref ProtoReader.State state, Guid value)
        {
            ulong low = 0, high = 0;
            int fieldNumber;
            while ((fieldNumber = state.ReadFieldHeader()) > 0)
            {
                switch (fieldNumber)
                {
                    case FieldGuidLow: low = state.ReadUInt64(); break;
                    case FieldGuidHigh: high = state.ReadUInt64(); break;
                    default: state.SkipField(); break;
                }
            }

            if (low == 0 & high == 0) return default;
            if (s_guidOptimized)
            {
                var acc = new GuidAccessor(low, high);
                return acc.Guid;
            }
            else
            {
                uint a = (uint)(low >> 32), b = (uint)low, c = (uint)(high >> 32), d = (uint)high;
                return new Guid((int)b, (short)a, (short)(a >> 16),
                    (byte)d, (byte)(d >> 8), (byte)(d >> 16), (byte)(d >> 24),
                    (byte)c, (byte)(c >> 8), (byte)(c >> 16), (byte)(c >> 24));
            }
        }

        void ISerializer<Guid>.Write(ref ProtoWriter.State state, Guid value)
        {
            if (value == Guid.Empty) { }
            else if (s_guidOptimized)
            {
                var obj = new GuidAccessor(value);
                state.WriteFieldHeader(FieldGuidLow, WireType.Fixed64);
                state.WriteUInt64(obj.Low);
                state.WriteFieldHeader(FieldGuidHigh, WireType.Fixed64);
                state.WriteUInt64(obj.High);
            }
            else
            {
                byte[] blob = value.ToByteArray();
                state.WriteFieldHeader(FieldGuidLow, WireType.Fixed64);
                state.WriteBytes(new ReadOnlyMemory<byte>(blob, 0, 8));
                state.WriteFieldHeader(FieldGuidHigh, WireType.Fixed64);
                state.WriteBytes(new ReadOnlyMemory<byte>(blob, 8, 8));
            }
        }

        /// <summary>
        /// Provides access to the inner fields of a Guid.
        /// Similar to Guid.ToByteArray(), but faster and avoids the byte[] allocation
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private readonly struct GuidAccessor
        {
            [FieldOffset(0)]
            public readonly Guid Guid;

            [FieldOffset(0)]
            public readonly ulong Low;

            [FieldOffset(8)]
            public readonly ulong High;

            public GuidAccessor(Guid value)
            {
                Low = High = default;
                Guid = value;
            }

            public GuidAccessor(ulong low, ulong high)
            {
                Guid = default;
                Low = low;
                High = high;
            }
        }
        private static bool VerifyGuidLayout()
        {
            try
            {
                if (!Guid.TryParse("12345678-2345-3456-4567-56789a6789ab", out var guid))
                    return false;

                var obj = new GuidAccessor(guid);
                var low = obj.Low;
                var high = obj.High;

                // check it the fast way against our known sentinels
                if (low != 0x3456234512345678 | high != 0xAB89679A78566745) return false;

                // and do it "for real"
                var expected = guid.ToByteArray();
                for (int i = 0; i < 8; i++)
                {
                    if (expected[i] != (byte)(low >> (8 * i))) return false;
                }
                for (int i = 0; i < 8; i++)
                {
                    if (expected[i + 8] != (byte)(high >> (8 * i))) return false;
                }
                return true;
            }
            catch { }
            return false;
        }
    }
}
