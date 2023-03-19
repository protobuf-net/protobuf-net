﻿using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Text;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider :
        IMeasuringSerializer<string>,
        IMeasuringSerializer<int>,
        IMeasuringSerializer<long>,
        IMeasuringSerializer<bool>,
        IMeasuringSerializer<float>,
        IMeasuringSerializer<double>,
        IMeasuringSerializer<byte[]>,
        IMeasuringSerializer<ArraySegment<byte>>,
        IMeasuringSerializer<Memory<byte>>,
        IMeasuringSerializer<ReadOnlyMemory<byte>>,
        IMeasuringSerializer<byte>,
        IMeasuringSerializer<ushort>,
        IMeasuringSerializer<uint>,
        IMeasuringSerializer<ulong>,
        IMeasuringSerializer<sbyte>,
        IMeasuringSerializer<short>,
        IMeasuringSerializer<char>,
        IMeasuringSerializer<Uri>,
        IMeasuringSerializer<Type>,
        IMeasuringSerializer<IntPtr>,
        IMeasuringSerializer<UIntPtr>,

        IFactory<string>,
        IFactory<byte[]>,

        IMeasuringSerializer<int?>,
        IMeasuringSerializer<long?>,
        IMeasuringSerializer<bool?>,
        IMeasuringSerializer<float?>,
        IMeasuringSerializer<double?>,
        IMeasuringSerializer<byte?>,
        IMeasuringSerializer<ushort?>,
        IMeasuringSerializer<uint?>,
        IMeasuringSerializer<ulong?>,
        IMeasuringSerializer<sbyte?>,
        IMeasuringSerializer<short?>,
        IMeasuringSerializer<char?>,
        IMeasuringSerializer<IntPtr?>,
        IMeasuringSerializer<UIntPtr?>,

        IValueChecker<string>,
        IValueChecker<int>,
        IValueChecker<long>,
        IValueChecker<bool>,
        IValueChecker<float>,
        IValueChecker<double>,
        IValueChecker<byte[]>,
        IValueChecker<byte>,
        IValueChecker<ushort>,
        IValueChecker<uint>,
        IValueChecker<ulong>,
        IValueChecker<sbyte>,
        IValueChecker<short>,
        IValueChecker<char>,
        IValueChecker<IntPtr>,
        IValueChecker<UIntPtr>,
        IValueChecker<Uri>,
        IValueChecker<Type>,

        IValueChecker<int?>,
        IValueChecker<long?>,
        IValueChecker<bool?>,
        IValueChecker<float?>,
        IValueChecker<double?>,
        IValueChecker<byte?>,
        IValueChecker<ushort?>,
        IValueChecker<uint?>,
        IValueChecker<ulong?>,
        IValueChecker<sbyte?>,
        IValueChecker<short?>,
        IValueChecker<char?>,
        IValueChecker<IntPtr?>,
        IValueChecker<UIntPtr?>

    {
        string ISerializer<string>.Read(ref ProtoReader.State state, string value) => state.ReadString();
        void ISerializer<string>.Write(ref ProtoWriter.State state, string value) => state.WriteString(value);
        SerializerFeatures ISerializer<string>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;
        int IMeasuringSerializer<string>.Measure(ISerializationContext context, WireType wireType, string value)
            => wireType switch
            {
                WireType.String => ProtoWriter.UTF8.GetByteCount(value),
                _ => -1,
            };

        int ISerializer<int>.Read(ref ProtoReader.State state, int value) => state.ReadInt32();
        void ISerializer<int>.Write(ref ProtoWriter.State state, int value) => state.WriteInt32(value);
        SerializerFeatures ISerializer<int>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
        int IMeasuringSerializer<int>.Measure(ISerializationContext context, WireType wireType, int value)
            => wireType switch
            {
                WireType.Fixed32 => 4,
                WireType.Fixed64 => 8,
                WireType.Varint => value < 0 ? 10 : ProtoWriter.MeasureUInt32((uint)value),
                WireType.SignedVarint => ProtoWriter.MeasureUInt32(ProtoWriter.Zig(value)),
                _ => -1,
            };


        byte[] ISerializer<byte[]>.Read(ref ProtoReader.State state, byte[] value) => state.AppendBytes(value);
        void ISerializer<byte[]>.Write(ref ProtoWriter.State state, byte[] value) => state.WriteBytes(value);
        SerializerFeatures ISerializer<byte[]>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;
        int IMeasuringSerializer<byte[]>.Measure(ISerializationContext context, WireType wireType, byte[] value)
            => wireType switch
            {
                WireType.String => value.Length,
                _ => -1,
            };

        ArraySegment<byte> ISerializer<ArraySegment<byte>>.Read(ref ProtoReader.State state, ArraySegment<byte> value) => state.AppendBytes(value);
        void ISerializer<ArraySegment<byte>>.Write(ref ProtoWriter.State state, ArraySegment<byte> value) => state.WriteBytes(value);
        SerializerFeatures ISerializer<ArraySegment<byte>>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;

        int IMeasuringSerializer<ArraySegment<byte>>.Measure(ISerializationContext context, WireType wireType, ArraySegment<byte> value)
            => wireType switch
            {
                WireType.String => value.Count,
                _ => -1,
            };

        Memory<byte> ISerializer<Memory<byte>>.Read(ref ProtoReader.State state, Memory<byte> value) => state.AppendBytes(value);
        void ISerializer<Memory<byte>>.Write(ref ProtoWriter.State state, Memory<byte> value) => state.WriteBytes(value);
        SerializerFeatures ISerializer<Memory<byte>>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;

        int IMeasuringSerializer<Memory<byte>>.Measure(ISerializationContext context, WireType wireType, Memory<byte> value)
            => wireType switch
            {
                WireType.String => value.Length,
                _ => -1,
            };

        ReadOnlyMemory<byte> ISerializer<ReadOnlyMemory<byte>>.Read(ref ProtoReader.State state, ReadOnlyMemory<byte> value) => state.AppendBytes(value);
        void ISerializer<ReadOnlyMemory<byte>>.Write(ref ProtoWriter.State state, ReadOnlyMemory<byte> value) => state.WriteBytes(value);
        SerializerFeatures ISerializer<ReadOnlyMemory<byte>>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;

        int IMeasuringSerializer<ReadOnlyMemory<byte>>.Measure(ISerializationContext context, WireType wireType, ReadOnlyMemory<byte> value)
            => wireType switch
            {
                WireType.String => value.Length,
                _ => -1,
            };

        byte ISerializer<byte>.Read(ref ProtoReader.State state, byte value) => state.ReadByte();
        void ISerializer<byte>.Write(ref ProtoWriter.State state, byte value) => state.WriteByte(value);
        SerializerFeatures ISerializer<byte>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
        int IMeasuringSerializer<byte>.Measure(ISerializationContext context, WireType wireType, byte value)
            => wireType switch
            {
                WireType.Fixed32 => 4,
                WireType.Fixed64 => 8,
                WireType.Varint => ProtoWriter.MeasureUInt32(value),
                _ => -1,
            };

        ushort ISerializer<ushort>.Read(ref ProtoReader.State state, ushort value) => state.ReadUInt16();
        void ISerializer<ushort>.Write(ref ProtoWriter.State state, ushort value) => state.WriteUInt16(value);
        SerializerFeatures ISerializer<ushort>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
        int IMeasuringSerializer<ushort>.Measure(ISerializationContext context, WireType wireType, ushort value)
            => wireType switch
            {
                WireType.Fixed32 => 4,
                WireType.Fixed64 => 8,
                WireType.Varint => ProtoWriter.MeasureUInt32(value),
                _ => -1,
            };

        uint ISerializer<uint>.Read(ref ProtoReader.State state, uint value) => state.ReadUInt32();
        void ISerializer<uint>.Write(ref ProtoWriter.State state, uint value) => state.WriteUInt32(value);
        SerializerFeatures ISerializer<uint>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
        int IMeasuringSerializer<uint>.Measure(ISerializationContext context, WireType wireType, uint value)
            => wireType switch
            {
                WireType.Fixed32 => 4,
                WireType.Fixed64 => 8,
                WireType.Varint => ProtoWriter.MeasureUInt32(value),
                _ => -1,
            };

        ulong ISerializer<ulong>.Read(ref ProtoReader.State state, ulong value) => state.ReadUInt64();
        void ISerializer<ulong>.Write(ref ProtoWriter.State state, ulong value) => state.WriteUInt64(value);
        SerializerFeatures ISerializer<ulong>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
        int IMeasuringSerializer<ulong>.Measure(ISerializationContext context, WireType wireType, ulong value)
            => wireType switch
            {
                WireType.Fixed32 => 4,
                WireType.Fixed64 => 8,
                WireType.Varint => ProtoWriter.MeasureUInt64(value),
                _ => -1,
            };

        long ISerializer<long>.Read(ref ProtoReader.State state, long value) => state.ReadInt64();
        void ISerializer<long>.Write(ref ProtoWriter.State state, long value) => state.WriteInt64(value);
        SerializerFeatures ISerializer<long>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
        int IMeasuringSerializer<long>.Measure(ISerializationContext context, WireType wireType, long value)
            => wireType switch
            {
                WireType.Fixed32 => 4,
                WireType.Fixed64 => 8,
                WireType.Varint => ProtoWriter.MeasureUInt64((ulong)value),
                WireType.SignedVarint => ProtoWriter.MeasureUInt64(ProtoWriter.Zig(value)),
                _ => -1,
            };

        bool ISerializer<bool>.Read(ref ProtoReader.State state, bool value) => state.ReadBoolean();
        void ISerializer<bool>.Write(ref ProtoWriter.State state, bool value) => state.WriteBoolean(value);
        SerializerFeatures ISerializer<bool>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
        int IMeasuringSerializer<bool>.Measure(ISerializationContext context, WireType wireType, bool value)
            => wireType switch
            {
                WireType.Fixed32 => 4,
                WireType.Fixed64 => 8,
                WireType.Varint => 1,
                _ => -1,
            };

        float ISerializer<float>.Read(ref ProtoReader.State state, float value) => state.ReadSingle();
        void ISerializer<float>.Write(ref ProtoWriter.State state, float value) => state.WriteSingle(value);
        SerializerFeatures ISerializer<float>.Features => SerializerFeatures.WireTypeFixed32 | SerializerFeatures.CategoryScalar;
        int IMeasuringSerializer<float>.Measure(ISerializationContext context, WireType wireType, float value)
            => wireType switch
            {
                WireType.Fixed32 => 4,
                WireType.Fixed64 => 8,
                _ => -1,
            };

        double ISerializer<double>.Read(ref ProtoReader.State state, double value) => state.ReadDouble();
        void ISerializer<double>.Write(ref ProtoWriter.State state, double value) => state.WriteDouble(value);
        SerializerFeatures ISerializer<double>.Features => SerializerFeatures.WireTypeFixed64 | SerializerFeatures.CategoryScalar;
        int IMeasuringSerializer<double>.Measure(ISerializationContext context, WireType wireType, double value)
            => wireType switch
            {
                WireType.Fixed32 => 4,
                WireType.Fixed64 => 8,
                _ => -1,
            };

        sbyte ISerializer<sbyte>.Read(ref ProtoReader.State state, sbyte value) => state.ReadSByte();
        void ISerializer<sbyte>.Write(ref ProtoWriter.State state, sbyte value) => state.WriteSByte(value);
        SerializerFeatures ISerializer<sbyte>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
        int IMeasuringSerializer<sbyte>.Measure(ISerializationContext context, WireType wireType, sbyte value)
            => wireType switch
            {
                WireType.Fixed32 => 4,
                WireType.Fixed64 => 8,
                WireType.Varint => value < 0 ? 10 : ProtoWriter.MeasureUInt32((uint)value),
                WireType.SignedVarint => ProtoWriter.MeasureUInt32(ProtoWriter.Zig(value)),
                _ => -1,
            };

        short ISerializer<short>.Read(ref ProtoReader.State state, short value) => state.ReadInt16();
        void ISerializer<short>.Write(ref ProtoWriter.State state, short value) => state.WriteInt16(value);
        SerializerFeatures ISerializer<short>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
        int IMeasuringSerializer<short>.Measure(ISerializationContext context, WireType wireType, short value)
            => wireType switch
            {
                WireType.Fixed32 => 4,
                WireType.Fixed64 => 8,
                WireType.Varint => value < 0 ? 10 : ProtoWriter.MeasureUInt32((uint)value),
                WireType.SignedVarint => ProtoWriter.MeasureUInt32(ProtoWriter.Zig(value)),
                _ => -1,
            };

        Uri ISerializer<Uri>.Read(ref ProtoReader.State state, Uri value)
        {
            var uri = state.ReadString();
            return string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute);
        }
        void ISerializer<Uri>.Write(ref ProtoWriter.State state, Uri value) => state.WriteString(value.OriginalString);
        SerializerFeatures ISerializer<Uri>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;
        int IMeasuringSerializer<Uri>.Measure(ISerializationContext context, WireType wireType, Uri value)
            => wireType switch
            {
                WireType.String => ProtoWriter.UTF8.GetByteCount(value.OriginalString),
                _ => -1,
            };

        char ISerializer<char>.Read(ref ProtoReader.State state, char value) => (char)state.ReadUInt16();
        void ISerializer<char>.Write(ref ProtoWriter.State state, char value) => state.WriteUInt16(value);
        SerializerFeatures ISerializer<char>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
        int IMeasuringSerializer<char>.Measure(ISerializationContext context, WireType wireType, char value)
            => wireType switch {
            WireType.Fixed32 => 4,
            WireType.Fixed64 => 8,
            WireType.Varint => ProtoWriter.MeasureUInt32(value),
            _ => -1,
        };

        string IFactory<string>.Create(ISerializationContext context) => "";

        byte[] IFactory<byte[]>.Create(ISerializationContext context) => Array.Empty<byte>();

        SerializerFeatures ISerializer<int?>.Features => ((ISerializer<int>)this).Features;
        void ISerializer<int?>.Write(ref ProtoWriter.State state, int? value) => ((ISerializer<int>)this).Write(ref state, value.Value);
        int? ISerializer<int?>.Read(ref ProtoReader.State state, int? value) => ((ISerializer<int>)this).Read(ref state, value.GetValueOrDefault());

        SerializerFeatures ISerializer<short?>.Features => ((ISerializer<short>)this).Features;
        void ISerializer<short?>.Write(ref ProtoWriter.State state, short? value) => ((ISerializer<short>)this).Write(ref state, value.Value);
        short? ISerializer<short?>.Read(ref ProtoReader.State state, short? value) => ((ISerializer<short>)this).Read(ref state, value.GetValueOrDefault());

        SerializerFeatures ISerializer<long?>.Features => ((ISerializer<long>)this).Features;
        void ISerializer<long?>.Write(ref ProtoWriter.State state, long? value) => ((ISerializer<long>)this).Write(ref state, value.Value);
        long? ISerializer<long?>.Read(ref ProtoReader.State state, long? value) => ((ISerializer<long>)this).Read(ref state, value.GetValueOrDefault());

        SerializerFeatures ISerializer<sbyte?>.Features => ((ISerializer<sbyte>)this).Features;
        void ISerializer<sbyte?>.Write(ref ProtoWriter.State state, sbyte? value) => ((ISerializer<sbyte>)this).Write(ref state, value.Value);
        sbyte? ISerializer<sbyte?>.Read(ref ProtoReader.State state, sbyte? value) => ((ISerializer<sbyte>)this).Read(ref state, value.GetValueOrDefault());

        SerializerFeatures ISerializer<uint?>.Features => ((ISerializer<uint>)this).Features;
        void ISerializer<uint?>.Write(ref ProtoWriter.State state, uint? value) => ((ISerializer<uint>)this).Write(ref state, value.Value);
        uint? ISerializer<uint?>.Read(ref ProtoReader.State state, uint? value) => ((ISerializer<uint>)this).Read(ref state, value.GetValueOrDefault());

        SerializerFeatures ISerializer<ushort?>.Features => ((ISerializer<ushort>)this).Features;
        void ISerializer<ushort?>.Write(ref ProtoWriter.State state, ushort? value) => ((ISerializer<ushort>)this).Write(ref state, value.Value);
        ushort? ISerializer<ushort?>.Read(ref ProtoReader.State state, ushort? value) => ((ISerializer<ushort>)this).Read(ref state, value.GetValueOrDefault());

        SerializerFeatures ISerializer<ulong?>.Features => ((ISerializer<ulong>)this).Features;
        void ISerializer<ulong?>.Write(ref ProtoWriter.State state, ulong? value) => ((ISerializer<ulong>)this).Write(ref state, value.Value);
        ulong? ISerializer<ulong?>.Read(ref ProtoReader.State state, ulong? value) => ((ISerializer<ulong>)this).Read(ref state, value.GetValueOrDefault());

        SerializerFeatures ISerializer<byte?>.Features => ((ISerializer<byte>)this).Features;
        void ISerializer<byte?>.Write(ref ProtoWriter.State state, byte? value) => ((ISerializer<byte>)this).Write(ref state, value.Value);
        byte? ISerializer<byte?>.Read(ref ProtoReader.State state, byte? value) => ((ISerializer<byte>)this).Read(ref state, value.GetValueOrDefault());

        SerializerFeatures ISerializer<char?>.Features => ((ISerializer<char>)this).Features;
        void ISerializer<char?>.Write(ref ProtoWriter.State state, char? value) => ((ISerializer<char>)this).Write(ref state, value.Value);
        char? ISerializer<char?>.Read(ref ProtoReader.State state, char? value) => ((ISerializer<char>)this).Read(ref state, value.GetValueOrDefault());

        SerializerFeatures ISerializer<bool?>.Features => ((ISerializer<bool>)this).Features;
        void ISerializer<bool?>.Write(ref ProtoWriter.State state, bool? value) => ((ISerializer<bool>)this).Write(ref state, value.Value);
        bool? ISerializer<bool?>.Read(ref ProtoReader.State state, bool? value) => ((ISerializer<bool>)this).Read(ref state, value.GetValueOrDefault());

        SerializerFeatures ISerializer<float?>.Features => ((ISerializer<float>)this).Features;
        void ISerializer<float?>.Write(ref ProtoWriter.State state, float? value) => ((ISerializer<float>)this).Write(ref state, value.Value);
        float? ISerializer<float?>.Read(ref ProtoReader.State state, float? value) => ((ISerializer<float>)this).Read(ref state, value.GetValueOrDefault());

        SerializerFeatures ISerializer<double?>.Features => ((ISerializer<double>)this).Features;
        void ISerializer<double?>.Write(ref ProtoWriter.State state, double? value) => ((ISerializer<double>)this).Write(ref state, value.Value);
        double? ISerializer<double?>.Read(ref ProtoReader.State state, double? value) => ((ISerializer<double>)this).Read(ref state, value.GetValueOrDefault());

        Type ISerializer<Type>.Read(ref ProtoReader.State state, Type value) => state.ReadType();
        void ISerializer<Type>.Write(ref ProtoWriter.State state, Type value) => state.WriteType(value);
        SerializerFeatures ISerializer<Type>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;
        int IMeasuringSerializer<Type>.Measure(ISerializationContext context, WireType wireType, Type value)
            => wireType switch
            {
                WireType.String => Encoding.UTF8.GetByteCount(TypeModel.SerializeType(context?.Model, value)),
                _ => -1,
            };


        bool IValueChecker<string>.HasNonTrivialValue(string value) => value is not null; //  note: we write "" (when found), for compat
        bool IValueChecker<Uri>.HasNonTrivialValue(Uri value) => value?.OriginalString is not null; //  note: we write "" (when found), for compat
        bool IValueChecker<Type>.HasNonTrivialValue(Type value) => value is not null;
        bool IValueChecker<byte[]>.HasNonTrivialValue(byte[] value) => value is not null;  //  note: we write [] (when found), for compat
        bool IValueChecker<sbyte>.HasNonTrivialValue(sbyte value) => value != 0;
        bool IValueChecker<short>.HasNonTrivialValue(short value) => value != 0;
        bool IValueChecker<int>.HasNonTrivialValue(int value) => value != 0;
        bool IValueChecker<long>.HasNonTrivialValue(long value) => value != 0;
        bool IValueChecker<byte>.HasNonTrivialValue(byte value) => value != 0;
        bool IValueChecker<ushort>.HasNonTrivialValue(ushort value) => value != 0;
        bool IValueChecker<uint>.HasNonTrivialValue(uint value) => value != 0;
        bool IValueChecker<ulong>.HasNonTrivialValue(ulong value) => value != 0;
        bool IValueChecker<char>.HasNonTrivialValue(char value) => value != 0;
        bool IValueChecker<bool>.HasNonTrivialValue(bool value) => value;
        bool IValueChecker<float>.HasNonTrivialValue(float value) => value != 0;
        bool IValueChecker<double>.HasNonTrivialValue(double value) => value != 0;
        bool IValueChecker<sbyte>.IsNull(sbyte value) => false;
        bool IValueChecker<short>.IsNull(short value) => false;
        bool IValueChecker<int>.IsNull(int value) => false;
        bool IValueChecker<long>.IsNull(long value) => false;
        bool IValueChecker<byte>.IsNull(byte value) => false;
        bool IValueChecker<ushort>.IsNull(ushort value) => false;
        bool IValueChecker<uint>.IsNull(uint value) => false;
        bool IValueChecker<ulong>.IsNull(ulong value) => false;
        bool IValueChecker<char>.IsNull(char value) => false;
        bool IValueChecker<bool>.IsNull(bool value) => false;
        bool IValueChecker<float>.IsNull(float value) => false;
        bool IValueChecker<double>.IsNull(double value) => false;
        bool IValueChecker<string>.IsNull(string value) => value is null;
        bool IValueChecker<byte[]>.IsNull(byte[] value) => value is null;
        bool IValueChecker<Uri>.IsNull(Uri value) => value is null;
        bool IValueChecker<Type>.IsNull(Type value) => value is null;

        bool IValueChecker<int?>.HasNonTrivialValue(int? value) => value.GetValueOrDefault() != 0;
        bool IValueChecker<int?>.IsNull(int? value) => !value.HasValue;
        bool IValueChecker<uint?>.HasNonTrivialValue(uint? value) => value.GetValueOrDefault() != 0;
        bool IValueChecker<uint?>.IsNull(uint? value) => !value.HasValue;
        bool IValueChecker<short?>.HasNonTrivialValue(short? value) => value.GetValueOrDefault() != 0;
        bool IValueChecker<short?>.IsNull(short? value) => !value.HasValue;
        bool IValueChecker<ushort?>.HasNonTrivialValue(ushort? value) => value.GetValueOrDefault() != 0;
        bool IValueChecker<ushort?>.IsNull(ushort? value) => !value.HasValue;
        bool IValueChecker<long?>.HasNonTrivialValue(long? value) => value.GetValueOrDefault() != 0;
        bool IValueChecker<long?>.IsNull(long? value) => !value.HasValue;
        bool IValueChecker<ulong?>.HasNonTrivialValue(ulong? value) => value.GetValueOrDefault() != 0;
        bool IValueChecker<ulong?>.IsNull(ulong? value) => !value.HasValue;
        bool IValueChecker<float?>.HasNonTrivialValue(float? value) => value.GetValueOrDefault() != 0;
        bool IValueChecker<float?>.IsNull(float? value) => !value.HasValue;
        bool IValueChecker<double?>.HasNonTrivialValue(double? value) => value.GetValueOrDefault() != 0;
        bool IValueChecker<double?>.IsNull(double? value) => !value.HasValue;
        bool IValueChecker<byte?>.HasNonTrivialValue(byte? value) => value.GetValueOrDefault() != 0;
        bool IValueChecker<byte?>.IsNull(byte? value) => !value.HasValue;
        bool IValueChecker<sbyte?>.HasNonTrivialValue(sbyte? value) => value.GetValueOrDefault() != 0;
        bool IValueChecker<sbyte?>.IsNull(sbyte? value) => !value.HasValue;
        bool IValueChecker<bool?>.HasNonTrivialValue(bool? value) => value.GetValueOrDefault();
        bool IValueChecker<bool?>.IsNull(bool? value) => !value.HasValue;
        bool IValueChecker<char?>.HasNonTrivialValue(char? value) => value.GetValueOrDefault() != 0;
        bool IValueChecker<char?>.IsNull(char? value) => !value.HasValue;

        int IMeasuringSerializer<int?>.Measure(ISerializationContext context, WireType wireType, int? value)
            => ((IMeasuringSerializer<int>)this).Measure(context, wireType, value.Value);
        int IMeasuringSerializer<long?>.Measure(ISerializationContext context, WireType wireType, long? value)
            => ((IMeasuringSerializer<long>)this).Measure(context, wireType, value.Value);
        int IMeasuringSerializer<float?>.Measure(ISerializationContext context, WireType wireType, float? value)
            => ((IMeasuringSerializer<float>)this).Measure(context, wireType, value.Value);
        int IMeasuringSerializer<double?>.Measure(ISerializationContext context, WireType wireType, double? value)
            => ((IMeasuringSerializer<double>)this).Measure(context, wireType, value.Value);
        int IMeasuringSerializer<byte?>.Measure(ISerializationContext context, WireType wireType, byte? value)
            => ((IMeasuringSerializer<byte>)this).Measure(context, wireType, value.Value);
        int IMeasuringSerializer<ushort?>.Measure(ISerializationContext context, WireType wireType, ushort? value)
            => ((IMeasuringSerializer<ushort>)this).Measure(context, wireType, value.Value);
        int IMeasuringSerializer<uint?>.Measure(ISerializationContext context, WireType wireType, uint? value)
            => ((IMeasuringSerializer<uint>)this).Measure(context, wireType, value.Value);
        int IMeasuringSerializer<ulong?>.Measure(ISerializationContext context, WireType wireType, ulong? value)
            => ((IMeasuringSerializer<ulong>)this).Measure(context, wireType, value.Value);
        int IMeasuringSerializer<sbyte?>.Measure(ISerializationContext context, WireType wireType, sbyte? value)
            => ((IMeasuringSerializer<sbyte>)this).Measure(context, wireType, value.Value);
        int IMeasuringSerializer<short?>.Measure(ISerializationContext context, WireType wireType, short? value)
            => ((IMeasuringSerializer<short>)this).Measure(context, wireType, value.Value);
        int IMeasuringSerializer<char?>.Measure(ISerializationContext context, WireType wireType, char? value)
            => ((IMeasuringSerializer<char>)this).Measure(context, wireType, value.Value);
        int IMeasuringSerializer<bool?>.Measure(ISerializationContext context, WireType wireType, bool? value)
            => ((IMeasuringSerializer<bool>)this).Measure(context, wireType, value.Value);


        SerializerFeatures ISerializer<IntPtr>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
        SerializerFeatures ISerializer<UIntPtr>.Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
        SerializerFeatures ISerializer<IntPtr?>.Features => ((ISerializer<IntPtr>)this).Features;
        SerializerFeatures ISerializer<UIntPtr?>.Features => ((ISerializer<UIntPtr>)this).Features;

        void ISerializer<IntPtr?>.Write(ref ProtoWriter.State state, IntPtr? value) => ((ISerializer<IntPtr>)this).Write(ref state, value.Value);
        IntPtr? ISerializer<IntPtr?>.Read(ref ProtoReader.State state, IntPtr? value) => ((ISerializer<IntPtr>)this).Read(ref state, value.GetValueOrDefault());
        void ISerializer<UIntPtr?>.Write(ref ProtoWriter.State state, UIntPtr? value) => ((ISerializer<UIntPtr>)this).Write(ref state, value.Value);
        UIntPtr? ISerializer<UIntPtr?>.Read(ref ProtoReader.State state, UIntPtr? value) => ((ISerializer<UIntPtr>)this).Read(ref state, value.GetValueOrDefault());

        void ISerializer<IntPtr>.Write(ref ProtoWriter.State state, IntPtr value) => state.WriteIntPtr(value);
        IntPtr ISerializer<IntPtr>.Read(ref ProtoReader.State state, IntPtr value) => state.ReadIntPtr();
        void ISerializer<UIntPtr>.Write(ref ProtoWriter.State state, UIntPtr value) => state.WriteUIntPtr(value);
        UIntPtr ISerializer<UIntPtr>.Read(ref ProtoReader.State state, UIntPtr value) => state.ReadUIntPtr();

        int IMeasuringSerializer<IntPtr>.Measure(ISerializationContext context, WireType wireType, IntPtr value)
            => ((IMeasuringSerializer<long>)this).Measure(context, wireType, value.ToInt64());

        int IMeasuringSerializer<UIntPtr>.Measure(ISerializationContext context, WireType wireType, UIntPtr value)
            => ((IMeasuringSerializer<ulong>)this).Measure(context, wireType, value.ToUInt64());

        int IMeasuringSerializer<IntPtr?>.Measure(ISerializationContext context, WireType wireType, IntPtr? value)
            => ((IMeasuringSerializer<long>)this).Measure(context, wireType, value.Value.ToInt64());
        int IMeasuringSerializer<UIntPtr?>.Measure(ISerializationContext context, WireType wireType, UIntPtr? value)
            => ((IMeasuringSerializer<ulong>)this).Measure(context, wireType, value.Value.ToUInt64());

        bool IValueChecker<IntPtr>.HasNonTrivialValue(IntPtr value) => value != IntPtr.Zero;
        bool IValueChecker<IntPtr>.IsNull(IntPtr value) => false;

        bool IValueChecker<UIntPtr>.HasNonTrivialValue(UIntPtr value) => value != UIntPtr.Zero;
        bool IValueChecker<UIntPtr>.IsNull(UIntPtr value) => false;

        bool IValueChecker<IntPtr?>.HasNonTrivialValue(IntPtr? value) => value.GetValueOrDefault() != IntPtr.Zero;
        bool IValueChecker<IntPtr?>.IsNull(IntPtr? value) => !value.HasValue;

        bool IValueChecker<UIntPtr?>.HasNonTrivialValue(UIntPtr? value) => value.GetValueOrDefault() != UIntPtr.Zero;
        bool IValueChecker<UIntPtr?>.IsNull(UIntPtr? value) => !value.HasValue;

    }
}
