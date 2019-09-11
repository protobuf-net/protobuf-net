using System;
using System.IO;
using System.Text;
using ProtoBuf.Meta;

namespace ProtoBuf
{
    /// <summary>
    /// <para>Represents an output stream for writing protobuf data.</para>
    /// <para>
    /// Why is the API backwards (static methods with writer arguments)?
    /// See: http://marcgravell.blogspot.com/2010/03/last-will-be-first-and-first-will-be.html
    /// </para>
    /// </summary>
    public abstract partial class ProtoWriter : IDisposable
    {
        internal const string UseStateAPI = ProtoReader.UseStateAPI;

        private TypeModel model;
        private int packedFieldNumber;

        void IDisposable.Dispose()
        {
            Dispose();
        }

        /// <summary>
        /// Write an encapsulated sub-object, using the supplied unique key (reprasenting a type).
        /// </summary>
        /// <param name="value">The object to write.</param>
        /// <param name="key">The key that uniquely identifies the type within the model.</param>
        /// <param name="writer">The destination.</param>
        [Obsolete(ProtoWriter.UseStateAPI, false)]
        public static void WriteObject(object value, int key, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteObject(value, key, writer, ref state);
        }

        /// <summary>
        /// Write an encapsulated sub-object, using the supplied unique key (reprasenting a type).
        /// </summary>
        /// <param name="value">The object to write.</param>
        /// <param name="key">The key that uniquely identifies the type within the model.</param>
        /// <param name="writer">The destination.</param>
        /// <param name="state">Writer state</param>
        public static void WriteObject(object value, int key, ProtoWriter writer, ref State state)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (writer.model == null)
            {
                throw new InvalidOperationException("Cannot serialize sub-objects unless a model is provided");
            }

            SubItemToken token = StartSubItem(value, writer, ref state);
            if (key >= 0)
            {
                writer.model.Serialize(writer, ref state, key, value);
            }
            else if (writer.model != null && writer.model.TrySerializeAuxiliaryType(writer, ref state, value.GetType(), DataFormat.Default, TypeModel.ListItemTag, value, false, null))
            {
                // all ok
            }
            else
            {
                TypeModel.ThrowUnexpectedType(value.GetType());
            }

