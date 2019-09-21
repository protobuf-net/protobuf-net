using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
namespace ProtoBuf
{
    partial class ProtoWriter
    {
        ref partial struct State
        {
            /// <summary>
            /// Writes a string to the stream
            /// </summary>
            public void WriteString(int fieldNumber, string value, StringMap map = null)
            {
                WriteFieldHeader(fieldNumber, WireType.String);
                WriteString(value, map);
            }

            /// <summary>
            /// Writes a string to the stream; supported wire-types: String
            /// </summary>
            public void WriteString(string value, StringMap map = null)
            {
                var writer = _writer;
                switch (writer.WireType)
                {
                    case WireType.String:
                        if (string.IsNullOrEmpty(value))
                        {
                            writer.AdvanceAndReset(writer.ImplWriteVarint32(ref this, 0));
                        }
                        else
                        {
                            var len = UTF8.GetByteCount(value);
                            writer.AdvanceAndReset(writer.ImplWriteVarint32(ref this, (uint)len) + len);
                            writer.ImplWriteString(ref this, value, len);
                        }
                        break;
                    default:
                        ThrowException(writer);
                        break;
                }
            }

            /// <summary>
            /// Writes a Type to the stream, using the model's DynamicTypeFormatting if appropriate; supported wire-types: String
            /// </summary>
            public void WriteType(Type value)
            {
                WriteString(_writer.SerializeType(value));
            }

            /// <summary>
            /// Writes a field-header, indicating the format of the next data we plan to write.
            /// </summary>
            public void WriteFieldHeader(int fieldNumber, WireType wireType)
            {
                var writer = _writer;
                if (writer.WireType != WireType.None) FailPendingField(writer, wireType);
                if (fieldNumber < 0) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(fieldNumber));
                writer._needFlush = true;
                if (writer.packedFieldNumber == 0)
                {
                    writer.fieldNumber = fieldNumber;
                    writer.WireType = wireType;
                    WriteHeaderCore(fieldNumber, wireType, writer, ref this);
                }
                else
                {
                    WritePackedField(writer, fieldNumber, wireType);
                }

                static void FailPendingField(ProtoWriter writer, WireType wireType)
                {
                    ThrowHelper.ThrowInvalidOperationException("Cannot write a " + wireType.ToString()
                    + " header until the " + writer.WireType.ToString() + " data has been written");
                }
                static void WritePackedField(ProtoWriter writer, int fieldNumber, WireType wireType)
                {
                    if (writer.packedFieldNumber == fieldNumber)
                    { // we'll set things up, but note we *don't* actually write the header here
                        switch (wireType)
                        {
                            case WireType.Fixed32:
                            case WireType.Fixed64:
                            case WireType.Varint:
                            case WireType.SignedVarint:
                                break; // fine
                            default:
                                ThrowHelper.ThrowInvalidOperationException("Wire-type cannot be encoded as packed: " + wireType.ToString());
                                break;
                        }
                        writer.fieldNumber = fieldNumber;
                        writer.WireType = wireType;
                    }
                    else
                    {
                        ThrowHelper.ThrowInvalidOperationException("Field mismatch during packed encoding; expected " + writer.packedFieldNumber.ToString() + " but received " + fieldNumber.ToString());
                    }
                }
            }

            public void WriteInt32Varint(int fieldNumber, int value)
            {
                WriteFieldHeader(fieldNumber, WireType.Varint);
                var writer = _writer;
                if (value >= 0)
                {
                    writer.AdvanceAndReset(writer.ImplWriteVarint32(ref this, (uint)value));
                }
                else
                {
                    writer.AdvanceAndReset(writer.ImplWriteVarint64(ref this, (ulong)(long)value));
                }
            }

