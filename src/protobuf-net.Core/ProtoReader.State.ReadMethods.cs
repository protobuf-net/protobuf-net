using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace ProtoBuf
{
    partial class ProtoReader
    {
#if PLAT_AGGRESSIVE_OPTIMIZE
        internal const MethodImplOptions HotPath = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
#else
        internal const MethodImplOptions HotPath = MethodImplOptions.AggressiveInlining;
#endif

        ref partial struct State
        {


            /// <summary>
            /// Reads an unsigned 16-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
            /// </summary>
            [MethodImpl(HotPath)]
            public ushort ReadUInt16()
            {
                checked { return (ushort)ReadUInt32(); }
            }

            /// <summary>
            /// Reads a signed 16-bit integer from the stream: Variant, Fixed32, Fixed64, SignedVariant
            /// </summary>
            [MethodImpl(HotPath)]
            public short ReadInt16()
            {
                checked { return (short)ReadInt32(); }
            }

            /// <summary>
            /// Returns the position of the current reader (note that this is not necessarily the same as the position
            /// in the underlying stream, if multiple readers are used on the same stream)
            /// </summary>
            [MethodImpl(HotPath)]
            public long GetPosition() => _reader._longPosition;

            /// <summary>
            /// Reads an unsigned 8-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
            /// </summary>
            [MethodImpl(HotPath)]
            public byte ReadByte()
            {
                checked { return (byte)ReadUInt32(); }
            }

            /// <summary>
            /// Reads a signed 8-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
            /// </summary>
            [MethodImpl(HotPath)]
            public sbyte ReadSByte()
            {
                checked { return (sbyte)ReadInt32(); }
            }

            /// <summary>
            /// Reads an unsigned 32-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
            /// </summary>
            [MethodImpl(HotPath)]
            public uint ReadUInt32()
            {
                switch (_reader.WireType)
                {
                    case WireType.Varint:
                        return ReadUInt32Varint(Read32VarintMode.Signed);
                    case WireType.Fixed32:
                        return _reader.ImplReadUInt32Fixed(ref this);
                    case WireType.Fixed64:
                        ulong val = _reader.ImplReadUInt64Fixed(ref this);
                        checked { return (uint)val; }
                    default:
                        ThrowWireTypeException();
                        return default;
                }
            }

            /// <summary>
            /// Reads a signed 32-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
            /// </summary>
            [MethodImpl(HotPath)]
            public int ReadInt32()
            {
                switch (_reader.WireType)
                {
                    case WireType.Varint:
                        return (int)ReadUInt32Varint(Read32VarintMode.Signed);
                    case WireType.Fixed32:
                        return (int)_reader.ImplReadUInt32Fixed(ref this);
                    case WireType.Fixed64:
                        long l = ReadInt64();
                        checked { return (int)l; }
                    case WireType.SignedVarint:
                        return Zag(ReadUInt32Varint(Read32VarintMode.Signed));
                    default:
                        ThrowWireTypeException();
                        return default;
                }
            }

            /// <summary>
            /// Reads a signed 64-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
            /// </summary>
            [MethodImpl(HotPath)]
            public long ReadInt64()
            {
                switch (_reader.WireType)
                {
                    case WireType.Varint:
                        return (long)ReadUInt64Varint();
                    case WireType.Fixed32:
                        return (int)_reader.ImplReadUInt32Fixed(ref this);
                    case WireType.Fixed64:
                        return (long)_reader.ImplReadUInt64Fixed(ref this);
                    case WireType.SignedVarint:
                        return Zag(ReadUInt64Varint());
                    default:
                        ThrowWireTypeException();
                        return default;
                }
            }

            /// <summary>
            /// Reads a double-precision number from the stream; supported wire-types: Fixed32, Fixed64
            /// </summary>
            [MethodImpl(HotPath)]
            public double ReadDouble()
            {
                switch (_reader.WireType)
                {
                    case WireType.Fixed32:
                        return ReadSingle();
                    case WireType.Fixed64:
                        long value = ReadInt64();
                        unsafe { return *(double*)&value; }
                    default:
                        ThrowWireTypeException();
                        return default;
                }
            }

            /// <summary>
            /// Reads a single-precision number from the stream; supported wire-types: Fixed32, Fixed64
            /// </summary>
            [MethodImpl(HotPath)]
            public float ReadSingle()
            {
                switch (_reader.WireType)
                {
                    case WireType.Fixed32:
                        {
                            int value = ReadInt32();
                            unsafe { return *(float*)&value; }
                        }
                    case WireType.Fixed64:
                        {
                            double value = ReadDouble();
                            float f = (float)value;
                            if (float.IsInfinity(f) && !double.IsInfinity(value)) ThrowOverflow();
                            return f;
                        }
                    default:
                        ThrowWireTypeException();
                        return default;
                }
            }


            /// <summary>
            /// Reads a boolean value from the stream; supported wire-types: Variant, Fixed32, Fixed64
            /// </summary>
            [MethodImpl(HotPath)]
            public bool ReadBoolean() => ReadUInt32() != 0;

            /// <summary>
            /// Reads an unsigned 64-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
            /// </summary>
            [MethodImpl(HotPath)]
            public ulong ReadUInt64()
            {
                switch (_reader.WireType)
                {
                    case WireType.Varint: return ReadUInt64Varint();
                    case WireType.Fixed32: return _reader.ImplReadUInt32Fixed(ref this);
                    case WireType.Fixed64: return _reader.ImplReadUInt64Fixed(ref this);
                    default:
                        ThrowWireTypeException();
                        return default;
                }
            }

            /// <summary>
            /// Reads a byte-sequence from the stream, appending them to an existing byte-sequence (which can be null); supported wire-types: String
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public byte[] AppendBytes(byte[] value)
            {
                {
                    switch (_reader.WireType)
                    {
                        case WireType.String:
                            int len = (int)ReadUInt32Varint(Read32VarintMode.Signed);
                            _reader.WireType = WireType.None;
                            if (len == 0) return value ?? EmptyBlob;
                            int offset;
                            if (value == null || value.Length == 0)
                            {
                                offset = 0;
                                value = new byte[len];
                            }
                            else
                            {
                                offset = value.Length;
                                byte[] tmp = new byte[value.Length + len];
                                Buffer.BlockCopy(value, 0, tmp, 0, value.Length);
                                value = tmp;
                            }
                            _reader.ImplReadBytes(ref this, new ArraySegment<byte>(value, offset, len));
                            return value;
                        //case WireType.Varint:
                        //    return new byte[0];
                        default:
                            ThrowWireTypeException();
                            return default;
                    }
                }
            }

            /// <summary>
            /// Begins consuming a nested message in the stream; supported wire-types: StartGroup, String
            /// </summary>
            /// <remarks>The token returned must be help and used when callining EndSubItem</remarks>
            // [Obsolete(PreferReadMessage, false)]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public SubItemToken StartSubItem()
            {
                var reader = _reader;
                switch (_reader.WireType)
                {
                    case WireType.StartGroup:
                        reader.WireType = WireType.None; // to prevent glitches from double-calling
                        reader._depth++;
                        return new SubItemToken((long)(-reader._fieldNumber));
                    case WireType.String:
                        long len = (long)ReadUInt64Varint();
                        if (len < 0) ThrowInvalidOperationException();
                        long lastEnd = reader.blockEnd64;
                        reader.blockEnd64 = reader._longPosition + len;
                        reader._depth++;
                        return new SubItemToken(lastEnd);
                    default:
                        ThrowWireTypeException();
                        return default;
                }
            }

            /// <summary>
            /// Makes the end of consuming a nested message in the stream; the stream must be either at the correct EndGroup
            /// marker, or all fields of the sub-message must have been consumed (in either case, this means ReadFieldHeader
            /// should return zero)
            /// </summary>
            // [Obsolete(PreferReadMessage, false)]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void EndSubItem(SubItemToken token)
            {
                long value64 = token.value64;
                var reader = _reader;
                switch (reader.WireType)
                {
                    case WireType.EndGroup:
                        if (value64 >= 0) ThrowArgumentException(nameof(token));
                        if (-(int)value64 != reader._fieldNumber) ThrowProtoException("Wrong group was ended"); // wrong group ended!
                        reader.WireType = WireType.None; // this releases ReadFieldHeader
                        reader._depth--;
                        break;
                    // case WireType.None: // TODO reinstate once reads reset the wire-type
                    default:
                        long position = reader._longPosition;
                        if (value64 < position) ThrowProtoException($"Sub-message not read entirely; expected {value64}, was {position}");
                        if (reader.blockEnd64 != position && reader.blockEnd64 != long.MaxValue)
                        {
                            ThrowProtoException($"Sub-message not read correctly (end {reader.blockEnd64} vs {position})");
                        }
                        reader.blockEnd64 = value64;
                        reader._depth--;
                        break;
                        /*default:
                            throw reader.BorkedIt(); */
                }
            }

            /// <summary>
            /// Reads (merges) a sub-message from the stream, internally calling StartSubItem and EndSubItem, and (in between)
            /// parsing the message in accordance with the model associated with the reader
            /// </summary>
            [MethodImpl(HotPath)]
            public object ReadObject(object value, Type type) => ReadTypedObject(value, type);

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal object ReadTypedObject(object value, Type type)
            {
                var model = Model;
                if (model == null) ThrowInvalidOperationException("Cannot deserialize sub-objects unless a model is provided");

                SubItemToken token = StartSubItem();
                if (model.IsKnownType(ref type))
                {
                    value = DynamicStub.ReadMessage(type, model, ref this, value);
                }
                else if (type != null && model.TryDeserializeAuxiliaryType(ref this, DataFormat.Default, TypeModel.ListItemTag, type, ref value, true, false, true, false, null))
                {
                    // ok
                }
                else
                {
                    TypeModel.ThrowUnexpectedType(type);
                }
                EndSubItem(token);
                return value;
            }


            /// <summary>
            /// Reads a string from the stream (using UTF8); supported wire-types: String
            /// </summary>
            [MethodImpl(HotPath)]
#pragma warning disable IDE0060 // map isn't implemented yet, but we definitely want it
            public string ReadString(StringMap map = null)
#pragma warning restore IDE0060
            {
                if (_reader.WireType == WireType.String)
                {
                    int bytes = (int)ReadUInt32Varint(Read32VarintMode.Unsigned);
                    if (bytes == 0) return "";
                    var s = _reader.ImplReadString(ref this, bytes);
                    if (_reader.InternStrings) { s = _reader.Intern(s); }
                    return s;
                }
                ThrowWireTypeException();
                return default;
            }

            [MethodImpl(HotPath)]
            private uint ReadUInt32Varint(Read32VarintMode mode)
            {
                int read = _reader.ImplTryReadUInt32VarintWithoutMoving(ref this, mode, out uint value);
                if (read <= 0)
                {
                    if (mode == Read32VarintMode.FieldHeader) return 0;
                    ThrowEoF();
                }
                _reader.ImplSkipBytes(ref this, read);
                return value;
            }

            [MethodImpl(HotPath)]
            private ulong ReadUInt64Varint()
            {
                int read = _reader.ImplTryReadUInt64VarintWithoutMoving(ref this, out ulong value);
                if (read <= 0) ThrowEoF();

                _reader.ImplSkipBytes(ref this, read);
                return value;
            }

            /// <summary>
            /// Verifies that the stream's current wire-type is as expected, or a specialized sub-type (for example,
            /// SignedVariant) - in which case the current wire-type is updated. Otherwise an exception is thrown.
            /// </summary>
            [MethodImpl(HotPath)]
            public void Assert(WireType wireType)
            {
                var actual = _reader.WireType;
                if (actual == wireType) { }  // fine; everything as we expect
                else if (((int)wireType & 7) == (int)actual)
                {   // the underling type is a match; we're customising it with an extension
                    _reader.WireType = wireType;
                }
                else
                {   // nope; that is *not* what we were expecting!
                    ThrowWireTypeException();
                }
            }

            internal object GetKeyedObject(int key) => _reader.GetKeyedObject(key);

            internal void SetKeyedObject(int key, object value) => _reader.SetKeyedObject(key, value);

            internal void TrapNextObject(int key) => _reader.TrapNextObject(key);

            /// <summary>
            /// Discards the data for the current field.
            /// </summary>
            [MethodImpl(HotPath)]
            public void SkipField()
            {
                switch (_reader.WireType)
                {
                    case WireType.Fixed32:
                        _reader.ImplSkipBytes(ref this, 4);
                        break;
                    case WireType.Fixed64:
                        _reader.ImplSkipBytes(ref this, 8);
                        break;
                    case WireType.String:
                        long len = (long)ReadUInt64Varint();
                        _reader.ImplSkipBytes(ref this, len);
                        break;
                    case WireType.Varint:
                    case WireType.SignedVarint:
                        ReadUInt64Varint(); // and drop it
                        break;
                    case WireType.StartGroup:
                        SkipGroup();
                        break;
                    case WireType.None: // treat as explicit errorr
                    case WireType.EndGroup: // treat as explicit error
                    default: // treat as implicit error
                        ThrowWireTypeException();
                        break;
                }
            }

            internal bool IsKnownType(ref Type type) => Model != null && Model.IsKnownType(ref type);

            internal Type DeserializeType(string typeName) => _reader.DeserializeType(typeName);

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void SkipGroup()
            {
                int originalFieldNumber = _reader._fieldNumber;
                _reader._depth++; // need to satisfy the sanity-checks in ReadFieldHeader
                while (ReadFieldHeader() > 0) { SkipField(); }
                _reader._depth--;
                if (_reader.WireType == WireType.EndGroup && _reader._fieldNumber == originalFieldNumber)
                { // we expect to exit in a similar state to how we entered
                    _reader.WireType = WireType.None;
                    return;
                }
                ThrowWireTypeException();
            }

            /// <summary>
            /// Reads a field header from the stream, setting the wire-type and retuning the field number. If no
            /// more fields are available, then 0 is returned. This methods respects sub-messages.
            /// </summary>
            [MethodImpl(HotPath)]
            public int ReadFieldHeader()
            {
                // at the end of a group the caller must call EndSubItem to release the
                // reader (which moves the status to Error, since ReadFieldHeader must
                // then be called)
                if (_reader.blockEnd64 <= _reader._longPosition || _reader.WireType == WireType.EndGroup)
                    return 0;

                if (RemainingInCurrent >= 5)
                {
                    var read = ReadVarintUInt32(out var tag);
                    _reader.Advance(read);
                    return _reader.SetTag(tag);
                }
                return ReadFieldHeaderFallback();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private int ReadFieldHeaderFallback()
            {
                int read = _reader.ImplTryReadUInt32VarintWithoutMoving(ref this, Read32VarintMode.FieldHeader, out var tag);
                if (read == 0)
                {
                    _reader.WireType = 0;
                    return _reader._fieldNumber = 0;
                }
                _reader.ImplSkipBytes(ref this, read);
                return _reader.SetTag(tag);
            }

            /// <summary>
            /// Looks ahead to see whether the next field in the stream is what we expect
            /// (typically; what we've just finished reading - for example ot read successive list items)
            /// </summary>
            [MethodImpl(HotPath)]
            public bool TryReadFieldHeader(int field)
            {
                var reader = _reader;
                // check for virtual end of stream
                if (reader.blockEnd64 <= reader._longPosition || reader.WireType == WireType.EndGroup) { return false; }

                int read = reader.ImplTryReadUInt32VarintWithoutMoving(ref this, Read32VarintMode.FieldHeader, out uint tag);
                WireType tmpWireType; // need to catch this to exclude (early) any "end group" tokens
                if (read > 0 && ((int)tag >> 3) == field
                    && (tmpWireType = (WireType)(tag & 7)) != WireType.EndGroup)
                {
                    reader.WireType = tmpWireType;
                    reader._fieldNumber = field;
                    reader.ImplSkipBytes(ref this, read);
                    return true;
                }
                return false;
            }

            [MethodImpl(HotPath)]
            internal void CheckFullyConsumed()
            {
                if (!_reader.IsFullyConsumed(ref this)) ThrowProtoException("Incorrect number of bytes consumed");
            }

            /// <summary>
            /// Compares the streams current wire-type to the hinted wire-type, updating the reader if necessary; for example,
            /// a Variant may be updated to SignedVariant. If the hinted wire-type is unrelated then no change is made.
            /// </summary>
            [MethodImpl(HotPath)]
#pragma warning disable CS0618
            public void Hint(WireType wireType) => _reader.Hint(wireType);
#pragma warning restore CS0618

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void ThrowWireTypeException()
            {
                var message = _reader == null ? "(no reader)" : $"Invalid wire-type ({_reader.WireType}); this usually means you have over-written a file without truncating or setting the length; see https://stackoverflow.com/q/2152978/23354";
                ThrowProtoException(message);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void ThrowProtoException(string message)
            {
                throw AddErrorData(new ProtoException(message), _reader, ref this);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void ThrowEoF()
            {
                throw AddErrorData(new EndOfStreamException(), _reader, ref this);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void ThrowInvalidOperationException(string message = null)
            {
                var ex = string.IsNullOrWhiteSpace(message) ? new InvalidOperationException() : new InvalidOperationException(message);
                throw AddErrorData(ex, _reader, ref this);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void ThrowArgumentException(string message)
            {
                throw AddErrorData(new ArgumentException(message), _reader, ref this);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void ThrowOverflow()
            {
                throw AddErrorData(new OverflowException(), _reader, ref this);
            }

            internal static Exception AddErrorData(Exception exception, ProtoReader source, ref State state)
            {
                if (exception != null && source != null && !exception.Data.Contains("protoSource"))
                {
                    exception.Data.Add("protoSource", string.Format("tag={0}; wire-type={1}; offset={2}; depth={3}",
                        source.FieldNumber, source.WireType, state.GetPosition(), source._depth));
                }
                return exception;
            }

            [MethodImpl(HotPath)]
            private static int Zag(uint ziggedValue)
            {
                const int Int32Msb = 1 << 31;

                int value = (int)ziggedValue;
                return (-(value & 0x01)) ^ ((value >> 1) & ~Int32Msb);
            }

            [MethodImpl(HotPath)]
            private static long Zag(ulong ziggedValue)
            {
                const long Int64Msb = 1L << 63;

                long value = (long)ziggedValue;
                return (-(value & 0x01L)) ^ ((value >> 1) & ~Int64Msb);
            }

            /// <summary>
            /// Throws an exception indication that the given value cannot be mapped to an enum.
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void ThrowEnumException(Type type, int value)
            {
                string desc = type == null ? "<null>" : type.FullName;
                throw AddErrorData(new ProtoException("No " + desc + " enum is mapped to the wire-value " + value.ToString()), _reader, ref this);
            }

            /// <summary>
            /// Copies the current field into the instance as extension data
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void AppendExtensionData(IExtensible instance)
            {
                if (instance == null) ThrowHelper.ThrowArgumentNullException(nameof(instance));
                IExtension extn = instance.GetExtensionObject(true);
                bool commit = false;
                // unusually we *don't* want "using" here; the "finally" does that, with
                // the extension object being responsible for disposal etc
                Stream dest = extn.BeginAppend();
                try
                {
                    //TODO: replace this with stream-based, buffered raw copying
                    var writeState = ProtoWriter.State.Create(dest, _reader._model, null);
                    try
                    {
                        AppendExtensionField(ref writeState);
                        writeState.Close();
                    }
                    catch
                    {
                        writeState.Abandon();
                        throw;
                    }
                    finally
                    {
                        writeState.Dispose();
                    }
                    commit = true;
                }
                finally { extn.EndAppend(dest, commit); }
            }

            /// <summary>
            /// Indicates the underlying proto serialization format on the wire.
            /// </summary>
            public WireType WireType
            {
                [MethodImpl(HotPath)]
                get => _reader.WireType;
            }

            /// <summary>
            /// Gets / sets a flag indicating whether strings should be checked for repetition; if
            /// true, any repeated UTF-8 byte sequence will result in the same String instance, rather
            /// than a second instance of the same string. Disabled by default. Note that this uses
            /// a <i>custom</i> interner - the system-wide string interner is not used.
            /// </summary>
            public bool InternStrings
            {
                get => _reader.InternStrings;
                set => _reader.InternStrings = value;
            }

            /// <summary>
            /// Gets the number of the field being processed.
            /// </summary>
            public int FieldNumber
            {
                [MethodImpl(HotPath)]
                get => _reader._fieldNumber;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void AppendExtensionField(ref ProtoWriter.State writeState)
            {
                //TODO: replace this with stream-based, buffered raw copying
                var reader = _reader;
                writeState.WriteFieldHeader(reader._fieldNumber, reader.WireType);
                switch (reader.WireType)
                {
                    case WireType.Fixed32:
                        writeState.WriteInt32(ReadInt32());
                        return;
                    case WireType.Varint:
                    case WireType.SignedVarint:
                    case WireType.Fixed64:
                        writeState.WriteInt64(ReadInt64());
                        return;
                    case WireType.String:
                        writeState.WriteBytes(AppendBytes(null));
                        return;
                    case WireType.StartGroup:
                        SubItemToken readerToken = StartSubItem(),
                            writerToken = writeState.StartSubItem(null);
                        while (ReadFieldHeader() > 0) { AppendExtensionField(ref writeState); }
                        EndSubItem(readerToken);
                        writeState.EndSubItem(writerToken);
                        return;
                    case WireType.None: // treat as explicit errorr
                    case WireType.EndGroup: // treat as explicit error
                    default: // treat as implicit error
                        ThrowWireTypeException();
                        break;
                }
            }

            /// <summary>
            /// Reads a Type from the stream, using the model's DynamicTypeFormatting if appropriate; supported wire-types: String
            /// </summary>
            [MethodImpl(HotPath)]
            public Type ReadType() => TypeModel.DeserializeType(_reader._model, ReadString());

            /// <summary>
            /// Reads a sub-item from the input reader
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public T ReadMessage<T>(T value = default, IMessageSerializer<T> serializer = null)
            {
                var tok = StartSubItem();
                var result = (serializer ?? TypeModel.GetSerializer<T>(_reader._model)).Read(ref this, value);
                EndSubItem(tok);
                return result;
            }

            internal TypeModel Model
            {
                [MethodImpl(HotPath)]
                get => _reader?.Model;
                private set => _reader.Model = value;
            }

            /// <summary>
            /// Reads a sub-item from the input reader
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public T ReadBaseType<TBaseType, T>(T value = null, ISubTypeSerializer<TBaseType> serializer = null)
                where TBaseType : class
                where T : class, TBaseType
            {
                return (T)(serializer ?? TypeModel.GetSubTypeSerializer<TBaseType>(_reader._model)).ReadSubType(ref this, SubTypeState<TBaseType>.Create<T>(_reader, value));
            }

            /// <summary>
            /// Deserialize an instance of the provided type
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public T Deserialize<T>(T value = default, IMessageSerializer<T> serializer = null)
            {
                if (TypeHelper<T>.IsObjectType && value != null)
                {
                    _reader.SetRootObject(value);
                }

                var result = (serializer ?? TypeModel.GetSerializer<T>(Model)).Read(ref this, value);
                CheckFullyConsumed();
                return result;
            }

            /// <summary>
            /// Gets the serialization context associated with this instance;
            /// </summary>
            public ISerializationContext Context
            {
                [MethodImpl(HotPath)]
                get => _reader;
            }

            /// <summary>
            /// Indicates whether the reader still has data remaining in the current sub-item,
            /// additionally setting the wire-type for the next field if there is more data.
            /// This is used when decoding packed data.
            /// </summary>
            [MethodImpl(HotPath)]
            public bool HasSubValue(WireType wireType) => ProtoReader.HasSubValue(wireType, _reader);

            /// <summary>
            /// Create an instance of the provided type, respecting any custom factory rules
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public T CreateInstance<T>(IFactory<T> factory = null)
            {
                var obj = TypeModel.CreateInstance<T>(Context, factory);
                if (TypeHelper<T>.IsObjectType) NoteObject(obj);
                return obj;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal object DeserializeFallback(object value, Type type, TypeModel overrideModel)
            {
                var oldModel = Model;
                try
                {
                    Model = overrideModel;
                    return DeserializeFallback(value, type);
                }
                finally
                {
                    Model = oldModel;
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal object DeserializeFallback(object value, Type type)
            {
                if (type == null || type == typeof(object))
                    type = value?.GetType() ?? typeof(object);

                bool autoCreate = Model.PrepareDeserialize(value, ref type);
                if (value != null) _reader.SetRootObject(value);
                object obj = Model.DeserializeAny(ref this, type, value, autoCreate);
                CheckFullyConsumed();
                return obj;
            }

            [MethodImpl(HotPath)]
            internal T DeserializeImpl<T>(T value = default)
            {
                if (TypeHelper<T>.UseFallback)
                {
                    Debug.Assert(Model != null, "Model is null");
                    return (T)DeserializeFallback(value, typeof(T));
                }
                else
                {
                    return Deserialize<T>(value);
                }
            }

            /// <summary>
            /// Utility method, not intended for public use; this helps maintain the root object is complex scenarios
            /// </summary>
            [MethodImpl(HotPath)]
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            public void NoteObject(object value) => ProtoReader.NoteObject(value, _reader);
        }
    }
}
