using System.Runtime.CompilerServices;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider
    {
        private abstract class EnumSerializer<TEnum, TRaw>
            : ISerializer<TEnum>, ISerializer<TEnum?>, IScalarSerializer<TEnum>, IScalarSerializer<TEnum?>
            where TRaw : unmanaged
            where TEnum : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected abstract TRaw Read(ref ProtoReader.State state);
            [MethodImpl(ProtoReader.HotPath)]
            protected abstract void Write(ref ProtoWriter.State state, TRaw value);

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
            public WireType DefaultWireType => WireType.Varint;
        }
        private sealed class EnumSerializerSByte<T> : EnumSerializer<T, sbyte> where T : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected override sbyte Read(ref ProtoReader.State state) => state.ReadSByte();
            [MethodImpl(ProtoReader.HotPath)]
            protected override void Write(ref ProtoWriter.State state, sbyte value) => state.WriteSByte(value);
        }
        private sealed class EnumSerializerInt16<T> : EnumSerializer<T, short> where T : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected override short Read(ref ProtoReader.State state) => state.ReadInt16();
            [MethodImpl(ProtoReader.HotPath)]
            protected override void Write(ref ProtoWriter.State state, short value) => state.WriteInt16(value);
        }
        private sealed class EnumSerializerInt32<T> : EnumSerializer<T, int> where T : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected override int Read(ref ProtoReader.State state) => state.ReadInt32();
            [MethodImpl(ProtoReader.HotPath)]
            protected override void Write(ref ProtoWriter.State state, int value) => state.WriteInt32(value);
        }
        private sealed class EnumSerializerInt64<T> : EnumSerializer<T, long> where T : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected override long Read(ref ProtoReader.State state) => state.ReadInt64();
            [MethodImpl(ProtoReader.HotPath)]
            protected override void Write(ref ProtoWriter.State state, long value) => state.WriteInt64(value);
        }
        private sealed class EnumSerializerByte<T> : EnumSerializer<T, byte> where T : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected override byte Read(ref ProtoReader.State state) => state.ReadByte();
            [MethodImpl(ProtoReader.HotPath)]
            protected override void Write(ref ProtoWriter.State state, byte value) => state.WriteByte(value);
        }
        private sealed class EnumSerializerUInt16<T> : EnumSerializer<T, ushort> where T : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected override ushort Read(ref ProtoReader.State state) => state.ReadUInt16();
            [MethodImpl(ProtoReader.HotPath)]
            protected override void Write(ref ProtoWriter.State state, ushort value) => state.WriteUInt16(value);
        }
        private sealed class EnumSerializerUInt32<T> : EnumSerializer<T, uint> where T : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected override uint Read(ref ProtoReader.State state) => state.ReadUInt32();
            [MethodImpl(ProtoReader.HotPath)]
            protected override void Write(ref ProtoWriter.State state, uint value) => state.WriteUInt32(value);
        }
        private sealed class EnumSerializerUInt64<T> : EnumSerializer<T, ulong> where T : unmanaged
        {
            [MethodImpl(ProtoReader.HotPath)]
            protected override ulong Read(ref ProtoReader.State state) => state.ReadUInt64();
            [MethodImpl(ProtoReader.HotPath)]
            protected override void Write(ref ProtoWriter.State state, ulong value) => state.WriteUInt64(value);
        }
    }
}