            EndSubItem(token, writer, ref state);
        }
        /// <summary>
        /// Write an encapsulated sub-object, using the supplied unique key (reprasenting a type) - but the
        /// caller is asserting that this relationship is non-recursive; no recursion check will be
        /// performed.
        /// </summary>
        /// <param name="value">The object to write.</param>
        /// <param name="key">The key that uniquely identifies the type within the model.</param>
        /// <param name="writer">The destination.</param>
        [Obsolete(UseStateAPI, false)]
        public static void WriteRecursionSafeObject(object value, int key, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteRecursionSafeObject(value, key, writer, ref state);
        }
        /// <summary>
        /// Write an encapsulated sub-object, using the supplied unique key (reprasenting a type) - but the
        /// caller is asserting that this relationship is non-recursive; no recursion check will be
        /// performed.
        /// </summary>
        /// <param name="value">The object to write.</param>
        /// <param name="key">The key that uniquely identifies the type within the model.</param>
        /// <param name="writer">The destination.</param>
        /// <param name="state">Writer state</param>
        public static void WriteRecursionSafeObject(object value, int key, ProtoWriter writer, ref State state)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (writer.model == null)
            {
                throw new InvalidOperationException("Cannot serialize sub-objects unless a model is provided");
            }
            SubItemToken token = StartSubItem(null, writer, ref state);
            writer.model.Serialize(writer, ref state, key, value);
            EndSubItem(token, writer, ref state);
        }

        internal static void WriteObject(ProtoWriter writer, ref State state, object value, int key, PrefixStyle style, int fieldNumber)
        {
            if (writer.model == null)
            {
                throw new InvalidOperationException("Cannot serialize sub-objects unless a model is provided");
            }
            if (writer.WireType != WireType.None) throw ProtoWriter.CreateException(writer);

            switch (style)
            {
                case PrefixStyle.Base128:
                    writer.WireType = WireType.String;
                    writer.fieldNumber = fieldNumber;
                    if (fieldNumber > 0) WriteHeaderCore(fieldNumber, WireType.String, writer, ref state);
                    break;
                case PrefixStyle.Fixed32:
                case PrefixStyle.Fixed32BigEndian:
                    writer.fieldNumber = 0;
                    writer.WireType = WireType.Fixed32;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(style));
            }
            SubItemToken token = writer.StartSubItem(ref state, value, style);
            if (key < 0)
            {
                if (!writer.model.TrySerializeAuxiliaryType(writer, ref state, value.GetType(), DataFormat.Default, TypeModel.ListItemTag, value, false, null))
                {
                    TypeModel.ThrowUnexpectedType(value.GetType());
                }
            }
            else
            {
                writer.model.Serialize(writer, ref state, key, value);
            }
            writer.EndSubItem(ref state, token, style);
        }

        internal int GetTypeKey(ref Type type)
        {
            return model.GetKey(ref type);
        }

        internal NetObjectCache NetCache { get; } = new NetObjectCache();

        private int fieldNumber;

        internal WireType WireType { get; set; }

        /// <summary>
        /// Writes a field-header, indicating the format of the next data we plan to write.
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteFieldHeader(int fieldNumber, WireType wireType, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteFieldHeader(fieldNumber, wireType, writer, ref state);
        }

        /// <summary>
        /// Writes a field-header, indicating the format of the next data we plan to write.
        /// </summary>
        public static void WriteFieldHeader(int fieldNumber, WireType wireType, ProtoWriter writer, ref State state)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (writer.WireType != WireType.None)
            {
                throw new InvalidOperationException("Cannot write a " + wireType.ToString()
                + " header until the " + writer.WireType.ToString() + " data has been written");
            }
            if (fieldNumber < 0) throw new ArgumentOutOfRangeException(nameof(fieldNumber));
#if DEBUG
            switch (wireType)
            {   // validate requested header-type
                case WireType.Fixed32:
                case WireType.Fixed64:
                case WireType.String:
                case WireType.StartGroup:
                case WireType.SignedVariant:
                case WireType.Variant:
                    break; // fine
                case WireType.None:
                case WireType.EndGroup:
                default:
                    throw new ArgumentException("Invalid wire-type: " + wireType.ToString(), nameof(wireType));
            }
