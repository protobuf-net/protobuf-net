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
        IScalarSerializer<Uri>,

        IScalarSerializer<int?>,
        IScalarSerializer<long?>,
        IScalarSerializer<bool?>,
        IScalarSerializer<float?>,
        IScalarSerializer<double?>,
        IScalarSerializer<byte?>,
        IScalarSerializer<ushort?>,
        IScalarSerializer<uint?>,
        IScalarSerializer<ulong?>,
        IScalarSerializer<sbyte?>,
        IScalarSerializer<short?>,
        IScalarSerializer<char>,
        IScalarSerializer<char?>

    {
        string ISerializer<string>.Read(ref ProtoReader.State state, string value) => state.ReadString();
        void ISerializer<string>.Write(ref ProtoWriter.State state, string value) => state.WriteString(value);
        WireType IScalarSerializer<string>.DefaultWireType => WireType.String;

        int ISerializer<int>.Read(ref ProtoReader.State state, int value) => state.ReadInt32();
        void ISerializer<int>.Write(ref ProtoWriter.State state, int value) => state.WriteInt32(value);
        WireType IScalarSerializer<int>.DefaultWireType => WireType.Varint;

        int? ISerializer<int?>.Read(ref ProtoReader.State state, int? value) => state.ReadInt32();
        void ISerializer<int?>.Write(ref ProtoWriter.State state, int? value) => state.WriteInt32(value.Value);
        WireType IScalarSerializer<int?>.DefaultWireType => WireType.Varint;

        byte[] ISerializer<byte[]>.Read(ref ProtoReader.State state, byte[] value) => state.AppendBytes(value);
        void ISerializer<byte[]>.Write(ref ProtoWriter.State state, byte[] value) => state.WriteBytes(value);
        WireType IScalarSerializer<byte[]>.DefaultWireType => WireType.String;

        byte ISerializer<byte>.Read(ref ProtoReader.State state, byte value) => state.ReadByte();
        void ISerializer<byte>.Write(ref ProtoWriter.State state, byte value) => state.WriteByte(value);
        WireType IScalarSerializer<byte>.DefaultWireType => WireType.Varint;

        byte? ISerializer<byte?>.Read(ref ProtoReader.State state, byte? value) => state.ReadByte();
        void ISerializer<byte?>.Write(ref ProtoWriter.State state, byte? value) => state.WriteByte(value.Value);
        WireType IScalarSerializer<byte?>.DefaultWireType => WireType.Varint;

        ushort ISerializer<ushort>.Read(ref ProtoReader.State state, ushort value) => state.ReadUInt16();
        void ISerializer<ushort>.Write(ref ProtoWriter.State state, ushort value) => state.WriteUInt16(value);
        WireType IScalarSerializer<ushort>.DefaultWireType => WireType.Varint;

        ushort? ISerializer<ushort?>.Read(ref ProtoReader.State state, ushort? value) => state.ReadUInt16();
        void ISerializer<ushort?>.Write(ref ProtoWriter.State state, ushort? value) => state.WriteUInt16(value.Value);
        WireType IScalarSerializer<ushort?>.DefaultWireType => WireType.Varint;

        uint ISerializer<uint>.Read(ref ProtoReader.State state, uint value) => state.ReadUInt32();
        void ISerializer<uint>.Write(ref ProtoWriter.State state, uint value) => state.WriteUInt32(value);
        WireType IScalarSerializer<uint>.DefaultWireType => WireType.Varint;

        uint? ISerializer<uint?>.Read(ref ProtoReader.State state, uint? value) => state.ReadUInt32();
        void ISerializer<uint?>.Write(ref ProtoWriter.State state, uint? value) => state.WriteUInt32(value.Value);
        WireType IScalarSerializer<uint?>.DefaultWireType => WireType.Varint;

        ulong ISerializer<ulong>.Read(ref ProtoReader.State state, ulong value) => state.ReadUInt64();
        void ISerializer<ulong>.Write(ref ProtoWriter.State state, ulong value) => state.WriteUInt64(value);
        WireType IScalarSerializer<ulong>.DefaultWireType => WireType.Varint;

        ulong? ISerializer<ulong?>.Read(ref ProtoReader.State state, ulong? value) => state.ReadUInt64();
        void ISerializer<ulong?>.Write(ref ProtoWriter.State state, ulong? value) => state.WriteUInt64(value.Value);
        WireType IScalarSerializer<ulong?>.DefaultWireType => WireType.Varint;

        long ISerializer<long>.Read(ref ProtoReader.State state, long value) => state.ReadInt64();
        void ISerializer<long>.Write(ref ProtoWriter.State state, long value) => state.WriteInt64(value);
        WireType IScalarSerializer<long>.DefaultWireType => WireType.Varint;

        long? ISerializer<long?>.Read(ref ProtoReader.State state, long? value) => state.ReadInt64();
        void ISerializer<long?>.Write(ref ProtoWriter.State state, long? value) => state.WriteInt64(value.Value);
        WireType IScalarSerializer<long?>.DefaultWireType => WireType.Varint;

        bool ISerializer<bool>.Read(ref ProtoReader.State state, bool value) => state.ReadBoolean();
        void ISerializer<bool>.Write(ref ProtoWriter.State state, bool value) => state.WriteBoolean(value);
        WireType IScalarSerializer<bool>.DefaultWireType => WireType.Varint;

        bool? ISerializer<bool?>.Read(ref ProtoReader.State state, bool? value) => state.ReadBoolean();
        void ISerializer<bool?>.Write(ref ProtoWriter.State state, bool? value) => state.WriteBoolean(value.Value);
        WireType IScalarSerializer<bool?>.DefaultWireType => WireType.Varint;

        float ISerializer<float>.Read(ref ProtoReader.State state, float value) => state.ReadSingle();
        void ISerializer<float>.Write(ref ProtoWriter.State state, float value) => state.WriteSingle(value);
        WireType IScalarSerializer<float>.DefaultWireType => WireType.Fixed32;

        float? ISerializer<float?>.Read(ref ProtoReader.State state, float? value) => state.ReadSingle();
        void ISerializer<float?>.Write(ref ProtoWriter.State state, float? value) => state.WriteSingle(value.Value);
        WireType IScalarSerializer<float?>.DefaultWireType => WireType.Fixed32;

        double ISerializer<double>.Read(ref ProtoReader.State state, double value) => state.ReadDouble();
        void ISerializer<double>.Write(ref ProtoWriter.State state, double value) => state.WriteDouble(value);
        WireType IScalarSerializer<double>.DefaultWireType => WireType.Fixed64;

        double? ISerializer<double?>.Read(ref ProtoReader.State state, double? value) => state.ReadDouble();
        void ISerializer<double?>.Write(ref ProtoWriter.State state, double? value) => state.WriteDouble(value.Value);
        WireType IScalarSerializer<double?>.DefaultWireType => WireType.Fixed64;

        sbyte ISerializer<sbyte>.Read(ref ProtoReader.State state, sbyte value) => state.ReadSByte();
        void ISerializer<sbyte>.Write(ref ProtoWriter.State state, sbyte value) => state.WriteSByte(value);
        WireType IScalarSerializer<sbyte>.DefaultWireType => WireType.Varint;

        sbyte? ISerializer<sbyte?>.Read(ref ProtoReader.State state, sbyte? value) => state.ReadSByte();
        void ISerializer<sbyte?>.Write(ref ProtoWriter.State state, sbyte? value) => state.WriteSByte(value.Value);
        WireType IScalarSerializer<sbyte?>.DefaultWireType => WireType.Varint;

        short ISerializer<short>.Read(ref ProtoReader.State state, short value) => state.ReadInt16();
        void ISerializer<short>.Write(ref ProtoWriter.State state, short value) => state.WriteInt16(value);
        WireType IScalarSerializer<short>.DefaultWireType => WireType.Varint;

        short? ISerializer<short?>.Read(ref ProtoReader.State state, short? value) => state.ReadInt16();
        void ISerializer<short?>.Write(ref ProtoWriter.State state, short? value) => state.WriteInt16(value.Value);
        WireType IScalarSerializer<short?>.DefaultWireType => WireType.Varint;

        Uri ISerializer<Uri>.Read(ref ProtoReader.State state, Uri value)
        {
            var uri = state.ReadString();
            return string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute);
        }
        void ISerializer<Uri>.Write(ref ProtoWriter.State state, Uri value) => state.WriteString(value.OriginalString);
        WireType IScalarSerializer<Uri>.DefaultWireType => WireType.String;

        char ISerializer<char>.Read(ref ProtoReader.State state, char value) => (char)state.ReadUInt16();
        void ISerializer<char>.Write(ref ProtoWriter.State state, char value) => state.WriteUInt16(value);
        WireType IScalarSerializer<char>.DefaultWireType => WireType.Varint;

        char? ISerializer<char?>.Read(ref ProtoReader.State state, char? value) => (char)state.ReadUInt16();
        void ISerializer<char?>.Write(ref ProtoWriter.State state, char? value) => state.WriteUInt16(value.Value);
        WireType IScalarSerializer<char?>.DefaultWireType => WireType.Varint;
    }
}
