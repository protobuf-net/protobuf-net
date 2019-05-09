
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace ProtoBuf
{
    /// <summary>
    /// A stateful reader, used to read a protobuf stream. Typical usage would be (sequentially) to call
    /// ReadFieldHeader and (after matching the field) an appropriate Read* method.
    /// </summary>
    public abstract partial class ProtoReader : IDisposable
    {
        internal const string UseStateAPI = "If possible, please use the State API; a transitionary implementation is provided, but this API may be removed in a future version";
        private TypeModel _model;
        private int _fieldNumber, _depth;
        private long blockEnd64;
        private NetObjectCache netCache;

        // this is how many outstanding objects do not currently have
        // values for the purposes of reference tracking; we'll default
        // to just trapping the root object
        // note: objects are trapped (the ref and key mapped) via NoteObject
        private uint trapCount; // uint is so we can use beq/bne more efficiently than bgt

        /// <summary>
        /// Gets the number of the field being processed.
        /// </summary>
        public int FieldNumber => _fieldNumber;

        /// <summary>
        /// Indicates the underlying proto serialization format on the wire.
        /// </summary>
        public WireType WireType {
            get;
            private protected set;
        }

        internal const long TO_EOF = -1;

        /// <summary>
        /// Gets / sets a flag indicating whether strings should be checked for repetition; if
        /// true, any repeated UTF-8 byte sequence will result in the same String instance, rather
        /// than a second instance of the same string. Disabled by default. Note that this uses
        /// a <i>custom</i> interner - the system-wide string interner is not used.
        /// </summary>
        public bool InternStrings { get; set; }

        private protected ProtoReader() { }

        /// <summary>
        /// Initialize the reader
        /// </summary>
        internal void Init(TypeModel model, SerializationContext context)
        {
            _model = model;

            if (context == null) { context = SerializationContext.Default; }
            else { context.Freeze(); }
            this.context = context;
            _longPosition = 0;
            _depth = _fieldNumber = 0;

            blockEnd64 = long.MaxValue;
            InternStrings = (model ?? RuntimeTypeModel.Default).InternStrings;
            WireType = WireType.None;
            trapCount = 1;
            if (netCache == null) netCache = new NetObjectCache();
        }

        private SerializationContext context;

        /// <summary>
        /// Addition information about this deserialization operation.
        /// </summary>
        public SerializationContext Context => context;

        /// <summary>
        /// Releases resources used by the reader, but importantly <b>does not</b> Dispose the 
        /// underlying stream; in many typical use-cases the stream is used for different
        /// processes, so it is assumed that the consumer will Dispose their stream separately.
        /// </summary>
        public virtual void Dispose()
        {
            _model = null;
            if (stringInterner != null)
            {
                stringInterner.Clear();
                stringInterner = null;
            }
            if (netCache != null) netCache.Clear();
        }

        private protected enum Read32VarintMode
        {
            Signed,
            Unsigned,
            FieldHeader,
        }

        private uint ReadUInt32Varint(ref State state, Read32VarintMode mode)
        {
            int read = ImplTryReadUInt32VarintWithoutMoving(ref state, mode, out uint value);
            if (read <= 0)
            {
                if (mode == Read32VarintMode.FieldHeader) return 0;
                ThrowEoF(this, ref state);
            }
            ImplSkipBytes(ref state, read);
            return value;
        }

        private ulong ReadUInt64Varint(ref State state)
        {
            int read = ImplTryReadUInt64VarintWithoutMoving(ref state, out ulong value);
            if (read <= 0)
            {
                ThrowEoF(this, ref state);
            }
            ImplSkipBytes(ref state, read);
            return value;
        }

        private protected abstract int ImplTryReadUInt64VarintWithoutMoving(ref State state, out ulong value);

        /// <summary>
        /// Returns the position of the current reader (note that this is not necessarily the same as the position
        /// in the underlying stream, if multiple readers are used on the same stream)
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public int Position { get { return checked((int)_longPosition); } }

        /// <summary>
        /// Returns the position of the current reader (note that this is not necessarily the same as the position
        /// in the underlying stream, if multiple readers are used on the same stream)
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public long LongPosition => _longPosition;

        private long _longPosition;

        internal void Advance(long count) => _longPosition += count;

        /// <summary>
        /// Returns the position of the current reader (note that this is not necessarily the same as the position
        /// in the underlying stream, if multiple readers are used on the same stream)
        /// </summary>
#pragma warning disable RCS1163 // Unused parameter.
        public long GetPosition(ref State state) => _longPosition;
#pragma warning restore RCS1163 // Unused parameter.

        /// <summary>
        /// Reads a signed 16-bit integer from the stream: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public short ReadInt16()
        {
            State state = default;
            return ReadInt16(ref state);
        }

        /// <summary>
        /// Reads a signed 16-bit integer from the stream: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        public short ReadInt16(ref State state)
        {
            checked { return (short)ReadInt32(ref state); }
        }

        /// <summary>
        /// Reads an unsigned 16-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public ushort ReadUInt16()
        {
            State state = default;
            return ReadUInt16(ref state);
        }

        /// <summary>
        /// Reads an unsigned 16-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        public ushort ReadUInt16(ref State state)
        {
            checked { return (ushort)ReadUInt32(ref state); }
        }

        /// <summary>
        /// Reads an unsigned 8-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public byte ReadByte()
        {
            State state = default;
            return ReadByte(ref state);
        }

        /// <summary>
        /// Reads an unsigned 8-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        public byte ReadByte(ref State state)
        {
            checked { return (byte)ReadUInt32(ref state); }
        }

        /// <summary>
        /// Reads a signed 8-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public sbyte ReadSByte()
        {
            State state = default;
            return ReadSByte(ref state);
        }

        /// <summary>
        /// Reads a signed 8-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        public sbyte ReadSByte(ref State state)
        {
            checked { return (sbyte)ReadInt32(ref state); }
        }

        /// <summary>
        /// Reads an unsigned 32-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public uint ReadUInt32()
        {
            State state = default;
            return ReadUInt32(ref state);
        }

        /// <summary>
        /// Reads an unsigned 32-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        public uint ReadUInt32(ref State state)
        {
            switch (WireType)
            {
                case WireType.Variant:
                    return ReadUInt32Varint(ref state, Read32VarintMode.Signed);
                case WireType.Fixed32:
                    return ImplReadUInt32Fixed(ref state);
                case WireType.Fixed64:
                    ulong val = ImplReadUInt64Fixed(ref state);
                    checked { return (uint)val; }
                default:
                    throw CreateWireTypeException(ref state);
            }
        }

        /// <summary>
        /// Reads a signed 32-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public int ReadInt32()
        {
            State state = default;
            return ReadInt32(ref state);
        }

        /// <summary>
        /// Reads a signed 32-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        public int ReadInt32(ref State state)
        {
            switch (WireType)
            {
                case WireType.Variant:
                    return (int)ReadUInt32Varint(ref state, Read32VarintMode.Signed);
                case WireType.Fixed32:
                    return (int)ImplReadUInt32Fixed(ref state);
                case WireType.Fixed64:
                    long l = ReadInt64(ref state);
                    checked { return (int)l; }
                case WireType.SignedVariant:
                    return Zag(ReadUInt32Varint(ref state, Read32VarintMode.Signed));
                default:
                    throw CreateWireTypeException(ref state);
            }
        }

        private protected abstract uint ImplReadUInt32Fixed(ref State state);
        private protected abstract ulong ImplReadUInt64Fixed(ref State state);

        private const long Int64Msb = ((long)1) << 63;
        private const int Int32Msb = ((int)1) << 31;
        private protected static int Zag(uint ziggedValue)
        {
            int value = (int)ziggedValue;
            return (-(value & 0x01)) ^ ((value >> 1) & ~ProtoReader.Int32Msb);
        }

        private protected static long Zag(ulong ziggedValue)
        {
            long value = (long)ziggedValue;
            return (-(value & 0x01L)) ^ ((value >> 1) & ~ProtoReader.Int64Msb);
        }

        /// <summary>
        /// Reads a signed 64-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public long ReadInt64()
        {
            State state = default;
            return ReadInt64(ref state);
        }

        /// <summary>
        /// Reads a signed 64-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        public long ReadInt64(ref State state)
        {
            switch (WireType)
            {
                case WireType.Variant:
                    return (long)ReadUInt64Varint(ref state);
                case WireType.Fixed32:
                    return (int)ImplReadUInt32Fixed(ref state);
                case WireType.Fixed64:
                    return (long)ImplReadUInt64Fixed(ref state);
                case WireType.SignedVariant:
                    return Zag(ReadUInt64Varint(ref state));
                default:
                    throw CreateWireTypeException(ref state);
            }
        }

        private Dictionary<string, string> stringInterner;
        private protected string Intern(string value)
        {
            if (value == null) return null;
            if (value.Length == 0) return "";
            if (stringInterner == null)
            {
                stringInterner = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { value, value }
                };
            }
            else if (stringInterner.TryGetValue(value, out string found))
            {
                value = found;
            }
            else
            {
                stringInterner.Add(value, value);
            }
            return value;
        }

#if COREFX
        private protected static readonly Encoding UTF8 = Encoding.UTF8;
#else
        private protected static readonly UTF8Encoding UTF8 = new UTF8Encoding();
#endif
        /// <summary>
        /// Reads a string from the stream (using UTF8); supported wire-types: String
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public string ReadString()
        {
            State state = default;
            return ReadString(ref state);
        }

        /// <summary>
        /// Reads a string from the stream (using UTF8); supported wire-types: String
        /// </summary>
        public string ReadString(ref State state)
        {
            if (WireType == WireType.String)
            {
                int bytes = (int)ReadUInt32Varint(ref state, Read32VarintMode.Unsigned);
                if (bytes == 0) return "";
                var s = ImplReadString(ref state, bytes);
                if (InternStrings) { s = Intern(s); }
                return s;
            }
            throw CreateWireTypeException(ref state);
        }

        private protected abstract string ImplReadString(ref State state, int bytes);

        /// <summary>
        /// Throws an exception indication that the given value cannot be mapped to an enum.
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public void ThrowEnumException(Type type, int value)
        {
            State state = default;
            ThrowEnumException(ref state, type, value);
        }
        /// <summary>
        /// Throws an exception indication that the given value cannot be mapped to an enum.
        /// </summary>
        public void ThrowEnumException(ref State state, Type type, int value)
        {
            string desc = type == null ? "<null>" : type.FullName;
            throw AddErrorData(new ProtoException("No " + desc + " enum is mapped to the wire-value " + value.ToString()), this, ref state);
        }

        private protected Exception CreateWireTypeException(ref State state)
        {
            return CreateException(ref state, $"Invalid wire-type ({WireType}); this usually means you have over-written a file without truncating or setting the length; see https://stackoverflow.com/q/2152978/23354");
        }

        private Exception CreateException(ref State state, string message)
        {
            return AddErrorData(new ProtoException(message), this, ref state);
        }

        /// <summary>
        /// Reads a double-precision number from the stream; supported wire-types: Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public double ReadDouble()
        {
            State state = default;
            return ReadDouble(ref state);
        }

        /// <summary>
        /// Reads a double-precision number from the stream; supported wire-types: Fixed32, Fixed64
        /// </summary>
        public double ReadDouble(ref State state)
        {
            switch (WireType)
            {
                case WireType.Fixed32:
                    return ReadSingle(ref state);
                case WireType.Fixed64:
                    long value = ReadInt64(ref state);
#if FEAT_SAFE
                    return BitConverter.Int64BitsToDouble(value);
                    // return BitConverter.ToDouble(BitConverter.GetBytes(value), 0);
#else
                    unsafe { return *(double*)&value; }
#endif
                default:
                    throw CreateWireTypeException(ref state);
            }
        }

        /// <summary>
        /// Reads (merges) a sub-message from the stream, internally calling StartSubItem and EndSubItem, and (in between)
        /// parsing the message in accordance with the model associated with the reader
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static object ReadObject(object value, int key, ProtoReader reader)
        {
            State state = default;
            return ReadTypedObject(reader, ref state, value, key, null);
        }

        /// <summary>
        /// Reads (merges) a sub-message from the stream, internally calling StartSubItem and EndSubItem, and (in between)
        /// parsing the message in accordance with the model associated with the reader
        /// </summary>
        public static object ReadObject(object value, int key, ProtoReader reader, ref State state)
            => ReadTypedObject(reader, ref state, value, key, null);

        internal static object ReadTypedObject(ProtoReader reader, ref State state, object value, int key, Type type)
        {
            if (reader._model == null)
            {
                throw AddErrorData(new InvalidOperationException("Cannot deserialize sub-objects unless a model is provided"), reader, ref state);
            }
            SubItemToken token = ProtoReader.StartSubItem(reader, ref state);
            if (key >= 0)
            {
                value = reader._model.DeserializeCore(reader, ref state, key, value);
            }
            else if (type != null && reader._model.TryDeserializeAuxiliaryType(reader, ref state, DataFormat.Default, Serializer.ListItemTag, type, ref value, true, false, true, false, null))
            {
                // ok
            }
            else
            {
                TypeModel.ThrowUnexpectedType(type);
            }
            ProtoReader.EndSubItem(token, reader, ref state);
            return value;
        }

        /// <summary>
        /// Makes the end of consuming a nested message in the stream; the stream must be either at the correct EndGroup
        /// marker, or all fields of the sub-message must have been consumed (in either case, this means ReadFieldHeader
        /// should return zero)
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void EndSubItem(SubItemToken token, ProtoReader reader)
        {
            State state = default;
            EndSubItem(token, reader, ref state);
        }
        /// <summary>
        /// Makes the end of consuming a nested message in the stream; the stream must be either at the correct EndGroup
        /// marker, or all fields of the sub-message must have been consumed (in either case, this means ReadFieldHeader
        /// should return zero)
        /// </summary>
        public static void EndSubItem(SubItemToken token, ProtoReader reader, ref State state)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            long value64 = token.value64;
            switch (reader.WireType)
            {
                case WireType.EndGroup:
                    if (value64 >= 0) throw AddErrorData(new ArgumentException("token"), reader, ref state);
                    if (-(int)value64 != reader._fieldNumber) throw reader.CreateException(ref state, "Wrong group was ended"); // wrong group ended!
                    reader.WireType = WireType.None; // this releases ReadFieldHeader
                    reader._depth--;
                    break;
                // case WireType.None: // TODO reinstate once reads reset the wire-type
                default:
                    long position = reader._longPosition;
                    if (value64 < position) throw reader.CreateException(ref state, $"Sub-message not read entirely; expected {value64}, was {position}");
                    if (reader.blockEnd64 != position && reader.blockEnd64 != long.MaxValue)
                    {
                        throw reader.CreateException(ref state, $"Sub-message not read correctly (end {reader.blockEnd64} vs {position})");
                    }
                    reader.blockEnd64 = value64;
                    reader._depth--;
                    break;
                    /*default:
                        throw reader.BorkedIt(); */
            }
        }

        /// <summary>
        /// Begins consuming a nested message in the stream; supported wire-types: StartGroup, String
        /// </summary>
        /// <remarks>The token returned must be help and used when callining EndSubItem</remarks>
        [Obsolete(UseStateAPI, false)]
        public static SubItemToken StartSubItem(ProtoReader reader)
        {
            State state = default;
            return StartSubItem(reader, ref state);
        }
        /// <summary>
        /// Begins consuming a nested message in the stream; supported wire-types: StartGroup, String
        /// </summary>
        /// <remarks>The token returned must be help and used when callining EndSubItem</remarks>
        public static SubItemToken StartSubItem(ProtoReader reader, ref State state)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            switch (reader.WireType)
            {
                case WireType.StartGroup:
                    reader.WireType = WireType.None; // to prevent glitches from double-calling
                    reader._depth++;
                    return new SubItemToken((long)(-reader._fieldNumber));
                case WireType.String:
                    long len = (long)reader.ReadUInt64Varint(ref state);
                    if (len < 0) throw AddErrorData(new InvalidOperationException(), reader, ref state);
                    long lastEnd = reader.blockEnd64;
                    reader.blockEnd64 = reader._longPosition + len;
                    reader._depth++;
                    return new SubItemToken(lastEnd);
                default:
                    throw reader.CreateWireTypeException(ref state); // throws
            }
        }

        /// <summary>
        /// Reads a field header from the stream, setting the wire-type and retuning the field number. If no
        /// more fields are available, then 0 is returned. This methods respects sub-messages.
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public int ReadFieldHeader()
        {
            State state = default;
            return ReadFieldHeader(ref state);
        }

        /// <summary>
        /// Reads a field header from the stream, setting the wire-type and retuning the field number. If no
        /// more fields are available, then 0 is returned. This methods respects sub-messages.
        /// </summary>
        public int ReadFieldHeader(ref State state)
        {
            // at the end of a group the caller must call EndSubItem to release the
            // reader (which moves the status to Error, since ReadFieldHeader must
            // then be called)
            if (blockEnd64 <= _longPosition || WireType == WireType.EndGroup) { return 0; }

            if (state.RemainingInCurrent >= 5)
            {
                var read = state.ReadVarintUInt32(out var tag);
                Advance(read);
                return SetTag(tag);
            }
            return ReadFieldHeaderFallback(ref state);
        }
        private int ReadFieldHeaderFallback(ref State state)
        {
            int read = ImplTryReadUInt32VarintWithoutMoving(ref state, Read32VarintMode.FieldHeader, out var tag);
            if (read == 0)
            {
                WireType = 0;
                return _fieldNumber = 0;
            }
            ImplSkipBytes(ref state, read);
            return SetTag(tag);
        }