            /// <summary>
            /// Writes a signed 32-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
            /// </summary>
            public void WriteInt32(int value)
            {
                var writer = _writer;
                switch (writer.WireType)
                {
                    case WireType.Fixed32:
                        writer.ImplWriteFixed32(ref this, (uint)value);
                        writer.AdvanceAndReset(4);
                        return;
                    case WireType.Fixed64:
                        writer.ImplWriteFixed64(ref this, (ulong)(long)value);
                        writer.AdvanceAndReset(8);
                        return;
                    case WireType.Varint:
                        if (value >= 0)
                        {
                            writer.AdvanceAndReset(writer.ImplWriteVarint32(ref this, (uint)value));
                        }
                        else
                        {
                            writer.AdvanceAndReset(writer.ImplWriteVarint64(ref this, (ulong)(long)value));
                        }
                        return;
                    case WireType.SignedVarint:
                        writer.AdvanceAndReset(writer.ImplWriteVarint32(ref this, Zig(value)));
                        return;
                    default:
                        ThrowException(writer);
                        break;
                }
            }

            /// <summary>
            /// Writes a signed 8-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
            /// </summary>
            public void WriteSByte(sbyte value) => WriteInt32(value);

            /// <summary>
            /// Writes a signed 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
            /// </summary>
            public void WriteInt16(short value) => WriteInt32(value);

            /// <summary>
            /// Writes an unsigned 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
            /// </summary>
            public void WriteUInt16(ushort value) => WriteUInt32(value);

            /// <summary>
            /// Writes an unsigned 8-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
            /// </summary>
            public void WriteByte(byte value) => WriteUInt32(value);

            /// <summary>
            /// Writes a boolean to the stream; supported wire-types: Variant, Fixed32, Fixed64
            /// </summary>
            public void WriteBoolean(bool value) => WriteUInt32(value ? (uint)1 : (uint)0);

            /// <summary>
            /// Writes an unsigned 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
            /// </summary>
            public void WriteUInt32(uint value)
            {
                var writer = _writer;
                switch (writer.WireType)
                {
                    case WireType.Fixed32:
                        writer.ImplWriteFixed32(ref this, value);
                        writer.AdvanceAndReset(4);
                        return;
                    case WireType.Fixed64:
                        writer.ImplWriteFixed64(ref this, value);
                        writer.AdvanceAndReset(8);
                        return;
                    case WireType.Varint:
                        int bytes = writer.ImplWriteVarint32(ref this, value);
                        writer.AdvanceAndReset(bytes);
                        return;
                    default:
                        ThrowException(writer);
                        break;
                }
            }

            /// <summary>
            /// Writes a double-precision number to the stream; supported wire-types: Fixed32, Fixed64
            /// </summary>
            public void WriteDouble(double value)
            {
                var writer = _writer;
                switch (writer.WireType)
                {
                    case WireType.Fixed32:
                        float f = (float)value;
                        if (float.IsInfinity(f) && !double.IsInfinity(value))
                        {
                            ThrowHelper.ThrowOverflowException();
                        }
                        WriteSingle(f);
                        return;
                    case WireType.Fixed64:
                        unsafe { writer.ImplWriteFixed64(ref this, *(ulong*)&value); }
                        writer.AdvanceAndReset(8);
                        return;
                    default:
                        ThrowException(writer);
                        return;
                }
            }

            /// <summary>
            /// Writes a single-precision number to the stream; supported wire-types: Fixed32, Fixed64
            /// </summary>
            public void WriteSingle(float value)
            {
                var writer = _writer;
                switch (writer.WireType)
                {
                    case WireType.Fixed32:
                        unsafe { writer.ImplWriteFixed32(ref this, *(uint*)&value); }
                        writer.AdvanceAndReset(4);
                        return;
                    case WireType.Fixed64:
                        WriteDouble(value);
                        return;
                    default:
                        ThrowException(writer);
                        break;
                }
            }