#endif
            writer._needFlush = true;
            if (writer.packedFieldNumber == 0)
            {
                writer.fieldNumber = fieldNumber;
                writer.WireType = wireType;
                WriteHeaderCore(fieldNumber, wireType, writer, ref state);
            }
            else if (writer.packedFieldNumber == fieldNumber)
            { // we'll set things up, but note we *don't* actually write the header here
                switch (wireType)
                {
                    case WireType.Fixed32:
                    case WireType.Fixed64:
                    case WireType.Variant:
                    case WireType.SignedVariant:
                        break; // fine
                    default:
                        throw new InvalidOperationException("Wire-type cannot be encoded as packed: " + wireType.ToString());
                }
                writer.fieldNumber = fieldNumber;
                writer.WireType = wireType;
            }
            else
            {
                throw new InvalidOperationException("Field mismatch during packed encoding; expected " + writer.packedFieldNumber.ToString() + " but received " + fieldNumber.ToString());
            }
        }
        internal static void WriteHeaderCore(int fieldNumber, WireType wireType, ProtoWriter writer, ref State state)
        {
            uint header = (((uint)fieldNumber) << 3)
                | (((uint)wireType) & 7);
            int bytes = writer.ImplWriteVarint32(ref state, header);
            writer.Advance(bytes);
        }

        /// <summary>
        /// Writes a byte-array to the stream; supported wire-types: String
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteBytes(byte[] data, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteBytes(data, writer, ref state);
        }

        /// <summary>
        /// Writes a byte-array to the stream; supported wire-types: String
        /// </summary>
        public static void WriteBytes(byte[] data, ProtoWriter writer, ref State state)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            WriteBytes(data, 0, data.Length, writer, ref state);
        }

        /// <summary>
        /// Writes a byte-array to the stream; supported wire-types: String
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteBytes(byte[] data, int offset, int length, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteBytes(data, offset, length, writer, ref state);
        }

        /// <summary>
        /// Writes a byte-array to the stream; supported wire-types: String
        /// </summary>
        public static void WriteBytes(byte[] data, int offset, int length, ProtoWriter writer, ref State state)
        {
            switch (writer.WireType)
            {
                case WireType.Fixed32:
                    if (length != 4) throw new ArgumentException(nameof(length));
                    writer.ImplWriteBytes(ref state, data, offset, 4);
                    writer.AdvanceAndReset(4);
                    return;
                case WireType.Fixed64:
                    if (length != 8) throw new ArgumentException(nameof(length));
                    writer.ImplWriteBytes(ref state, data, offset, 8);
                    writer.AdvanceAndReset(8);
                    return;
                case WireType.String:
                    writer.AdvanceAndReset(writer.ImplWriteVarint32(ref state, (uint)length) + length);
                    if (length == 0) return;
                    writer.ImplWriteBytes(ref state, data, offset, length);
                    break;
                default:
                    ThrowException(writer);
                    break;
            }
        }

        /// <summary>
        /// Writes a byte-array to the stream; supported wire-types: String
        /// </summary>
        public static void WriteBytes(System.Buffers.ReadOnlySequence<byte> data, ProtoWriter writer, ref State state)
        {
            int length = checked((int)data.Length);
            switch (writer.WireType)
            {
                case WireType.Fixed32:
                    if (length != 4) throw new ArgumentException(nameof(length));
                    writer.ImplWriteBytes(ref state, data);
                    writer.AdvanceAndReset(4);
                    return;
                case WireType.Fixed64:
                    if (length != 8) throw new ArgumentException(nameof(length));
                    writer.ImplWriteBytes(ref state, data);
                    writer.AdvanceAndReset(8);
                    return;
                case WireType.String:
                    writer.AdvanceAndReset(writer.ImplWriteVarint32(ref state, (uint)length) + length);
                    if (length == 0) return;
                    writer.ImplWriteBytes(ref state, data);
                    break;
                default:
                    ThrowException(writer);
                    break;
            }
        }

        private int depth = 0;
        private const int RecursionCheckDepth = 25;

        /// <summary>
        /// Indicates the start of a nested record.
        /// </summary>
        /// <param name="instance">The instance to write.</param>
        /// <param name="writer">The destination.</param>
        /// <returns>A token representing the state of the stream; this token is given to EndSubItem.</returns>
        [Obsolete(UseStateAPI, false)]
        public static SubItemToken StartSubItem(object instance, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            return writer.StartSubItem(ref state, instance, PrefixStyle.Base128);
        }

        /// <summary>
        /// Indicates the start of a nested record.
        /// </summary>
        /// <param name="instance">The instance to write.</param>
        /// <param name="writer">The destination.</param>
        /// <param name="state">Writer state</param>
        /// <returns>A token representing the state of the stream; this token is given to EndSubItem.</returns>
        public static SubItemToken StartSubItem(object instance, ProtoWriter writer, ref State state)
            => writer.StartSubItem(ref state, instance, PrefixStyle.Base128);

        private SubItemToken StartSubItem(ref State state, object instance, PrefixStyle style)
        {
            if (++depth > RecursionCheckDepth)
            {
                CheckRecursionStackAndPush(instance);
            }
            if (packedFieldNumber != 0) throw new InvalidOperationException("Cannot begin a sub-item while performing packed encoding");
            switch (WireType)
            {
                case WireType.StartGroup:
                    WireType = WireType.None;
                    return new SubItemToken((long)(-fieldNumber));
                case WireType.Fixed32:
                    switch (style)
                    {
                        case PrefixStyle.Fixed32:
                        case PrefixStyle.Fixed32BigEndian:
                            break; // OK
                        default:
                            throw CreateException(this);
                    }
                    goto case WireType.String;
                case WireType.String:
#if DEBUG
                    if (model != null && model.ForwardsOnly)
                    {
                        throw new ProtoException("Should not be buffering data: " + instance ?? "(null)");
                    }
#endif
                    return ImplStartLengthPrefixedSubItem(ref state, instance, style);
                default:
                    throw CreateException(this);
            }
        }

        private MutableList recursionStack;
        private void CheckRecursionStackAndPush(object instance)
        {
            int hitLevel;
            if (recursionStack == null) { recursionStack = new MutableList(); }
            else if (instance != null && (hitLevel = recursionStack.IndexOfReference(instance)) >= 0)
            {
#if DEBUG
                Helpers.DebugWriteLine("Stack:");
                foreach (object obj in recursionStack)
                {
                    Helpers.DebugWriteLine(obj == null ? "<null>" : obj.ToString());
                }
                Helpers.DebugWriteLine(instance == null ? "<null>" : instance.ToString());
#endif
#pragma warning disable RCS1097 // Remove redundant 'ToString' call.
                throw new ProtoException("Possible recursion detected (offset: " + (recursionStack.Count - hitLevel).ToString() + " level(s)): " + instance.ToString());
#pragma warning restore RCS1097 // Remove redundant 'ToString' call.
            }
            recursionStack.Add(instance);
        }
        private void PopRecursionStack() { recursionStack.RemoveLast(); }

        /// <summary>
        /// Indicates the end of a nested record.
        /// </summary>
        /// <param name="token">The token obtained from StartubItem.</param>
        /// <param name="writer">The destination.</param>
        [Obsolete(UseStateAPI, false)]
        public static void EndSubItem(SubItemToken token, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            writer.EndSubItem(ref state, token, PrefixStyle.Base128);
        }

        /// <summary>
        /// Indicates the end of a nested record.
        /// </summary>
        /// <param name="token">The token obtained from StartubItem.</param>
        /// <param name="writer">The destination.</param>
        /// <param name="state">Writer state</param>
        public static void EndSubItem(SubItemToken token, ProtoWriter writer, ref State state)
            => writer.EndSubItem(ref state, token, PrefixStyle.Base128);

        private void EndSubItem(ref State state, SubItemToken token, PrefixStyle style)
        {
            if (WireType != WireType.None) { throw CreateException(this); }
            int value = (int)token.value64;
            if (depth <= 0) throw CreateException(this);
            if (depth-- > RecursionCheckDepth)
            {
                PopRecursionStack();
            }
            packedFieldNumber = 0; // ending the sub-item always wipes packed encoding
            if (value < 0)
            {   // group - very simple append
                WriteHeaderCore(-value, WireType.EndGroup, this, ref state);
                WireType = WireType.None;
                return;
            }

            ImplEndLengthPrefixedSubItem(ref state, token, style);
        }

        /// <summary>
        /// Creates a new writer against a stream
        /// </summary>
        /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to serialize sub-objects</param>
        /// <param name="context">Additional context about this serialization operation</param>
        protected private ProtoWriter(TypeModel model, SerializationContext context)
        {
            this.model = model;
            WireType = WireType.None;
            if (context == null) { context = SerializationContext.Default; }
            else { context.Freeze(); }
            Context = context;
        }

        /// <summary>
        /// Addition information about this serialization operation.
        /// </summary>
        public SerializationContext Context { get; }

        protected private virtual void Dispose()
        {
            if (depth == 0 && _needFlush && ImplDemandFlushOnDispose)
            {
                throw new InvalidOperationException("Writer was diposed without being flushed; data may be lost - you should ensure that Flush (or Abandon) is called");
            }
            model = null;
        }

        /// <summary>
        /// Abandon any pending unflushed data
        /// </summary>
        public void Abandon() { _needFlush = false; }

        private bool _needFlush;

        // note that this is used by some of the unit tests and should not be removed
