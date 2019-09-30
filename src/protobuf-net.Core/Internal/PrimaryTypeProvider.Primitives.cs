using System;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider :
        ISerializer<string>,
        ISerializer<int>,
        ISerializer<long>,
        ISerializer<bool>,
        ISerializer<float>,
        ISerializer<double>,
        ISerializer<byte[]>,
        ISerializer<byte>,
        ISerializer<ushort>,
        ISerializer<uint>,
        ISerializer<ulong>,
        ISerializer<sbyte>,
        ISerializer<short>,
        ISerializer<Uri>,
        ISerializer<char>

    {
        string ISerializer<string>.Read(ref ProtoReader.State state, string value) => state.ReadString();
        void ISerializer<string>.Write(ref ProtoWriter.State state, string value) => state.WriteString(value);
        SerializerFeatures ISerializer<string>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;

        int ISerializer<int>.Read(ref ProtoReader.State state, int value) => state.ReadInt32();
        void ISerializer<int>.Write(ref ProtoWriter.State state, int value) => state.WriteInt32(value);
        SerializerFeatures ISerializer<int>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;

        byte[] ISerializer<byte[]>.Read(ref ProtoReader.State state, byte[] value) => state.AppendBytes(value);
        void ISerializer<byte[]>.Write(ref ProtoWriter.State state, byte[] value) => state.WriteBytes(value);
        SerializerFeatures ISerializer<byte[]>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;

        byte ISerializer<byte>.Read(ref ProtoReader.State state, byte value) => state.ReadByte();
        void ISerializer<byte>.Write(ref ProtoWriter.State state, byte value) => state.WriteByte(value);
        SerializerFeatures ISerializer<byte>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;

        ushort ISerializer<ushort>.Read(ref ProtoReader.State state, ushort value) => state.ReadUInt16();
        void ISerializer<ushort>.Write(ref ProtoWriter.State state, ushort value) => state.WriteUInt16(value);
        SerializerFeatures ISerializer<ushort>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;

        uint ISerializer<uint>.Read(ref ProtoReader.State state, uint value) => state.ReadUInt32();
        void ISerializer<uint>.Write(ref ProtoWriter.State state, uint value) => state.WriteUInt32(value);
        SerializerFeatures ISerializer<uint>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;

        ulong ISerializer<ulong>.Read(ref ProtoReader.State state, ulong value) => state.ReadUInt64();
        void ISerializer<ulong>.Write(ref ProtoWriter.State state, ulong value) => state.WriteUInt64(value);
        SerializerFeatures ISerializer<ulong>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;

        long ISerializer<long>.Read(ref ProtoReader.State state, long value) => state.ReadInt64();
        void ISerializer<long>.Write(ref ProtoWriter.State state, long value) => state.WriteInt64(value);
        SerializerFeatures ISerializer<long>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;

        bool ISerializer<bool>.Read(ref ProtoReader.State state, bool value) => state.ReadBoolean();
        void ISerializer<bool>.Write(ref ProtoWriter.State state, bool value) => state.WriteBoolean(value);
        SerializerFeatures ISerializer<bool>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;

        float ISerializer<float>.Read(ref ProtoReader.State state, float value) => state.ReadSingle();
        void ISerializer<float>.Write(ref ProtoWriter.State state, float value) => state.WriteSingle(value);
        SerializerFeatures ISerializer<float>.Features => SerializerFeatures.WireTypeFixed32 | SerializerFeatures.CategoryScalar;

        double ISerializer<double>.Read(ref ProtoReader.State state, double value) => state.ReadDouble();
        void ISerializer<double>.Write(ref ProtoWriter.State state, double value) => state.WriteDouble(value);
        SerializerFeatures ISerializer<double>.Features => SerializerFeatures.WireTypeFixed64 | SerializerFeatures.CategoryScalar;

        sbyte ISerializer<sbyte>.Read(ref ProtoReader.State state, sbyte value) => state.ReadSByte();
        void ISerializer<sbyte>.Write(ref ProtoWriter.State state, sbyte value) => state.WriteSByte(value);
        SerializerFeatures ISerializer<sbyte>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;

        short ISerializer<short>.Read(ref ProtoReader.State state, short value) => state.ReadInt16();
        void ISerializer<short>.Write(ref ProtoWriter.State state, short value) => state.WriteInt16(value);
        SerializerFeatures ISerializer<short>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;

        Uri ISerializer<Uri>.Read(ref ProtoReader.State state, Uri value)
        {
            var uri = state.ReadString();
            return string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute);
        }
        void ISerializer<Uri>.Write(ref ProtoWriter.State state, Uri value) => state.WriteString(value.OriginalString);
        SerializerFeatures ISerializer<Uri>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;

        char ISerializer<char>.Read(ref ProtoReader.State state, char value) => (char)state.ReadUInt16();
        void ISerializer<char>.Write(ref ProtoWriter.State state, char value) => state.WriteUInt16(value);
        SerializerFeatures ISerializer<char>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
    }
}
