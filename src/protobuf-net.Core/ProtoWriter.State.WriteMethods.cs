using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

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
                if (value != null)
                {
                    WriteFieldHeader(fieldNumber, WireType.String);
                    WriteStringWithLengthPrefix(value, map);
                }
            }

#pragma warning disable IDE0060 // map isn't implemented yet, but we definitely want it
            private void WriteStringWithLengthPrefix(string value, StringMap map)
#pragma warning restore IDE0060
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
                        ThrowInvalidSerializationOperation();
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
                if (writer.WireType != WireType.None) FailPendingField(writer, fieldNumber, wireType);
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

                static void FailPendingField(ProtoWriter writer, int fieldNumber, WireType wireType)
                {
                    ThrowHelper.ThrowInvalidOperationException($"Cannot write a {wireType}/{fieldNumber} header until the {writer.WireType}/{writer.fieldNumber} data has been written; writer: {writer}");
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

            /// <summary>
            /// Writes a signed 32-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
            /// </summary>
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
                        ThrowInvalidSerializationOperation();
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
                        ThrowInvalidSerializationOperation();
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
                        ThrowInvalidSerializationOperation();
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
                        ThrowInvalidSerializationOperation();
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
                        ThrowInvalidSerializationOperation();
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
                        ThrowInvalidSerializationOperation();
                        break;
                }
            }

            /// <summary>
            /// Writes a sub-item to the writer
            /// </summary>
            [MethodImpl(ProtoReader.HotPath)]
            public void WriteMessage<T>(SerializerFeatures features, T value, ISerializer<T> serializer = null)
                => _writer.WriteMessage<T>(ref this, value, serializer, PrefixStyle.Base128, features.ApplyRecursionCheck());

            /// <summary>
            /// Writes a sub-item to the writer
            /// </summary>
            [MethodImpl(ProtoReader.HotPath)]
            public void WriteMessage<T>(int fieldNumber, SerializerFeatures features, T value, ISerializer<T> serializer = null)
            {
                if (!(TypeHelper<T>.CanBeNull && value is null))
                {
                    WriteFieldHeader(fieldNumber, WireType.String);
                    _writer.WriteMessage<T>(ref this, value, serializer, PrefixStyle.Base128, features.ApplyRecursionCheck());
                }
            }

            /// <summary>
            /// Writes a sub-item to the writer
            /// </summary>
            public void WriteGroup<T>(int fieldNumber, SerializerFeatures features, T value, ISerializer<T> serializer = null)
            {
                if (!(TypeHelper<T>.CanBeNull && value is null))
                {
                    WriteFieldHeader(fieldNumber, WireType.StartGroup);
                    _writer.WriteMessage<T>(ref this, value, serializer, PrefixStyle.Base128, features.ApplyRecursionCheck());
                }
            }

            /// <summary>
            /// Writes a sequence of sub-items to the writer, with all default options
            /// </summary>
            public void WriteRepeated<T>(int fieldNumber, IEnumerable<T> values)
                => WriteRepeated<T>(fieldNumber, default, values, default);

            /// <summary>
            /// Writes a sequence of sub-items to the writer
            /// </summary>
            public void WriteRepeated<T>(int fieldNumber, SerializerFeatures features, IEnumerable<T> values, ISerializer<T> serializer = null)
            {
                serializer ??= TypeModel.GetSerializer<T>(Model);
                var serializerFeatures = serializer.Features;
                if (serializerFeatures.IsRepeated()) TypeModel.ThrowNestedListsNotSupported(typeof(T));
                var category = serializerFeatures.GetCategory();

                features.InheritFrom(serializerFeatures);
                var wireType = features.GetWireType();

                if (values == null) return; // nothing to do

                int count = -1;
                if (values is ICollection<T> collection)
                {
                    count = collection.Count;
                    if (count == 0) return;
                }

                if (TypeHelper<T>.CanBePacked && !features.IsPackedDisabled() && count > 1 && serializer is IMeasuringSerializer<T> measurer)
                {
                    if (category != SerializerFeatures.CategoryScalar) serializerFeatures.ThrowInvalidCategory();
                    if (values is T[] arr)
                    {   // exploit special JIT-based array handling
                        WritePackedScalar(fieldNumber, wireType, arr, count, measurer);
                    }
                    else if (values is List<T> list)
                    {   // exploit the custom List<T> iterator
                        WritePackedScalar(fieldNumber, wireType, list, count, measurer);
                    }
                    else
                    {   // just roll with it; it'll do
                        WritePackedScalar(fieldNumber, wireType, values, count, measurer);
                    }
                }
                else
                {
                    if (values is T[] arr)
                    {   // exploit special JIT-based array handling
                        WriteRepeatedCore(fieldNumber, category, wireType, arr, serializer);
                    }
                    else if (values is List<T> list)
                    {   // exploit the custom List<T> iterator
                        WriteRepeatedCore(fieldNumber, category, wireType, list, serializer);
                    }
                    else
                    {   // just roll with it; it'll do
                        WriteRepeatedCore(fieldNumber, category, wireType, values, serializer);
                    }
                }
            }

            private void WritePackedScalar<T>(int fieldNumber, WireType wireType, IEnumerable<T> values, int count, IMeasuringSerializer<T> serializer)
            {
                long expectedLength;
                switch (wireType)
                {
                    case WireType.Fixed32:
                        expectedLength = count * 4;
                        break;
                    case WireType.Fixed64:
                        expectedLength = count * 8;
                        break;
                    case WireType.Varint:
                    case WireType.SignedVarint:
                        expectedLength = 0;
                        var context = Context;
                        foreach (var value in values)
                        {
                            expectedLength += serializer.Measure(context, wireType, value);
                        }
                        break;
                    default:
                        ThrowHelper.ThrowInvalidOperationException($"Invalid wire-type for packed encoding: {wireType}");
                        expectedLength = default;
                        break;
                }

                WriteFieldHeader(fieldNumber, WireType.String);
                _writer.AdvanceAndReset(_writer.ImplWriteVarint64(ref this, (ulong)expectedLength));
                long before = GetPosition();
                foreach(var value in values)
                {
                    _writer.WireType = wireType; // tell the serializer what we want to do
                    serializer.Write(ref this, value);
                }
                long actualLength = GetPosition() - before;
                if (actualLength != expectedLength) ThrowHelper.ThrowInvalidOperationException(
                    $"packed encoding length miscalculation for {typeof(T).NormalizeName()}, {wireType}; expected {expectedLength}, got {actualLength}");
            }

            private void WritePackedScalar<T>(int fieldNumber, WireType wireType, List<T> values, int count, IMeasuringSerializer<T> serializer)
            {
                long expectedLength;
                switch (wireType)
                {
                    case WireType.Fixed32:
                        expectedLength = count * 4;
                        break;
                    case WireType.Fixed64:
                        expectedLength = count * 8;
                        break;
                    case WireType.Varint:
                    case WireType.SignedVarint:
                        expectedLength = 0;
                        var context = Context;
                        foreach (var value in values)
                        {
                            expectedLength += serializer.Measure(context, wireType, value);
                        }
                        break;
                    default:
                        ThrowHelper.ThrowInvalidOperationException($"Invalid wire-type for packed encoding: {wireType}");
                        expectedLength = default;
                        break;
                }

                WriteFieldHeader(fieldNumber, WireType.String);
                _writer.AdvanceAndReset(_writer.ImplWriteVarint64(ref this, (ulong)expectedLength));
                long before = GetPosition();
                foreach (var value in values)
                {
                    _writer.WireType = wireType; // tell the serializer what we want to do
                    serializer.Write(ref this, value);
                }
                long actualLength = GetPosition() - before;
                if (actualLength != expectedLength) ThrowHelper.ThrowInvalidOperationException(
                    $"packed encoding length miscalculation for {typeof(T).NormalizeName()}, {wireType}; expected {expectedLength}, got {actualLength}");
            }

            private void WritePackedScalar<T>(int fieldNumber, WireType wireType, T[] values, int count, IMeasuringSerializer<T> serializer)
            {
                long expectedLength;
                switch (wireType)
                {
                    case WireType.Fixed32:
                        expectedLength = count * 4;
                        break;
                    case WireType.Fixed64:
                        expectedLength = count * 8;
                        break;
                    case WireType.Varint:
                    case WireType.SignedVarint:
                        expectedLength = 0;
                        var context = Context;
                        for (int i = 0; i < values.Length; i++)
                        {
                            expectedLength += serializer.Measure(context, wireType, values[i]);
                        }
                        break;
                    default:
                        ThrowHelper.ThrowInvalidOperationException($"Invalid wire-type for packed encoding: {wireType}");
                        expectedLength = default;
                        break;
                }

                WriteFieldHeader(fieldNumber, WireType.String);
                _writer.AdvanceAndReset(_writer.ImplWriteVarint64(ref this, (ulong)expectedLength));
                long before = GetPosition();
                for (int i = 0; i < values.Length; i++)
                {
                    _writer.WireType = wireType; // tell the serializer what we want to do
                    serializer.Write(ref this, values[i]);
                }
                long actualLength = GetPosition() - before;
                if (actualLength != expectedLength) ThrowHelper.ThrowInvalidOperationException(
                    $"packed encoding length miscalculation for {typeof(T).NormalizeName()}, {wireType}; expected {expectedLength}, got {actualLength}");
            }

            private void WriteRepeatedCore<T>(int fieldNumber, SerializerFeatures category, WireType wireType, IEnumerable<T> values, ISerializer<T> serializer)
            {
                foreach (var value in values)
                {
                    if (TypeHelper<T>.CanBeNull && value is null) ThrowHelper.ThrowNullReferenceException<T>();
                    WriteFieldHeader(fieldNumber, wireType);
                    switch (category)
                    {
                        case SerializerFeatures.CategoryMessageWrappedAtRoot:
                        case SerializerFeatures.CategoryMessage:
                            _writer.WriteMessage<T>(ref this, value, serializer, PrefixStyle.Base128, true);
                            break;
                        case SerializerFeatures.CategoryScalar:
                            serializer.Write(ref this, value);
                            break;
                        default:
                            category.ThrowInvalidCategory();
                            break;
                    }
                }
            }

            private void WriteRepeatedCore<T>(int fieldNumber, SerializerFeatures category, WireType wireType, List<T> values, ISerializer<T> serializer)
            {
                foreach (var value in values)
                {
                    if (TypeHelper<T>.CanBeNull && value is null) ThrowHelper.ThrowNullReferenceException<T>();
                    WriteFieldHeader(fieldNumber, wireType);
                    switch (category)
                    {
                        case SerializerFeatures.CategoryMessageWrappedAtRoot:
                        case SerializerFeatures.CategoryMessage:
                            _writer.WriteMessage<T>(ref this, value, serializer, PrefixStyle.Base128, true);
                            break;
                        case SerializerFeatures.CategoryScalar:
                            serializer.Write(ref this, value);
                            break;
                        default:
                            category.ThrowInvalidCategory();
                            break;
                    }
                }
            }

            private void WriteRepeatedCore<T>(int fieldNumber, SerializerFeatures category, WireType wireType, T[] values, ISerializer<T> serializer)
            {
                for(int i = 0; i < values.Length; i++)
                {
                    ref T value = ref values[i];
                    if (TypeHelper<T>.CanBeNull && value is null) ThrowHelper.ThrowNullReferenceException<T>();
                    WriteFieldHeader(fieldNumber, wireType);
                    switch (category)
                    {
                        case SerializerFeatures.CategoryMessageWrappedAtRoot:
                        case SerializerFeatures.CategoryMessage:
                            _writer.WriteMessage<T>(ref this, value, serializer, PrefixStyle.Base128, true);
                            break;
                        case SerializerFeatures.CategoryScalar:
                            serializer.Write(ref this, value);
                            break;
                        default:
                            category.ThrowInvalidCategory();
                            break;
                    }
                }
            }

            /// <summary>
            /// Writes a map to the output using all default options
            /// </summary>
            public void WriteMap<TKey, TValue>(int fieldNumber, IEnumerable<KeyValuePair<TKey, TValue>> values)
                => WriteMap(fieldNumber, default, default, default, values, default, default);

            /// <summary>
            /// Writes a map to the output, specifying custom options
            /// </summary>
            public void WriteMap<TKey, TValue>(
                int fieldNumber,
                SerializerFeatures features,
                SerializerFeatures keyFeatures,
                SerializerFeatures valueFeatures,
                IEnumerable<KeyValuePair<TKey, TValue>> values, ISerializer<TKey> keySerializer = null, ISerializer<TValue> valueSerializer = null)
            {
                keySerializer ??= TypeModel.GetSerializer<TKey>(Model);
                valueSerializer ??= TypeModel.GetSerializer<TValue>(Model);

                keyFeatures.InheritFrom(keySerializer.Features);
                valueFeatures.InheritFrom(valueSerializer.Features);

                if (values == null)
                { }
                else
                {
                    var pairSerializer = KeyValuePairSerializer<TKey, TValue>.Create(Model, keySerializer, keyFeatures, valueSerializer, valueFeatures);
                    features.InheritFrom(pairSerializer.Features);
                    var wireType = features.GetWireType();

                    if (values is Dictionary<TKey, TValue> dict)
                    {   // exploit custom iterator
                        WriteMapCore(fieldNumber, wireType, dict, pairSerializer);
                    }
                    else
                    {
                        WriteMapCore(fieldNumber, wireType, values, pairSerializer);
                    }

                }
            }

            void WriteMapCore<TKey, TValue>(int fieldNumber, WireType wireType, IEnumerable<KeyValuePair<TKey, TValue>> values,
                KeyValuePairSerializer<TKey, TValue> pairSerializer)
            {
                foreach (var pair in values)
                {
                    WriteFieldHeader(fieldNumber, wireType);
                    _writer.WriteMessage(ref this, pair, pairSerializer, PrefixStyle.Base128, false);
                }
            }

            void WriteMapCore<TKey, TValue>(int fieldNumber, WireType wireType, Dictionary<TKey, TValue> values,
                KeyValuePairSerializer<TKey, TValue> pairSerializer)
            {
                foreach (var pair in values)
                {
                    WriteFieldHeader(fieldNumber, wireType);
                    _writer.WriteMessage(ref this, pair, pairSerializer, PrefixStyle.Base128, false);
                }
            }

            /// <summary>
            /// Writes a value or sub-item to the writer
            /// </summary>
            public void WriteAny<T>(int fieldNumber, T value, ISerializer<T> serializer = null)
            {
                serializer ??= TypeModel.GetSerializer<T>(Model);
                WriteAny<T>(fieldNumber, serializer.Features, value, serializer);
            }

            /// <summary>
            /// Writes a value or sub-item to the writer
            /// </summary>
            public void WriteAny<T>(int fieldNumber, SerializerFeatures features, T value, ISerializer<T> serializer = null)
            {
                if (!(TypeHelper<T>.CanBeNull && value is null))
                {
                    serializer ??= TypeModel.GetSerializer<T>(Model);
                    features.InheritFrom(serializer.Features);
                    WriteFieldHeader(fieldNumber, features.GetWireType());

                    switch (features.GetCategory())
                    {
                        case SerializerFeatures.CategoryRepeated:
                            ((IRepeatedSerializer<T>)serializer).Write(ref this, fieldNumber, features, value);
                            break;
                        case SerializerFeatures.CategoryMessageWrappedAtRoot:
                        case SerializerFeatures.CategoryMessage:
                            _writer.WriteMessage<T>(ref this, value, serializer, PrefixStyle.Base128, features.ApplyRecursionCheck());
                            break;
                        case SerializerFeatures.CategoryScalar:
                            serializer.Write(ref this, value);
                            break;
                        default:
                            features.ThrowInvalidCategory();
                            break;
                    }
                }
            }

            /// <summary>
            /// Writes a sub-type to the input writer
            /// </summary>
            public void WriteSubType<T>(T value, ISubTypeSerializer<T> serializer = null) where T : class
            {
                _writer.WriteSubType<T>(ref this, value, serializer ?? TypeModel.GetSubTypeSerializer<T>(Model));
            }

            /// <summary>
            /// Writes a sub-type to the input writer
            /// </summary>
            public void WriteSubType<T>(int fieldNumber, T value, ISubTypeSerializer<T> serializer = null) where T : class
            {
                WriteFieldHeader(fieldNumber, WireType.String);
                _writer.WriteSubType<T>(ref this, value, serializer ?? TypeModel.GetSubTypeSerializer<T>(Model));
            }

            /// <summary>
            /// Writes a base-type to the input writer
            /// </summary>
            public void WriteBaseType<T>(T value, ISubTypeSerializer<T> serializer = null) where T : class
                => (serializer ?? TypeModel.GetSubTypeSerializer<T>(Model)).WriteSubType(ref this, value);

            internal TypeModel Model => _writer?.Model;

            internal WireType WireType
            {
                get => _writer.WireType;
                private set => _writer.WireType = value;
            }

            internal int Depth => _writer.Depth;

            internal int FieldNumber
            {
                get => _writer.fieldNumber;
                private set => _writer.fieldNumber = value;
            }

            internal long GetPosition() => _writer._position64;

            internal ProtoWriter GetWriter() => _writer;

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
                        ThrowInvalidSerializationOperation();
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
                        ThrowInvalidSerializationOperation();
                        break;
                }
            }

            /// <summary>
            /// Writes a byte-array to the stream; supported wire-types: String
            /// </summary>
            public void WriteBytes(byte[] data) => WriteBytes(data, 0, data.Length);

            /// <summary>
            /// Writes an object to the input writer as a root value; if the
            /// object is determined to be a scalar, it is written as though it were
            /// part of a message with field-number 1
            /// </summary>
            public long SerializeRoot<T>(T value, ISerializer<T> serializer = null)
            {
                try
                {
                    CheckClear();
                    serializer ??= TypeModel.GetSerializer<T>(Model);
                    long before = GetPosition();
#if FEAT_DYNAMIC_REF
                    if (TypeHelper<T>.IsReferenceType && value != null)
                        SetRootObject(value);
#endif
                    WriteAsRoot<T>(value, serializer);
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

            internal void WriteAsRoot<T>(T value, ISerializer<T> serializer)
            {
                var features = serializer.Features;
                var category = features.GetCategory();

                if (category == SerializerFeatures.CategoryMessageWrappedAtRoot)
                {
                    // to preserve legacy behavior of DateTime/TimeSpan etc
                    WriteMessage<T>(1, default, value, serializer);
                }
                else if (TypeHelper<T>.CanBeNull && value == null)
                {
                    // nothing to do
                }
                else
                {
                    switch (category)
                    {
                        case SerializerFeatures.CategoryScalar:
                            WriteFieldHeader(1, features.GetWireType());
                            serializer.Write(ref this, value);
                            break;
                        case SerializerFeatures.CategoryMessage:
                        case SerializerFeatures.CategoryRepeated:
                            serializer.Write(ref this, value);
                            break;
                        default:
                            features.ThrowInvalidCategory();
                            break;
                    }
                }
            }

            //[MethodImpl(HotPath)]
            //internal void SerializeRaw<T>(T value, ISerializer<T> serializer)
            //    => (serializer ?? TypeModel.GetSerializer<T>(Model)).Write(ref this, value);

#if FEAT_DYNAMIC_REF
            /// <summary>
            /// Specifies a known root object to use during reference-tracked serialization
            /// </summary>
            [MethodImpl(HotPath)]
            internal void SetRootObject(object value) => _writer.SetRootObject(value);

            [MethodImpl(HotPath)]
            internal int AddObjectKey(object value, out bool existing) => _writer.AddObjectKey(value, out existing);
#endif

            /// <summary>
            /// Abandon any pending unflushed data
            /// </summary>
            [MethodImpl(HotPath)]
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

#if FEAT_DYNAMIC_REF
            /// <summary>
            /// Write an encapsulated sub-object, using the supplied unique key (reprasenting a type).
            /// </summary>
            /// <param name="value">The object to write.</param>
            /// <param name="type">The type that uniquely identifies the type within the model.</param>
            public void WriteObject(object value, Type type)
            {
                var model = Model;
                if (model == null)
                {
                    ThrowHelper.ThrowInvalidOperationException("Cannot serialize sub-objects unless a model is provided");
                }
                if (type == null) type = value.GetType();

                
                if (model.CanSerialize(type)
                    && DynamicStub.TrySerialize(ObjectScope.WrappedMessage, type, model, ref this, value))
                {
                    // done!
                }
                else
                {
#pragma warning disable CS0618
                    SubItemToken token = StartSubItem(value);
                    if (model.TrySerializeAuxiliaryType(ref this, type, DataFormat.Default, TypeModel.ListItemTag, value, false, null))
                    {
                        // all ok
                    }
                    else
                    {
                        TypeModel.ThrowUnexpectedType(value.GetType());
                    }
                    EndSubItem(token);
#pragma warning restore CS0618
                }

            }
#endif

            internal void WriteObject(object value, Type type, PrefixStyle style, int fieldNumber)
            {
                var model = Model;
                if (model == null)
                {
                    ThrowHelper.ThrowInvalidOperationException("Cannot serialize sub-objects unless a model is provided");
                }
                if (type == null) type = value.GetType();
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

#pragma warning disable CS0618
                SubItemToken token = StartSubItem(value, style);
                if (!DynamicStub.TrySerializeAny(TypeModel.ListItemTag, SerializerFeatures.CategoryMessageWrappedAtRoot, type, Model, ref this, value))
                {
                    TypeModel.ThrowUnexpectedType(value.GetType());
                }
                EndSubItem(token, style);
#pragma warning restore CS0618
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
            /// <returns>A token representing the state of the stream; this token is given to EndSubItem.</returns>
            [Obsolete(PreferWriteMessage, false)]
            public SubItemToken StartSubItem(object instance) => StartSubItem(instance, PrefixStyle.Base128);

            /// <summary>
            /// Releases any resources associated with this instance
            /// </summary>
            public void Dispose()
            {
                var writer = _writer;
                this = default;
                writer?.Dispose();
            }

            [Obsolete(PreferWriteMessage, false)]
            internal SubItemToken StartSubItem(object instance, PrefixStyle style)
            {
                _writer.PreSubItem(ref this, instance);
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

            [Obsolete(PreferWriteMessage, false)]
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
            [Obsolete(PreferWriteMessage, false)]
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
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void ThrowInvalidSerializationOperation()
            {
                if (_writer == null) ThrowHelper.ThrowProtoException("No underlying writer");
                ThrowHelper.ThrowProtoException($"Invalid serialization operation with wire-type {WireType} at position {GetPosition()}, depth {Depth}");
            }

            /// <summary>
            /// Used for packed encoding; indicates that the next field should be skipped rather than
            /// a field header written. Note that the field number must match, else an exception is thrown
            /// when the attempt is made to write the (incorrect) field. The wire-type is taken from the
            /// subsequent call to WriteFieldHeader. Only primitive types can be packed.
            /// </summary>
            public void SetPackedField(int fieldNumber)
            {
                if (fieldNumber <= 0) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(fieldNumber));
                _writer.packedFieldNumber = fieldNumber;
            }

            /// <summary>
            /// Used for packed encoding; explicitly reset the packed field marker; this is not required
            /// if using StartSubItem/EndSubItem
            /// </summary>
            public void ClearPackedField(int fieldNumber)
            {
                if (fieldNumber != _writer.packedFieldNumber)
                    ThrowWrongPackedField(fieldNumber, _writer);
                _writer.packedFieldNumber = 0;

                static void ThrowWrongPackedField(int fieldNumber, ProtoWriter writer)
                {
                    ThrowHelper.ThrowInvalidOperationException("Field mismatch during packed encoding; expected " + writer.packedFieldNumber.ToString() + " but received " + fieldNumber.ToString());
                }
            }

            /// <summary>
            /// Throws an exception indicating that the given enum cannot be mapped to a serialized value.
            /// </summary>
            public void ThrowEnumException(object enumValue)
            {
#pragma warning disable RCS1097 // Remove redundant 'ToString' call.
                string rhs = enumValue == null ? "<null>" : (enumValue.GetType().FullName + "." + enumValue.ToString());
#pragma warning restore RCS1097 // Remove redundant 'ToString' call.
                ThrowHelper.ThrowProtoException($"No wire-value is mapped to the enum {rhs} at position {GetPosition()}");
            }
        }
    }
}