#pragma warning disable RCS1163 // Unused parameter.
        internal static long GetLongPosition(ProtoWriter writer, ref State state) { return writer._position64; }
#pragma warning restore RCS1163 // Unused parameter.

        private long _position64;
        protected private void Advance(long count) => _position64 += count;
        protected private void AdvanceAndReset(int count)
        {
            _position64 += count;
            WireType = WireType.None;
        }
        protected private void AdvanceAndReset(long count)
        {
            _position64 += count;
            WireType = WireType.None;
        }

        /// <summary>
        /// Flushes data to the underlying stream, and releases any resources. The underlying stream is *not* disposed
        /// by this operation.
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public void Close()
        {
            State state = DefaultState();
            Close(ref state);
        }
        /// <summary>
        /// Flushes data to the underlying stream, and releases any resources. The underlying stream is *not* disposed
        /// by this operation.
        /// </summary>
        public void Close(ref State state)
        {
            CheckClear(ref state);
            Dispose();
        }

        internal void CheckClear(ref State state)
        {
            if (depth != 0 || !TryFlush(ref state)) throw new InvalidOperationException("The writer is in an incomplete state");
            _needFlush = false; // because we ^^^ *JUST DID*
        }

        /// <summary>
        /// Get the TypeModel associated with this writer
        /// </summary>
        public TypeModel Model => model;

        private protected static readonly UTF8Encoding UTF8 = new UTF8Encoding();

        private static uint Zig(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }

        private static ulong Zig(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }

        /// <summary>
        /// Writes a string to the stream; supported wire-types: String
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteString(string value, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteString(value, writer, ref state);
        }
        /// <summary>
        /// Writes a string to the stream; supported wire-types: String
        /// </summary>
        public static void WriteString(string value, ProtoWriter writer, ref State state)
        {
            switch (writer.WireType)
            {
                case WireType.String:
                    if (string.IsNullOrEmpty(value))
                    {
                        writer.AdvanceAndReset(writer.ImplWriteVarint32(ref state, 0));
                    }
                    else
                    {
                        var len = UTF8.GetByteCount(value);
                        writer.AdvanceAndReset(writer.ImplWriteVarint32(ref state, (uint)len) + len);
                        writer.ImplWriteString(ref state, value, len);
                    }
                    break;
                default:
                    ThrowException(writer);
                    break;
            }
        }

        protected private abstract void ImplWriteString(ref State state, string value, int expectedBytes);
        protected private abstract int ImplWriteVarint32(ref State state, uint value);
        protected private abstract int ImplWriteVarint64(ref State state, ulong value);
        protected private abstract void ImplWriteFixed32(ref State state, uint value);
        protected private abstract void ImplWriteFixed64(ref State state, ulong value);
        protected private abstract void ImplWriteBytes(ref State state, byte[] data, int offset, int length);
        protected private abstract void ImplWriteBytes(ref State state, System.Buffers.ReadOnlySequence<byte> data);
        protected private abstract void ImplCopyRawFromStream(ref State state, Stream source);
        private protected abstract SubItemToken ImplStartLengthPrefixedSubItem(ref State state, object instance, PrefixStyle style);
        protected private abstract void ImplEndLengthPrefixedSubItem(ref State state, SubItemToken token, PrefixStyle style);
        protected private abstract bool ImplDemandFlushOnDispose { get; }

        /// <summary>
        /// Writes any uncommitted data to the output
        /// </summary>
        public void Flush(ref State state)
        {
            if (TryFlush(ref state))
            {
                _needFlush = false;
            }
        }

        /// <summary>
        /// Writes any buffered data (if possible) to the underlying stream.
        /// </summary>
        /// <param name="state">Wwriter state</param>
        /// <remarks>It is not always possible to fully flush, since some sequences
        /// may require values to be back-filled into the byte-stream.</remarks>
        private protected abstract bool TryFlush(ref State state);

        /// <summary>
        /// Writes an unsigned 64-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteUInt64(ulong value, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteUInt64(value, writer, ref state);
        }
        /// <summary>
        /// Writes an unsigned 64-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        public static void WriteUInt64(ulong value, ProtoWriter writer, ref State state)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            switch (writer.WireType)
            {
                case WireType.Fixed64:
                    writer.ImplWriteFixed64(ref state, value);
                    writer.AdvanceAndReset(8);
                    return;
                case WireType.Variant:
                    int bytes = writer.ImplWriteVarint64(ref state, value);
                    writer.AdvanceAndReset(bytes);
                    return;
                case WireType.Fixed32:
                    writer.ImplWriteFixed32(ref state, checked((uint)value));
                    writer.AdvanceAndReset(4);
                    return;
                default:
                    ThrowException(writer);
                    break;
            }
        }

        /// <summary>
        /// Writes a signed 64-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteInt64(long value, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteInt64(value, writer, ref state);
        }
        /// <summary>
        /// Writes a signed 64-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        public static void WriteInt64(long value, ProtoWriter writer, ref State state)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            switch (writer.WireType)
            {
                case WireType.Fixed64:
                    writer.ImplWriteFixed64(ref state, (ulong)value);
                    writer.AdvanceAndReset(8);
                    return;
                case WireType.Variant:
                    writer.AdvanceAndReset(writer.ImplWriteVarint64(ref state, (ulong)value));
                    return;
                case WireType.SignedVariant:
                    writer.AdvanceAndReset(writer.ImplWriteVarint64(ref state, Zig(value)));
                    return;
                case WireType.Fixed32:
                    writer.ImplWriteFixed32(ref state, checked((uint)(int)value));
                    writer.AdvanceAndReset(4);
                    return;
                default:
                    ThrowException(writer);
                    break;
            }
        }

        /// <summary>
        /// Writes an unsigned 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteUInt32(uint value, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteUInt32(value, writer, ref state);
        }

        /// <summary>
        /// Writes an unsigned 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        public static void WriteUInt32(uint value, ProtoWriter writer, ref State state)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            switch (writer.WireType)
            {
                case WireType.Fixed32:
                    writer.ImplWriteFixed32(ref state, value);
                    writer.AdvanceAndReset(4);
                    return;
                case WireType.Fixed64:
                    writer.ImplWriteFixed64(ref state, value);
                    writer.AdvanceAndReset(8);
                    return;
                case WireType.Variant:
                    int bytes = writer.ImplWriteVarint32(ref state, value);
                    writer.AdvanceAndReset(bytes);
                    return;
                default:
                    ThrowException(writer);
                    break;
            }
        }

        /// <summary>
        /// Writes a signed 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteInt16(short value, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteInt32(value, writer, ref state);
        }

        /// <summary>
        /// Writes a signed 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        public static void WriteInt16(short value, ProtoWriter writer, ref State state)
            => WriteInt32(value, writer, ref state);

        /// <summary>
        /// Writes an unsigned 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteUInt16(ushort value, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteUInt32(value, writer, ref state);
        }

        /// <summary>
        /// Writes an unsigned 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        public static void WriteUInt16(ushort value, ProtoWriter writer, ref State state)
            => WriteUInt32(value, writer, ref state);

        /// <summary>
        /// Writes an unsigned 8-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteByte(byte value, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteUInt32(value, writer, ref state);
        }

        /// <summary>
        /// Writes an unsigned 8-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        public static void WriteByte(byte value, ProtoWriter writer, ref State state)
            => WriteUInt32(value, writer, ref state);

        /// <summary>
        /// Writes a signed 8-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteSByte(sbyte value, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteInt32(value, writer, ref state);
        }
        /// <summary>
        /// Writes a signed 8-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        public static void WriteSByte(sbyte value, ProtoWriter writer, ref State state)
            => WriteInt32(value, writer, ref state);

        /// <summary>
        /// Writes a signed 32-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteInt32(int value, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteInt32(value, writer, ref state);
        }
        /// <summary>
        /// Writes a signed 32-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        public static void WriteInt32(int value, ProtoWriter writer, ref State state)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            switch (writer.WireType)
            {
                case WireType.Fixed32:
                    writer.ImplWriteFixed32(ref state, (uint)value);
                    writer.AdvanceAndReset(4);
                    return;
                case WireType.Fixed64:
                    writer.ImplWriteFixed64(ref state, (ulong)(long)value);
                    writer.AdvanceAndReset(8);
                    return;
                case WireType.Variant:
                    if (value >= 0)
                    {
                        writer.AdvanceAndReset(writer.ImplWriteVarint32(ref state, (uint)value));
                    }
                    else
                    {
                        writer.AdvanceAndReset(writer.ImplWriteVarint64(ref state, (ulong)(long)value));
                    }
                    return;
                case WireType.SignedVariant:
                    writer.AdvanceAndReset(writer.ImplWriteVarint32(ref state, Zig(value)));
                    return;
                default:
                    ThrowException(writer);
                    break;
            }
        }

        /// <summary>
        /// Writes a double-precision number to the stream; supported wire-types: Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteDouble(double value, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteDouble(value, writer, ref state);
        }

        /// <summary>
        /// Writes a double-precision number to the stream; supported wire-types: Fixed32, Fixed64
        /// </summary>
        public static void WriteDouble(double value, ProtoWriter writer, ref State state)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            switch (writer.WireType)
            {
                case WireType.Fixed32:
                    float f = (float)value;
                    if (float.IsInfinity(f) && !double.IsInfinity(value))
                    {
                        throw new OverflowException();
                    }
                    WriteSingle(f, writer, ref state);
                    return;
                case WireType.Fixed64:
#if FEAT_SAFE
                    writer.ImplWriteFixed64(ref state, (ulong)BitConverter.DoubleToInt64Bits(value));
#else
                    unsafe { writer.ImplWriteFixed64(ref state, *(ulong*)&value); }
#endif
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
        [Obsolete(UseStateAPI, false)]
        public static void WriteSingle(float value, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteSingle(value, writer, ref state);
        }

        /// <summary>
        /// Writes a single-precision number to the stream; supported wire-types: Fixed32, Fixed64
        /// </summary>
        public static void WriteSingle(float value, ProtoWriter writer, ref State state)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            switch (writer.WireType)
            {
                case WireType.Fixed32:
#if FEAT_SAFE
                    writer.ImplWriteFixed32(ref state, BitConverter.ToUInt32(BitConverter.GetBytes(value), 0));
#else
                    unsafe { writer.ImplWriteFixed32(ref state, *(uint*)&value); }
#endif                    
                    writer.AdvanceAndReset(4);
                    return;
                case WireType.Fixed64:
                    WriteDouble(value, writer, ref state);
                    return;
                default:
                    ThrowException(writer);
                    break;
            }
        }

        /// <summary>
        /// Throws an exception indicating that the given enum cannot be mapped to a serialized value.
        /// </summary>
        public static void ThrowEnumException(ProtoWriter writer, object enumValue)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
