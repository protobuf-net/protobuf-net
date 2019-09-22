using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.IO;

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
                WriteStringWithLengthPrefix(value, map);
            }

            private void WriteStringWithLengthPrefix(string value, StringMap map)
            {
                var writer = _writer;
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
            }
            /// <summary>
            /// Writes a string to the stream; supported wire-types: String
            /// </summary>
            public void WriteString(string value, StringMap map = null)
            {
                switch (_writer.WireType)
                {
                    case WireType.String:
                        WriteStringWithLengthPrefix(value, map);
                        break;
                    default:
                        ThrowException(_writer);
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
                    WriteHeaderCore(fieldNumber, wireType);
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
                WriteInt32VarintImpl(value);
            }

            private void WriteInt32VarintImpl(int value)
            {
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
                        WriteInt32VarintImpl(value);
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
                => WriteSubItem<T>(value, serializer, PrefixStyle.Base128, recursionCheck);

            /// <summary>
            /// Writes a sub-item to the writer
            /// </summary>
            public void WriteSubItem<T>(int fieldNumber, T value, IProtoSerializer<T> serializer = null, bool recursionCheck = true)
            {
                WriteFieldHeader(fieldNumber, WireType.String);
                WriteSubItem<T>(value, serializer, PrefixStyle.Base128, recursionCheck);
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
                => (serializer ?? TypeModel.GetSubTypeSerializer<T>(Model)).WriteSubType(ref this, value);

            internal TypeModel Model => _writer?.Model;

            internal WireType WireType
            {
                get => _writer.WireType;
                private set => _writer.WireType = value;
            }

            internal int FieldNumber
            {
                get => _writer.fieldNumber;
                private set => _writer.fieldNumber = value;
            }

            internal long GetPosition() => _writer._position64;

            internal ProtoWriter GetWriter() => _writer;

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
                SubItemToken token = StartSubItem(null);
                writer.model.Serialize(ref this, key, value);
                EndSubItem(token);
            }

            /// <summary>
            /// The serialization context associated with this instance
            /// </summary>
            public ISerializationContext Context => _writer;

            /// <summary>
            /// Writes a byte-array to the stream; supported wire-types: String
            /// </summary>
            public void WriteBytes(System.Buffers.ReadOnlySequence<byte> data)
            {
                var writer = _writer;
                int length = checked((int)data.Length);
                switch (writer.WireType)
                {
                    case WireType.Fixed32:
                        if (length != 4) ThrowHelper.ThrowArgumentException(nameof(length));
                        writer.ImplWriteBytes(ref this, data);
                        writer.AdvanceAndReset(4);
                        return;
                    case WireType.Fixed64:
                        if (length != 8) ThrowHelper.ThrowArgumentException(nameof(length));
                        writer.ImplWriteBytes(ref this, data);
                        writer.AdvanceAndReset(8);
                        return;
                    case WireType.String:
                        writer.AdvanceAndReset(writer.ImplWriteVarint32(ref this, (uint)length) + length);
                        if (length == 0) return;
                        writer.ImplWriteBytes(ref this, data);
                        break;
                    default:
                        ThrowException(writer);
                        break;
                }
            }

            /// <summary>
            /// Writes a byte-array to the stream; supported wire-types: String
            /// </summary>
            public void WriteBytes(byte[] data, int offset, int length)
            {
                var writer = _writer;
                switch (writer.WireType)
                {
                    case WireType.Fixed32:
                        if (length != 4) ThrowHelper.ThrowArgumentException(nameof(length));
                        writer.ImplWriteBytes(ref this, data, offset, 4);
                        writer.AdvanceAndReset(4);
                        return;
                    case WireType.Fixed64:
                        if (length != 8) ThrowHelper.ThrowArgumentException(nameof(length));
                        writer.ImplWriteBytes(ref this, data, offset, 8);
                        writer.AdvanceAndReset(8);
                        return;
                    case WireType.String:
                        writer.AdvanceAndReset(writer.ImplWriteVarint32(ref this, (uint)length) + length);
                        if (length == 0) return;
                        writer.ImplWriteBytes(ref this, data, offset, length);
                        break;
                    default:
                        ThrowException(writer);
                        break;
                }
            }

            /// <summary>
            /// Writes a byte-array to the stream; supported wire-types: String
            /// </summary>
            public void WriteBytes(byte[] data) => WriteBytes(data, 0, data.Length);

            /// <summary>
            /// Writes an object to the input writer
            /// </summary>
            public long Serialize<T>(T value, IProtoSerializer<T> serializer = null)
            {
                try
                {
                    CheckClear();
                    long before = GetPosition();
                    if (value != null)
                    {
                        SetRootObject(value);
                        (serializer ?? TypeModel.GetSerializer<T>(Model)).Write(ref this, value);
                    }
                    CheckClear();
                    long after = GetPosition();
                    return after - before;
                }
                catch
                {
                    Abandon();
                    throw;
                }
            }

            /// <summary>
            /// Specifies a known root object to use during reference-tracked serialization
            /// </summary>
            public void SetRootObject(object value) => _writer.SetRootObject(value);

            public void Abandon() => _writer?.Abandon();

            void CheckClear() => _writer?.CheckClear(ref this);

            /// <summary>
            /// Used for packed encoding; writes the length prefix using fixed sizes rather than using
            /// buffering. Only valid for fixed-32 and fixed-64 encoding.
            /// </summary>
            public void WritePackedPrefix(int elementCount, WireType wireType)
            {
                if (WireType != WireType.String) ThrowHelper.ThrowInvalidOperationException("Invalid wire-type: " + WireType);
                if (elementCount < 0) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(elementCount));
                ulong bytes;
                switch (wireType)
                {
                    // use long in case very large arrays are enabled
                    case WireType.Fixed32: bytes = ((ulong)elementCount) << 2; break; // x4
                    case WireType.Fixed64: bytes = ((ulong)elementCount) << 3; break; // x8
                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(wireType), "Invalid wire-type: " + wireType);
                        bytes = default;
                        break;
                };
                int prefixLength = _writer.ImplWriteVarint64(ref this, bytes);
                _writer.AdvanceAndReset(prefixLength);
            }

            /// <summary>
            /// Write an encapsulated sub-object, using the supplied unique key (reprasenting a type).
            /// </summary>
            /// <param name="value">The object to write.</param>
            /// <param name="key">The key that uniquely identifies the type within the model.</param>
            /// <param name="writer">The destination.</param>
            /// <param name="state">Writer state</param>
            public void WriteObject(object value, int key)
            {
                var model = Model;
                if (model == null)
                {
                    ThrowHelper.ThrowInvalidOperationException("Cannot serialize sub-objects unless a model is provided");
                }

                SubItemToken token = StartSubItem(value);
                if (key >= 0)
                {
                    model.Serialize(ref this, key, value);
                }
                else if (model != null && model.TrySerializeAuxiliaryType(ref this, value.GetType(), DataFormat.Default, TypeModel.ListItemTag, value, false, null))
                {
                    // all ok
                }
                else
                {
                    TypeModel.ThrowUnexpectedType(value.GetType());
                }
                EndSubItem(token);
            }

            internal void WriteObject(object value, int key, PrefixStyle style, int fieldNumber)
            {
                var model = Model;
                if (model == null)
                {
                    ThrowHelper.ThrowInvalidOperationException("Cannot serialize sub-objects unless a model is provided");
                }
                if (WireType != WireType.None) ThrowInvalidSerializationOperation();

                switch (style)
                {
                    case PrefixStyle.Base128:
                        WireType = WireType.String;
                        FieldNumber = fieldNumber;
                        if (fieldNumber > 0) WriteHeaderCore(fieldNumber, WireType.String);
                        break;
                    case PrefixStyle.Fixed32:
                    case PrefixStyle.Fixed32BigEndian:
                        FieldNumber = 0;
                        WireType = WireType.Fixed32;
                        break;
                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(style));
                        break;
                }
                SubItemToken token = StartSubItem(value, style);
                if (key < 0)
                {
                    if (!model.TrySerializeAuxiliaryType(ref this, value.GetType(), DataFormat.Default, TypeModel.ListItemTag, value, false, null))
                    {
                        TypeModel.ThrowUnexpectedType(value.GetType());
                    }
                }
                else
                {
                    model.Serialize(ref this, key, value);
                }
                EndSubItem(token, style);
            }
            internal void WriteHeaderCore(int fieldNumber, WireType wireType)
            {
                uint header = (((uint)fieldNumber) << 3)
                    | (((uint)wireType) & 7);
                int bytes = _writer.ImplWriteVarint32(ref this, header);
                _writer.Advance(bytes);
            }

            /// <summary>
            /// Indicates the start of a nested record.
            /// </summary>
            /// <param name="instance">The instance to write.</param>
            /// <param name="writer">The destination.</param>
            /// <param name="state">Writer state</param>
            /// <returns>A token representing the state of the stream; this token is given to EndSubItem.</returns>
            [Obsolete(PreferWriteSubItem, false)]
            public SubItemToken StartSubItem(object instance) => StartSubItem(instance, PrefixStyle.Base128);

            /// <summary>
            /// Releases any resources associated with this instance
            /// </summary>
            public void Dispose()
            {
                var writer = _writer;
                _writer = null;
                writer?.Dispose();
            }

            [Obsolete(PreferWriteSubItem, false)]
            internal SubItemToken StartSubItem(object instance, PrefixStyle style)
            {
                _writer.PreSubItem(instance);
                switch (WireType)
                {
                    case WireType.StartGroup:
                        WireType = WireType.None;
                        return new SubItemToken((long)(-FieldNumber));
                    case WireType.Fixed32:
                        switch (style)
                        {
                            case PrefixStyle.Fixed32:
                            case PrefixStyle.Fixed32BigEndian:
                                break; // OK
                            default:
                                ThrowInvalidSerializationOperation();
                                return default;
                        }
                        goto case WireType.String;
                    case WireType.String:
#if DEBUG
                        if (Model != null && Model.ForwardsOnly)
                        {
                            ThrowHelper.ThrowProtoException("Should not be buffering data: " + instance ?? "(null)");
                        }
#endif
                        return _writer.ImplStartLengthPrefixedSubItem(ref this, instance, style);
                    default:
                        ThrowInvalidSerializationOperation();
                        return default;
                }
            }

            [Obsolete(PreferWriteSubItem, false)]
            internal void EndSubItem(SubItemToken token, PrefixStyle style)
            {
                _writer.PostSubItem(ref this);
                int value = (int)token.value64;
                if (value < 0)
                {   // group - very simple append
                    WriteHeaderCore(-value, WireType.EndGroup);
                    WireType = WireType.None;
                }
                else
                {
                    _writer.ImplEndLengthPrefixedSubItem(ref this, token, style);
                }
            }

            /// <summary>
            /// Flushes data to the underlying stream, and releases any resources. The underlying stream is *not* disposed
            /// by this operation.
            /// </summary>
            public void Close()
            {
                CheckClear();
                _writer?.Cleanup();
            }

            /// <summary>
            /// Indicates the end of a nested record.
            /// </summary>
            /// <param name="token">The token obtained from StartubItem.</param>
            /// <param name="writer">The destination.</param>
            /// <param name="state">Writer state</param>
            [Obsolete(PreferWriteSubItem, false)]
            public void EndSubItem(SubItemToken token)
                => EndSubItem(token, PrefixStyle.Base128);

            /// <summary>
            /// Copies any extension data stored for the instance to the underlying stream
            /// </summary>
            public void AppendExtensionData(IExtensible instance)
            {
                if (instance == null) ThrowHelper.ThrowArgumentNullException(nameof(instance));
                // we expect the writer to be raw here; the extension data will have the
                // header detail, so we'll copy it implicitly
                if (WireType != WireType.None) ThrowInvalidSerializationOperation();

                IExtension extn = instance.GetExtensionObject(false);
                if (extn != null)
                {
                    // unusually we *don't* want "using" here; the "finally" does that, with
                    // the extension object being responsible for disposal etc
                    Stream source = extn.BeginQuery();
                    try
                    {
                        if (ProtoReader.TryConsumeSegmentRespectingPosition(source, out var data, ProtoReader.TO_EOF))
                        {
                            _writer.ImplWriteBytes(ref this, data.Array, data.Offset, data.Count);
                            _writer.Advance(data.Count);
                        }
                        else
                        {
                            _writer.ImplCopyRawFromStream(ref this, source);
                        }
                    }
                    finally { extn.EndQuery(source); }
                }
            }

            // general purpose serialization exception message
            internal void ThrowInvalidSerializationOperation()
            {
                if (_writer == null) throw new ProtoException("No underlying writer");
                throw new ProtoException($"Invalid serialization operation with wire-type {WireType} at position {GetPosition()}");
            }

        }
    }
}