            /// <summary>
            /// Writes a signed 64-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
            /// </summary>
            public void WriteInt64(long value)
            {
                var writer = _writer;
                switch (writer.WireType)
                {
                    case WireType.Fixed64:
                        writer.ImplWriteFixed64(ref this, (ulong)value);
                        writer.AdvanceAndReset(8);
                        return;
                    case WireType.Varint:
                        writer.AdvanceAndReset(writer.ImplWriteVarint64(ref this, (ulong)value));
                        return;
                    case WireType.SignedVarint:
                        writer.AdvanceAndReset(writer.ImplWriteVarint64(ref this, Zig(value)));
                        return;
                    case WireType.Fixed32:
                        writer.ImplWriteFixed32(ref this, checked((uint)(int)value));
                        writer.AdvanceAndReset(4);
                        return;
                    default:
                        ThrowException(writer);
                        break;
                }
            }

            /// <summary>
            /// Writes an unsigned 64-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
            /// </summary>
            public void WriteUInt64(ulong value)
            {
                var writer = _writer;
                switch (writer.WireType)
                {
                    case WireType.Fixed64:
                        writer.ImplWriteFixed64(ref this, value);
                        writer.AdvanceAndReset(8);
                        return;
                    case WireType.Varint:
                        int bytes = writer.ImplWriteVarint64(ref this, value);
                        writer.AdvanceAndReset(bytes);
                        return;
                    case WireType.Fixed32:
                        writer.ImplWriteFixed32(ref this, checked((uint)value));
                        writer.AdvanceAndReset(4);
                        return;
                    default:
                        ThrowException(writer);
                        break;
                }
            }

            /// <summary>
            /// Writes a sub-item to the writer
            /// </summary>
            public void WriteSubItem<T>(T value, IProtoSerializer<T> serializer = null, bool recursionCheck = true)
                => _writer.WriteSubItem<T>(ref this, value, serializer, PrefixStyle.Base128, recursionCheck);

            /// <summary>
            /// Writes a sub-item to the writer
            /// </summary>
            public void WriteSubItem<T>(int fieldNumber, T value, IProtoSerializer<T> serializer = null, bool recursionCheck = true)
            {
                WriteFieldHeader(fieldNumber, WireType.String);
                _writer.WriteSubItem<T>(ref this, value, serializer, PrefixStyle.Base128, recursionCheck);
            }

            /// <summary>
            /// Writes a sub-type to the input writer
            /// </summary>
            public void WriteSubType<T>(T value, IProtoSubTypeSerializer<T> serializer = null) where T : class
            {
                _writer.WriteSubType<T>(ref this, value, serializer ?? TypeModel.GetSubTypeSerializer<T>(Model));
            }

            /// <summary>
            /// Writes a sub-type to the input writer
            /// </summary>
            public void WriteSubType<T>(int fieldNumber, T value, IProtoSubTypeSerializer<T> serializer = null) where T : class
            {
                WriteFieldHeader(fieldNumber, WireType.String);
                _writer.WriteSubType<T>(ref this, value, serializer ?? TypeModel.GetSubTypeSerializer<T>(Model));
            }

            /// <summary>
            /// Writes a base-type to the input writer
            /// </summary>
            public void WriteBaseType<T>(T value, IProtoSubTypeSerializer<T> serializer = null) where T : class
                => (serializer ?? TypeModel.GetSubTypeSerializer<T>(Model)).WriteSubType(_writer, ref this, value);

            internal TypeModel Model => _writer?.Model;

            /// <summary>
            /// Write an encapsulated sub-object, using the supplied unique key (reprasenting a type) - but the
            /// caller is asserting that this relationship is non-recursive; no recursion check will be
            /// performed.
            /// </summary>
            /// <param name="value">The object to write.</param>
            /// <param name="key">The key that uniquely identifies the type within the model.</param>
            public void WriteRecursionSafeObject(object value, int key)
            {
                var writer = _writer;
                if (writer.model == null)
                {
                    ThrowHelper.ThrowInvalidOperationException("Cannot serialize sub-objects unless a model is provided");
                }
                SubItemToken token = StartSubItem(null, writer, ref this);
                writer.model.Serialize(writer, ref this, key, value);
                EndSubItem(token, writer, ref this);
            }
        }
    }
}
