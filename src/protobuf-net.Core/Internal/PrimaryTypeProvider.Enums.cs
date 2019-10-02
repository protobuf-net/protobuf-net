using System.Runtime.CompilerServices;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider
    {
        private abstract class EnumSerializer<TEnum, TRaw>
            : IMeasuringSerializer<TEnum>, IMeasuringSerializer<TEnum?>
            where TRaw : unmanaged
            where TEnum : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected abstract TRaw Read(ref ProtoReader.State state);
            [MethodImpl(ProtoReader.HotPath)]
            protected abstract void Write(ref ProtoWriter.State state, TRaw value);
            [MethodImpl(ProtoReader.HotPath)]
            public abstract int MeasureVarint(TRaw value);
            [MethodImpl(ProtoReader.HotPath)]
            public virtual int MeasureSignedVarint(TRaw value) => -1;

            [MethodImpl(ProtoReader.HotPath)]
            public unsafe TEnum Read(ref ProtoReader.State state, TEnum value)
            {
                var raw = Read(ref state);
                return *(TEnum*)&raw;
            }

            [MethodImpl(ProtoReader.HotPath)]
            TEnum? ISerializer<TEnum?>.Read(ref ProtoReader.State state, TEnum? value)
                => Read(ref state, default);
            [MethodImpl(ProtoReader.HotPath)]
            public unsafe void Write(ref ProtoWriter.State state, TEnum value)
                => Write(ref state, *(TRaw*)&value);
            [MethodImpl(ProtoReader.HotPath)]
            void ISerializer<TEnum?>.Write(ref ProtoWriter.State state, TEnum? value)
                => Write(ref state, value.Value);
            public SerializerFeatures Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;

            public unsafe int Measure(ISerializationContext context, WireType wireType, TEnum value) => wireType switch
                {
                    WireType.Fixed32 => 4,
                    WireType.Fixed64 => 8,
                    WireType.Varint => MeasureVarint(*(TRaw*)&value),
                    WireType.SignedVarint => MeasureSignedVarint(*(TRaw*)&value),
                    _ => -1,
                };

            int IMeasuringSerializer<TEnum?>.Measure(ISerializationContext context, WireType wireType, TEnum? value)
                => Measure(context, wireType, value.Value);
        }
        const int NegLength = 10;
        private sealed class EnumSerializerSByte<T> : EnumSerializer<T, sbyte> where T : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected override sbyte Read(ref ProtoReader.State state) => state.ReadSByte();
            [MethodImpl(ProtoReader.HotPath)]
            protected override void Write(ref ProtoWriter.State state, sbyte value) => state.WriteSByte(value);

            public override int MeasureVarint(sbyte value) => value < 0 ? NegLength : ProtoWriter.MeasureUInt32((uint)value);
            public override int MeasureSignedVarint(sbyte value) => ProtoWriter.MeasureUInt32(ProtoWriter.Zig(value));
        }
        private sealed class EnumSerializerInt16<T> : EnumSerializer<T, short> where T : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected override short Read(ref ProtoReader.State state) => state.ReadInt16();
            [MethodImpl(ProtoReader.HotPath)]
            protected override void Write(ref ProtoWriter.State state, short value) => state.WriteInt16(value);
            public override int MeasureVarint(short value) => value < 0 ? NegLength : ProtoWriter.MeasureUInt32((uint)value);
            public override int MeasureSignedVarint(short value) => ProtoWriter.MeasureUInt32(ProtoWriter.Zig(value));
        }
        private sealed class EnumSerializerInt32<T> : EnumSerializer<T, int> where T : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected override int Read(ref ProtoReader.State state) => state.ReadInt32();
            [MethodImpl(ProtoReader.HotPath)]
            protected override void Write(ref ProtoWriter.State state, int value) => state.WriteInt32(value);
            public override int MeasureVarint(int value) => value < 0 ? NegLength : ProtoWriter.MeasureUInt32((uint)value);
            public override int MeasureSignedVarint(int value) => ProtoWriter.MeasureUInt32(ProtoWriter.Zig(value));
        }
        private sealed class EnumSerializerInt64<T> : EnumSerializer<T, long> where T : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected override long Read(ref ProtoReader.State state) => state.ReadInt64();
            [MethodImpl(ProtoReader.HotPath)]
            protected override void Write(ref ProtoWriter.State state, long value) => state.WriteInt64(value);
            public override int MeasureVarint(long value) => ProtoWriter.MeasureUInt64((ulong)value);
            public override int MeasureSignedVarint(long value) => ProtoWriter.MeasureUInt64(ProtoWriter.Zig(value));
        }
        private sealed class EnumSerializerByte<T> : EnumSerializer<T, byte> where T : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected override byte Read(ref ProtoReader.State state) => state.ReadByte();
            [MethodImpl(ProtoReader.HotPath)]
            protected override void Write(ref ProtoWriter.State state, byte value) => state.WriteByte(value);

            public override int MeasureVarint(byte value) => ProtoWriter.MeasureUInt32(value);
        }
        private sealed class EnumSerializerUInt16<T> : EnumSerializer<T, ushort> where T : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected override ushort Read(ref ProtoReader.State state) => state.ReadUInt16();
            [MethodImpl(ProtoReader.HotPath)]
            protected override void Write(ref ProtoWriter.State state, ushort value) => state.WriteUInt16(value);

            public override int MeasureVarint(ushort value) => ProtoWriter.MeasureUInt32(value);
        }
        private sealed class EnumSerializerUInt32<T> : EnumSerializer<T, uint> where T : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected override uint Read(ref ProtoReader.State state) => state.ReadUInt32();
            [MethodImpl(ProtoReader.HotPath)]
            protected override void Write(ref ProtoWriter.State state, uint value) => state.WriteUInt32(value);

            public override int MeasureVarint(uint value) => ProtoWriter.MeasureUInt32(value);
        }
        private sealed class EnumSerializerUInt64<T> : EnumSerializer<T, ulong> where T : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected override ulong Read(ref ProtoReader.State state) => state.ReadUInt64();
            [MethodImpl(ProtoReader.HotPath)]
            protected override void Write(ref ProtoWriter.State state, ulong value) => state.WriteUInt64(value);

            public override int MeasureVarint(ulong value) => ProtoWriter.MeasureUInt64(value);
        }
    }
}