#if !(NET20 || NET35 || NET40)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private int SetTag(uint tag)
        {
            if((_fieldNumber = (int)(tag >> 3)) < 1) ThrowInvalidField(_fieldNumber);
            if ((WireType = (WireType)(tag & 7)) == WireType.EndGroup)
            {
                if (_depth > 0) return 0; // spoof an end, but note we still set the field-number
                ThrowUnexpectedEndGroup();
            }
            return _fieldNumber;
        }
        private static void ThrowInvalidField(int fieldNumber)
            => throw new ProtoException("Invalid field in source data: " + fieldNumber.ToString());
        private static void ThrowUnexpectedEndGroup()
            => throw new ProtoException("Unexpected end-group in source data; this usually means the source data is corrupt");

        /// <summary>
        /// Looks ahead to see whether the next field in the stream is what we expect
        /// (typically; what we've just finished reading - for example ot read successive list items)
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public bool TryReadFieldHeader(int field)
        {
            State state = default;
            return TryReadFieldHeader(ref state, field);
        }

        /// <summary>
        /// Looks ahead to see whether the next field in the stream is what we expect
        /// (typically; what we've just finished reading - for example ot read successive list items)
        /// </summary>
        public bool TryReadFieldHeader(ref State state, int field)
        {
            // check for virtual end of stream
            if (blockEnd64 <= _longPosition || WireType == WireType.EndGroup) { return false; }

            int read = ImplTryReadUInt32VarintWithoutMoving(ref state, Read32VarintMode.FieldHeader, out uint tag);
            WireType tmpWireType; // need to catch this to exclude (early) any "end group" tokens
            if (read > 0 && ((int)tag >> 3) == field
                && (tmpWireType = (WireType)(tag & 7)) != WireType.EndGroup)
            {
                WireType = tmpWireType;
                _fieldNumber = field;
                ImplSkipBytes(ref state, read);
                return true;
            }
            return false;
        }

        private protected abstract int ImplTryReadUInt32VarintWithoutMoving(ref State state, Read32VarintMode mode, out uint value);

        /// <summary>
        /// Get the TypeModel associated with this reader
        /// </summary>
        public TypeModel Model { get { return _model; } }

        /// <summary>
        /// Compares the streams current wire-type to the hinted wire-type, updating the reader if necessary; for example,
        /// a Variant may be updated to SignedVariant. If the hinted wire-type is unrelated then no change is made.
        /// </summary>
        public void Hint(WireType wireType)
        {
#pragma warning disable RCS1218 // Simplify code branching.
            if (WireType == wireType) { }  // fine; everything as we expect
            else if (((int)wireType & 7) == (int)this.WireType)
            {   // the underling type is a match; we're customising it with an extension
                WireType = wireType;
            }
            // note no error here; we're OK about using alternative data
#pragma warning restore RCS1218 // Simplify code branching.
        }

        /// <summary>
        /// Verifies that the stream's current wire-type is as expected, or a specialized sub-type (for example,
        /// SignedVariant) - in which case the current wire-type is updated. Otherwise an exception is thrown.
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public void Assert(WireType wireType)
        {
            State state = default;
            Assert(ref state, wireType);
        }
        /// <summary>
        /// Verifies that the stream's current wire-type is as expected, or a specialized sub-type (for example,
        /// SignedVariant) - in which case the current wire-type is updated. Otherwise an exception is thrown.
        /// </summary>
        public void Assert(ref State state, WireType wireType)
        {
            if (this.WireType == wireType) { }  // fine; everything as we expect
            else if (((int)wireType & 7) == (int)this.WireType)
            {   // the underling type is a match; we're customising it with an extension
                this.WireType = wireType;
            }
            else
            {   // nope; that is *not* what we were expecting!
                throw CreateWireTypeException(ref state);
            }
        }

        private protected abstract void ImplSkipBytes(ref State state, long count);

        /// <summary>
        /// Discards the data for the current field.
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public void SkipField()
        {
            State state = default;
            SkipField(ref state);
        }

        /// <summary>
        /// Discards the data for the current field.
        /// </summary>
        public void SkipField(ref State state)
        {
            switch (WireType)
            {
                case WireType.Fixed32:
                    ImplSkipBytes(ref state, 4);
                    return;
                case WireType.Fixed64:
                    ImplSkipBytes(ref state, 8);
                    return;
                case WireType.String:
                    long len = (long)ReadUInt64Varint(ref state);
                    ImplSkipBytes(ref state, len);
                    return;
                case WireType.Variant:
                case WireType.SignedVariant:
                    ReadUInt64Varint(ref state); // and drop it
                    return;
                case WireType.StartGroup:
                    int originalFieldNumber = this._fieldNumber;
                    _depth++; // need to satisfy the sanity-checks in ReadFieldHeader
                    while (ReadFieldHeader(ref state) > 0) { SkipField(ref state); }
                    _depth--;
                    if (WireType == WireType.EndGroup && _fieldNumber == originalFieldNumber)
                    { // we expect to exit in a similar state to how we entered
                        WireType = ProtoBuf.WireType.None;
                        return;
                    }
                    throw CreateWireTypeException(ref state);
                case WireType.None: // treat as explicit errorr
                case WireType.EndGroup: // treat as explicit error
                default: // treat as implicit error
                    throw CreateWireTypeException(ref state);
            }
        }

        /// <summary>
        /// Reads an unsigned 64-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public ulong ReadUInt64()
        {
            State state = default;
            return ReadUInt64(ref state);
        }

        /// <summary>
        /// Reads an unsigned 64-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        public ulong ReadUInt64(ref State state)
        {
            switch (WireType)
            {
                case WireType.Variant:
                    return ReadUInt64Varint(ref state);
                case WireType.Fixed32:
                    return ImplReadUInt32Fixed(ref state);
                case WireType.Fixed64:
                    return ImplReadUInt64Fixed(ref state);
                default:
                    throw CreateWireTypeException(ref state);
            }
        }

        /// <summary>
        /// Reads a single-precision number from the stream; supported wire-types: Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public float ReadSingle()
        {
            State state = default;
            return ReadSingle(ref state);
        }

        /// <summary>
        /// Reads a single-precision number from the stream; supported wire-types: Fixed32, Fixed64
        /// </summary>
        public float ReadSingle(ref State state)
        {
            switch (WireType)
            {
                case WireType.Fixed32:
                    {
                        int value = ReadInt32(ref state);
#if FEAT_SAFE
                        return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
#else
                        unsafe { return *(float*)&value; }
#endif
                    }
                case WireType.Fixed64:
                    {
                        double value = ReadDouble(ref state);
                        float f = (float)value;
                        if (float.IsInfinity(f) && !double.IsInfinity(value))
                        {
                            throw AddErrorData(new OverflowException(), this, ref state);
                        }
                        return f;
                    }
                default:
                    throw CreateWireTypeException(ref state);
            }
        }

        /// <summary>
        /// Reads a boolean value from the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public bool ReadBoolean()
        {
            State state = default;
            return ReadBoolean(ref state);
        }

        /// <summary>
        /// Reads a boolean value from the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        public bool ReadBoolean(ref State state)
        {
            return ReadUInt32(ref state) != 0;
        }

        private protected static readonly byte[] EmptyBlob = new byte[0];
        /// <summary>
        /// Reads a byte-sequence from the stream, appending them to an existing byte-sequence (which can be null); supported wire-types: String
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static byte[] AppendBytes(byte[] value, ProtoReader reader)
        {
            State state = default;
            return reader.AppendBytes(ref state, value);
        }

        /// <summary>
        /// Reads a byte-sequence from the stream, appending them to an existing byte-sequence (which can be null); supported wire-types: String
        /// </summary>
        public static byte[] AppendBytes(byte[] value, ProtoReader reader, ref State state)
            => reader.AppendBytes(ref state, value);

        private protected byte[] AppendBytes(ref State state, byte[] value)
        {
            {
                switch (WireType)
                {
                    case WireType.String:
                        int len = (int)ReadUInt32Varint(ref state, Read32VarintMode.Signed);
                        WireType = WireType.None;
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
                        ImplReadBytes(ref state, new ArraySegment<byte>(value, offset, len));
                        return value;
                    case WireType.Variant:
                        return new byte[0];
                    default:
                        throw CreateWireTypeException(ref state);
                }
            }
        }

        private protected abstract void ImplReadBytes(ref State state, ArraySegment<byte> target);

        //static byte[] ReadBytes(Stream stream, int length)
        //{
        //    if (stream == null) throw new ArgumentNullException("stream");
        //    if (length < 0) throw new ArgumentOutOfRangeException("length");
        //    byte[] buffer = new byte[length];
        //    int offset = 0, read;
        //    while (length > 0 && (read = stream.Read(buffer, offset, length)) > 0)
        //    {
        //        length -= read;
        //    }
        //    if (length > 0) ThrowEoF();
        //    return buffer;
        //}
        private static int ReadByteOrThrow(Stream source)
        {
            int val = source.ReadByte();
            if (val < 0) ThrowEoF();
            return val;
        }

        /// <summary>
        /// Reads the length-prefix of a message from a stream without buffering additional data, allowing a fixed-length
        /// reader to be created.
        /// </summary>
        public static int ReadLengthPrefix(Stream source, bool expectHeader, PrefixStyle style, out int fieldNumber)
            => ReadLengthPrefix(source, expectHeader, style, out fieldNumber, out int bytesRead);

        /// <summary>
        /// Reads a little-endian encoded integer. An exception is thrown if the data is not all available.
        /// </summary>
        public static int DirectReadLittleEndianInt32(Stream source)
        {
            return ReadByteOrThrow(source)
                | (ReadByteOrThrow(source) << 8)
                | (ReadByteOrThrow(source) << 16)
                | (ReadByteOrThrow(source) << 24);
        }

        /// <summary>
        /// Reads a big-endian encoded integer. An exception is thrown if the data is not all available.
        /// </summary>
        public static int DirectReadBigEndianInt32(Stream source)
        {
            return (ReadByteOrThrow(source) << 24)
                 | (ReadByteOrThrow(source) << 16)
                 | (ReadByteOrThrow(source) << 8)
                 | ReadByteOrThrow(source);
        }

        /// <summary>
        /// Reads a varint encoded integer. An exception is thrown if the data is not all available.
        /// </summary>
        public static int DirectReadVarintInt32(Stream source)
        {
            int bytes = TryReadUInt64Varint(source, out ulong val);
            if (bytes <= 0) ThrowEoF();
            return checked((int)val);
        }

        /// <summary>
        /// Reads a string (of a given lenth, in bytes) directly from the source into a pre-existing buffer. An exception is thrown if the data is not all available.
        /// </summary>
        public static void DirectReadBytes(Stream source, byte[] buffer, int offset, int count)
        {
            int read;
            if (source == null) throw new ArgumentNullException(nameof(source));
            while (count > 0 && (read = source.Read(buffer, offset, count)) > 0)
            {
                count -= read;
                offset += read;
            }
            if (count > 0) ThrowEoF();
        }

        /// <summary>
        /// Reads a given number of bytes directly from the source. An exception is thrown if the data is not all available.
        /// </summary>
        public static byte[] DirectReadBytes(Stream source, int count)
        {
            byte[] buffer = new byte[count];
            DirectReadBytes(source, buffer, 0, count);
            return buffer;
        }

        /// <summary>
        /// Reads a string (of a given lenth, in bytes) directly from the source. An exception is thrown if the data is not all available.
        /// </summary>
        public static string DirectReadString(Stream source, int length)
        {
            byte[] buffer = new byte[length];
            DirectReadBytes(source, buffer, 0, length);
            return Encoding.UTF8.GetString(buffer, 0, length);
        }

        /// <summary>
        /// Reads the length-prefix of a message from a stream without buffering additional data, allowing a fixed-length
        /// reader to be created.
        /// </summary>
        public static int ReadLengthPrefix(Stream source, bool expectHeader, PrefixStyle style, out int fieldNumber, out int bytesRead)
        {
            if (style == PrefixStyle.None)
            {
                bytesRead = fieldNumber = 0;
                return int.MaxValue; // avoid the long.maxvalue causing overflow
            }
            long len64 = ReadLongLengthPrefix(source, expectHeader, style, out fieldNumber, out bytesRead);
            return checked((int)len64);
        }

        /// <summary>
        /// Reads the length-prefix of a message from a stream without buffering additional data, allowing a fixed-length
        /// reader to be created.
        /// </summary>
        public static long ReadLongLengthPrefix(Stream source, bool expectHeader, PrefixStyle style, out int fieldNumber, out int bytesRead)
        {
            fieldNumber = 0;
            switch (style)
            {
                case PrefixStyle.None:
                    bytesRead = 0;
                    return long.MaxValue;
                case PrefixStyle.Base128:
                    ulong val;
                    int tmpBytesRead;
                    bytesRead = 0;
                    if (expectHeader)
                    {
                        tmpBytesRead = ProtoReader.TryReadUInt64Varint(source, out val);
                        bytesRead += tmpBytesRead;
                        if (tmpBytesRead > 0)
                        {
                            if ((val & 7) != (uint)WireType.String)
                            { // got a header, but it isn't a string
                                throw new InvalidOperationException($"Unexpected wire-type: {(WireType)(val & 7)}, expected {WireType.String})");
                            }
                            fieldNumber = (int)(val >> 3);
                            tmpBytesRead = ProtoReader.TryReadUInt64Varint(source, out val);
                            bytesRead += tmpBytesRead;
                            if (bytesRead == 0) ThrowEoF(); // got a header, but no length
                            return (long)val;
                        }
                        else
                        { // no header
                            bytesRead = 0;
                            return -1;
                        }
                    }
                    // check for a length
                    tmpBytesRead = ProtoReader.TryReadUInt64Varint(source, out val);
                    bytesRead += tmpBytesRead;
                    return bytesRead < 0 ? -1 : (long)val;

                case PrefixStyle.Fixed32:
                    {
                        int b = source.ReadByte();
                        if (b < 0)
                        {
                            bytesRead = 0;
                            return -1;
                        }
                        bytesRead = 4;
                        return b
                             | (ReadByteOrThrow(source) << 8)
                             | (ReadByteOrThrow(source) << 16)
                             | (ReadByteOrThrow(source) << 24);
                    }
                case PrefixStyle.Fixed32BigEndian:
                    {
                        int b = source.ReadByte();
                        if (b < 0)
                        {
                            bytesRead = 0;
                            return -1;
                        }
                        bytesRead = 4;
                        return (b << 24)
                            | (ReadByteOrThrow(source) << 16)
                            | (ReadByteOrThrow(source) << 8)
                            | ReadByteOrThrow(source);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(style));
            }
        }

        private static void ThrowEoF()
        {
            State state = default;
            ThrowEoF(null, ref state);
        }

        /// <summary>Read a varint if possible</summary>
        /// <returns>The number of bytes consumed; 0 if no data available</returns>
        private static int TryReadUInt64Varint(Stream source, out ulong value)
        {
            value = 0;
            int b = source.ReadByte();
            if (b < 0) { return 0; }
            value = (uint)b;
            if ((value & 0x80) == 0) { return 1; }
            value &= 0x7F;
            int bytesRead = 1, shift = 7;
            while (bytesRead < 9)
            {
                b = source.ReadByte();
                if (b < 0) ThrowEoF();
                value |= ((ulong)b & 0x7F) << shift;
                shift += 7;
                bytesRead++;

                if ((b & 0x80) == 0) return bytesRead;
            }
            b = source.ReadByte();
            if (b < 0) ThrowEoF();
            if ((b & 1) == 0) // only use 1 bit from the last byte
            {
                value |= ((ulong)b & 0x7F) << shift;
                return ++bytesRead;
            }
            throw new OverflowException();
        }

        private protected abstract bool IsFullyConsumed(ref State state);

        internal void CheckFullyConsumed(ref State state)
        {
            if (!IsFullyConsumed(ref state))
            {
                throw AddErrorData(new ProtoException("Incorrect number of bytes consumed"), this, ref state);
            }
        }

        internal static void Seek(Stream source, long count, byte[] buffer)
        {
            if (source.CanSeek)
            {
                source.Seek(count, SeekOrigin.Current);
                count = 0;
            }
            else if (buffer != null)
            {
                int bytesRead;
                while (count > buffer.Length && (bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    count -= bytesRead;
                }
                while (count > 0 && (bytesRead = source.Read(buffer, 0, (int)count)) > 0)
                {
                    count -= bytesRead;
                }
            }
            else // borrow a buffer
            {
                buffer = BufferPool.GetBuffer();
                try
                {
                    int bytesRead;
                    while (count > buffer.Length && (bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        count -= bytesRead;
                    }
                    while (count > 0 && (bytesRead = source.Read(buffer, 0, (int)count)) > 0)
                    {
                        count -= bytesRead;
                    }
                }
                finally
                {
                    BufferPool.ReleaseBufferToPool(ref buffer);
                }
            }
            if (count > 0) ThrowEoF();
        }
#pragma warning disable RCS1163
        internal static Exception AddErrorData(Exception exception, ProtoReader source, ref State state)
#pragma warning restore RCS1163
        {
#if !CF && !PORTABLE
            if (exception != null && source != null && !exception.Data.Contains("protoSource"))
            {
                exception.Data.Add("protoSource", string.Format("tag={0}; wire-type={1}; offset={2}; depth={3}",
                    source._fieldNumber, source.WireType, source._longPosition, source._depth));
            }
#endif
            return exception;
        }

        /// <summary>
        /// Copies the current field into the instance as extension data
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public void AppendExtensionData(IExtensible instance)
        {
            State state = default;
            AppendExtensionData(ref state, instance);
        }

        /// <summary>
        /// Copies the current field into the instance as extension data
        /// </summary>
        public void AppendExtensionData(ref State state, IExtensible instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            IExtension extn = instance.GetExtensionObject(true);
            bool commit = false;
            // unusually we *don't* want "using" here; the "finally" does that, with
            // the extension object being responsible for disposal etc
            Stream dest = extn.BeginAppend();
            try
            {
                //TODO: replace this with stream-based, buffered raw copying
                using (ProtoWriter writer = ProtoWriter.Create(out var writeState, dest, _model, null))
                {
                    AppendExtensionField(ref state, writer, ref writeState);
                    writer.Close(ref writeState);
                }
                commit = true;
            }
            finally { extn.EndAppend(dest, commit); }
        }

        private void AppendExtensionField(ref ProtoReader.State readState, ProtoWriter writer, ref ProtoWriter.State writeState)
        {
            //TODO: replace this with stream-based, buffered raw copying
            ProtoWriter.WriteFieldHeader(_fieldNumber, WireType, writer, ref writeState);
            switch (WireType)
            {
                case WireType.Fixed32:
                    ProtoWriter.WriteInt32(ReadInt32(ref readState), writer, ref writeState);
                    return;
                case WireType.Variant:
                case WireType.SignedVariant:
                case WireType.Fixed64:
                    ProtoWriter.WriteInt64(ReadInt64(ref readState), writer, ref writeState);
                    return;
                case WireType.String:
                    ProtoWriter.WriteBytes(AppendBytes(null, this, ref readState), writer, ref writeState);
                    return;
                case WireType.StartGroup:
                    SubItemToken readerToken = StartSubItem(this, ref readState),
                        writerToken = ProtoWriter.StartSubItem(null, writer, ref writeState);
                    while (ReadFieldHeader(ref readState) > 0) { AppendExtensionField(ref readState, writer, ref writeState); }
                    EndSubItem(readerToken, this, ref readState);
                    ProtoWriter.EndSubItem(writerToken, writer, ref writeState);
                    return;
                case WireType.None: // treat as explicit errorr
                case WireType.EndGroup: // treat as explicit error
                default: // treat as implicit error
                    throw CreateWireTypeException(ref readState);
            }
        }

        /// <summary>
        /// Indicates whether the reader still has data remaining in the current sub-item,
        /// additionally setting the wire-type for the next field if there is more data.
        /// This is used when decoding packed data.
        /// </summary>
        public static bool HasSubValue(ProtoBuf.WireType wireType, ProtoReader source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            // check for virtual end of stream
            if (source.blockEnd64 <= source._longPosition || wireType == WireType.EndGroup) { return false; }
            source.WireType = wireType;
            return true;
        }

        internal int GetTypeKey(ref Type type)
        {
            return _model.GetKey(ref type);
        }

        internal NetObjectCache NetCache => netCache;

        internal Type DeserializeType(string value)
        {
            return TypeModel.DeserializeType(_model, value);
        }

        internal void SetRootObject(object value)
        {
            netCache.SetKeyedObject(NetObjectCache.Root, value);
            trapCount--;
        }

        /// <summary>
        /// Utility method, not intended for public use; this helps maintain the root object is complex scenarios
        /// </summary>
        public static void NoteObject(object value, ProtoReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (reader.trapCount != 0)
            {
                reader.netCache.RegisterTrappedObject(value);
                reader.trapCount--;
            }
        }

        /// <summary>
        /// Reads a Type from the stream, using the model's DynamicTypeFormatting if appropriate; supported wire-types: String
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public Type ReadType()
        {
            State state = default;
            return ReadType(ref state);
        }

        /// <summary>
        /// Reads a Type from the stream, using the model's DynamicTypeFormatting if appropriate; supported wire-types: String
        /// </summary>
        public Type ReadType(ref State state)
        {
            return TypeModel.DeserializeType(_model, ReadString(ref state));
        }

        internal void TrapNextObject(int newObjectKey)
        {
            trapCount++;
            netCache.SetKeyedObject(newObjectKey, null); // use null as a temp
        }

        /// <summary>
        /// Merge two objects using the details from the current reader; this is used to change the type
        /// of objects when an inheritance relationship is discovered later than usual during deserilazation.
        /// </summary>
        public static object Merge(ProtoReader parent, object from, object to)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            TypeModel model = parent.Model;
            SerializationContext ctx = parent.Context;
            if (model == null) throw new InvalidOperationException("Types cannot be merged unless a type-model has been specified");
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, from, ctx);
                ms.Position = 0;
                return model.Deserialize(ms, to, null);
            }
        }

        internal abstract void Recycle();

        /// <summary>
        /// Create an EOF
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception EoF(ProtoReader reader, ref State state)
        {
            return AddErrorData(new EndOfStreamException(), reader, ref state);
        }

        /// <summary>
        /// throw an EOF
        /// </summary>
        protected static void ThrowEoF(ProtoReader reader, ref State state)
        {
            throw EoF(reader, ref state);
        }

        /// <summary>
        /// Create an Overflow
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception Overflow(ProtoReader reader, ref State state)
        {
            return AddErrorData(new OverflowException(), reader, ref state);
        }

        internal static void ThrowOverflow(ProtoReader reader, ref State state)
        {
            throw Overflow(reader, ref state);
        }
        internal static void ThrowOverflow()
        {
            State state = default;
            ThrowOverflow(null, ref state);
        }
    }
}
