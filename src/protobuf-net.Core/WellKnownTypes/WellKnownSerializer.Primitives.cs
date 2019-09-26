using System;

namespace ProtoBuf.WellKnownTypes
{
    partial class WellKnownSerializer :
        IScalarSerializer<string>,
        IScalarSerializer<int>,
        IScalarSerializer<byte[]>,
        IScalarSerializer<long>,
        IScalarSerializer<bool>,
        IScalarSerializer<float>,
        IScalarSerializer<double>,
        IScalarSerializer<byte>,
        IScalarSerializer<ushort>,
        IScalarSerializer<uint>,
        IScalarSerializer<ulong>,
        IScalarSerializer<sbyte>,
        IScalarSerializer<short>,
        IScalarSerializer<Uri>

    {
        string IScalarSerializer<string>.Read(ref ProtoReader.State state) => state.ReadString();
        void IScalarSerializer<string>.Write(ref ProtoWriter.State state, string value) => state.WriteString(value);
        WireType IScalarSerializer<string>.DefaultWireType => WireType.String;

        int IScalarSerializer<int>.Read(ref ProtoReader.State state) => state.ReadInt32();
        void IScalarSerializer<int>.Write(ref ProtoWriter.State state, int value) => state.WriteInt32(value);
        WireType IScalarSerializer<int>.DefaultWireType => WireType.Varint;

        byte[] IScalarSerializer<byte[]>.Read(ref ProtoReader.State state) => state.AppendBytes(null);
        void IScalarSerializer<byte[]>.Write(ref ProtoWriter.State state, byte[] value) => state.WriteBytes(value);
        WireType IScalarSerializer<byte[]>.DefaultWireType => WireType.String;

        byte IScalarSerializer<byte>.Read(ref ProtoReader.State state) => state.ReadByte();
        void IScalarSerializer<byte>.Write(ref ProtoWriter.State state, byte value) => state.WriteByte(value);
        WireType IScalarSerializer<byte>.DefaultWireType => WireType.Varint;

        ushort IScalarSerializer<ushort>.Read(ref ProtoReader.State state) => state.ReadUInt16();
        void IScalarSerializer<ushort>.Write(ref ProtoWriter.State state, ushort value) => state.WriteUInt16(value);
        WireType IScalarSerializer<ushort>.DefaultWireType => WireType.Varint;

        uint IScalarSerializer<uint>.Read(ref ProtoReader.State state) => state.ReadUInt32();
        void IScalarSerializer<uint>.Write(ref ProtoWriter.State state, uint value) => state.WriteUInt32(value);
        WireType IScalarSerializer<uint>.DefaultWireType => WireType.Varint;

        ulong IScalarSerializer<ulong>.Read(ref ProtoReader.State state) => state.ReadUInt64();
        void IScalarSerializer<ulong>.Write(ref ProtoWriter.State state, ulong value) => state.WriteUInt64(value);
        WireType IScalarSerializer<ulong>.DefaultWireType => WireType.Varint;

        long IScalarSerializer<long>.Read(ref ProtoReader.State state) => state.ReadInt64();
        void IScalarSerializer<long>.Write(ref ProtoWriter.State state, long value) => state.WriteInt64(value);
        WireType IScalarSerializer<long>.DefaultWireType => WireType.Varint;

        bool IScalarSerializer<bool>.Read(ref ProtoReader.State state) => state.ReadBoolean();
        void IScalarSerializer<bool>.Write(ref ProtoWriter.State state, bool value) => state.WriteBoolean(value);
        WireType IScalarSerializer<bool>.DefaultWireType => WireType.Varint;

        float IScalarSerializer<float>.Read(ref ProtoReader.State state) => state.ReadSingle();
        void IScalarSerializer<float>.Write(ref ProtoWriter.State state, float value) => state.WriteSingle(value);
        WireType IScalarSerializer<float>.DefaultWireType => WireType.Fixed32;

        double IScalarSerializer<double>.Read(ref ProtoReader.State state) => state.ReadDouble();
        void IScalarSerializer<double>.Write(ref ProtoWriter.State state, double value) => state.WriteDouble(value);
        WireType IScalarSerializer<double>.DefaultWireType => WireType.Fixed64;

        sbyte IScalarSerializer<sbyte>.Read(ref ProtoReader.State state) => state.ReadSByte();
        void IScalarSerializer<sbyte>.Write(ref ProtoWriter.State state, sbyte value) => state.WriteSByte(value);
        WireType IScalarSerializer<sbyte>.DefaultWireType => WireType.Varint;

        short IScalarSerializer<short>.Read(ref ProtoReader.State state) => state.ReadInt16();
        void IScalarSerializer<short>.Write(ref ProtoWriter.State state, short value) => state.WriteInt16(value);
        WireType IScalarSerializer<short>.DefaultWireType => WireType.Varint;

        Uri IScalarSerializer<Uri>.Read(ref ProtoReader.State state)
        {
            var uri = state.ReadString();
            return string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute);
        }
        void IScalarSerializer<Uri>.Write(ref ProtoWriter.State state, Uri value) => state.WriteString(value.OriginalString);
        WireType IScalarSerializer<Uri>.DefaultWireType => WireType.String;
    }
}