#pragma warning disable RCS1097 // Remove redundant 'ToString' call.
            string rhs = enumValue == null ? "<null>" : (enumValue.GetType().FullName + "." + enumValue.ToString());
#pragma warning restore RCS1097 // Remove redundant 'ToString' call.
            throw new ProtoException("No wire-value is mapped to the enum " + rhs + " at position " + writer._position64.ToString());
        }

        // general purpose serialization exception message
        internal static Exception CreateException(ProtoWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            return new ProtoException("Invalid serialization operation with wire-type " + writer.WireType.ToString() + " at position " + writer._position64.ToString());
        }
        internal static void ThrowException(ProtoWriter writer)
            => throw CreateException(writer);

        /// <summary>
        /// Writes a boolean to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteBoolean(bool value, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteBoolean(value, writer, ref state);
        }

        /// <summary>
        /// Writes a boolean to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        public static void WriteBoolean(bool value, ProtoWriter writer, ref State state)
        {
            ProtoWriter.WriteUInt32(value ? (uint)1 : (uint)0, writer, ref state);
        }

        /// <summary>
        /// Copies any extension data stored for the instance to the underlying stream
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void AppendExtensionData(IExtensible instance, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            AppendExtensionData(instance, writer, ref state);
        }

        /// <summary>
        /// Copies any extension data stored for the instance to the underlying stream
        /// </summary>
        public static void AppendExtensionData(IExtensible instance, ProtoWriter writer, ref State state)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            // we expect the writer to be raw here; the extension data will have the
            // header detail, so we'll copy it implicitly
            if (writer.WireType != WireType.None) throw CreateException(writer);

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
                        writer.ImplWriteBytes(ref state, data.Array, data.Offset, data.Count);
                        writer.Advance(data.Count);
                    }
                    else
                    {
                        writer.ImplCopyRawFromStream(ref state, source);
                    }
                }
                finally { extn.EndQuery(source); }
            }
        }

        /// <summary>
        /// Used for packed encoding; indicates that the next field should be skipped rather than
        /// a field header written. Note that the field number must match, else an exception is thrown
        /// when the attempt is made to write the (incorrect) field. The wire-type is taken from the
        /// subsequent call to WriteFieldHeader. Only primitive types can be packed.
        /// </summary>
        public static void SetPackedField(int fieldNumber, ProtoWriter writer)
        {
            if (fieldNumber <= 0) throw new ArgumentOutOfRangeException(nameof(fieldNumber));
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            writer.packedFieldNumber = fieldNumber;
        }

        /// <summary>
        /// Used for packed encoding; explicitly reset the packed field marker; this is not required
        /// if using StartSubItem/EndSubItem
        /// </summary>
        public static void ClearPackedField(int fieldNumber, ProtoWriter writer)
        {
            if (fieldNumber != writer.packedFieldNumber)
                throw new InvalidOperationException("Field mismatch during packed encoding; expected " + writer.packedFieldNumber.ToString() + " but received " + fieldNumber.ToString());
            writer.packedFieldNumber = 0;
        }

        /// <summary>
        /// Used for packed encoding; writes the length prefix using fixed sizes rather than using
        /// buffering. Only valid for fixed-32 and fixed-64 encoding.
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WritePackedPrefix(int elementCount, WireType wireType, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WritePackedPrefix(elementCount, wireType, writer, ref state);
        }
        /// <summary>
        /// Used for packed encoding; writes the length prefix using fixed sizes rather than using
        /// buffering. Only valid for fixed-32 and fixed-64 encoding.
        /// </summary>
        public static void WritePackedPrefix(int elementCount, WireType wireType, ProtoWriter writer, ref State state)
        {
            if (writer.WireType != WireType.String) throw new InvalidOperationException("Invalid wire-type: " + writer.WireType);
            if (elementCount < 0) throw new ArgumentOutOfRangeException(nameof(elementCount));
            ulong bytes;
            switch (wireType)
            {
                // use long in case very large arrays are enabled
                case WireType.Fixed32: bytes = ((ulong)elementCount) << 2; break; // x4
                case WireType.Fixed64: bytes = ((ulong)elementCount) << 3; break; // x8
                default:
                    throw new ArgumentOutOfRangeException(nameof(wireType), "Invalid wire-type: " + wireType);
            }
            int prefixLength = writer.ImplWriteVarint64(ref state, bytes);
            writer.AdvanceAndReset(prefixLength);
        }

        internal string SerializeType(Type type)
        {
            return TypeModel.SerializeType(model, type);
        }

        /// <summary>
        /// Specifies a known root object to use during reference-tracked serialization
        /// </summary>
        public void SetRootObject(object value)
        {
            NetCache.SetKeyedObject(NetObjectCache.Root, value);
        }

        /// <summary>
        /// Writes a Type to the stream, using the model's DynamicTypeFormatting if appropriate; supported wire-types: String
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteType(Type value, ProtoWriter writer)
        {
            State state = writer.DefaultState();
            WriteType(value, writer, ref state);
        }
        /// <summary>
        /// Writes a Type to the stream, using the model's DynamicTypeFormatting if appropriate; supported wire-types: String
        /// </summary>
        public static void WriteType(Type value, ProtoWriter writer, ref State state)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            WriteString(writer.SerializeType(value), writer, ref state);
        }
    }
}
