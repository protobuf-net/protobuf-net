using ProtoBuf.Serializers;
using System;
using System.Runtime.InteropServices;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider : ISerializer<Guid>, ISerializer<Guid?>,
        IMeasuringSerializer<Guid>, IMeasuringSerializer<Guid?>
    {
        // MeasureGuidBody already existed for the generated path; exposing it through the interface
        // is what lets a CALLER ask. Safe to add without changing any wire output: the classic
        // engine only consults a measure when the serializer also declares
        // OptionTrySkipWritingWhenMeasuring (ProtoWriter.Measure), which nothing here does, and
        // RepeatedSerializer's packed branch is gated on TypeHelper<T>.CanBePacked, which is false
        // for Guid. See notes/gaps.md B42.
        int IMeasuringSerializer<Guid>.Measure(ISerializationContext context, WireType wireType, Guid value)
            => MeasureGuidBody(value);

        int IMeasuringSerializer<Guid?>.Measure(ISerializationContext context, WireType wireType, Guid? value)
            => MeasureGuidBody(value.GetValueOrDefault());

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
            => ReadRawGuidBody(ref state);

        /// <summary>
        /// The bcl.Guid loop over the raw surface, reading within the CURRENT scope (the caller
        /// frames it - ReadMessage on the stateful path, a self-framing raw wrapper on the
        /// generated path). The fields are written Fixed64; wire tolerance mirrors the stateful
        /// ReadUInt64 this replaced (varint/fixed64/fixed32).
        /// </summary>
        internal static Guid ReadRawGuidBody(ref ProtoReader.State state)
        {
            ulong low = 0, high = 0;
            uint tag = state.ReadRawTag();
            while (tag != 0)
            {
                switch (tag)
                {
                    case (FieldGuidLow << 3) | 1: low = state.ReadRawFixed64(); break;
                    case (FieldGuidLow << 3) | 0: low = state.ReadRawVarint64(); break;
                    case (FieldGuidLow << 3) | 5: low = state.ReadRawFixed32(); break;
                    case (FieldGuidHigh << 3) | 1: high = state.ReadRawFixed64(); break;
                    case (FieldGuidHigh << 3) | 0: high = state.ReadRawVarint64(); break;
                    case (FieldGuidHigh << 3) | 5: high = state.ReadRawFixed32(); break;
                    default:
                        if (state.IsScopeEnd(tag)) goto done;
                        if ((tag >> 3) is FieldGuidLow or FieldGuidHigh)
                        {
                            state.ThrowUnexpectedWireType(tag);
                        }
                        state.SkipTag(tag);
                        break;
                }
                tag = state.ReadRawTag();
            }
            done: ;

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

        /// <summary>
        /// The body length the <c>Write</c> below produces. Beside it deliberately: the two must
        /// agree, and adjacency is the cheapest way to keep that true.
        /// </summary>
        /// <remarks>
        /// Constant, unlike the <c>ScaledTicks</c> body: both branches of the writer emit the same
        /// two <c>Fixed64</c> fields at numbers 1 and 2, so it is two one-byte tags plus sixteen
        /// bytes. <c>Guid.Empty</c> writes nothing at all.
        /// </remarks>
        internal static int MeasureGuidBody(Guid value) => value == Guid.Empty ? 0 : (1 + 8) * 2;

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
