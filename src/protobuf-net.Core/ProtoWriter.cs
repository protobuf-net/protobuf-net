using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using ProtoBuf.Internal;
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
    public abstract partial class ProtoWriter : IDisposable, ISerializationContext
    {
        internal const string UseStateAPI = ProtoReader.UseStateAPI,
            PreferWriteSubItem = "If possible, please use the WriteSubItem API; this API may not work correctly with all writers";

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
            writer.DefaultState().WriteRecursionSafeObject(value, key);
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
            writer.DefaultState().WriteFieldHeader(fieldNumber, wireType);
        }

        /// <summary>
        /// Writes a byte-array to the stream; supported wire-types: String
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteBytes(byte[] data, ProtoWriter writer)
        {
            writer.DefaultState().WriteBytes(data);
        }

        /// <summary>
        /// Writes a byte-array to the stream; supported wire-types: String
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteBytes(byte[] data, int offset, int length, ProtoWriter writer)
        {
            writer.DefaultState().WriteBytes(data, offset, length);
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

        private void PreSubItem(object instance)
        {
            if (++depth > RecursionCheckDepth)
            {
                CheckRecursionStackAndPush(instance);
            }
            if (packedFieldNumber != 0) ThrowHelper.ThrowInvalidOperationException("Cannot begin a sub-item while performing packed encoding");
        }

        private List<object> recursionStack;
        private void CheckRecursionStackAndPush(object instance)
        {
            if (recursionStack == null) { recursionStack = new List<object>(); }
            else if (instance != null)
            {
                int hitLevel = 0;
                foreach (var obj in recursionStack)
                {
                    if (obj == instance)
                    {
                        ThrowHelper.ThrowProtoException($"Possible recursion detected (offset: {(recursionStack.Count - hitLevel)} level(s)): {instance}");
                    }
                    hitLevel++;
                }
            }
            recursionStack.Add(instance);
        }
        private void PopRecursionStack() { recursionStack.RemoveAt(recursionStack.Count - 1); }

        /// <summary>
        /// Indicates the end of a nested record.
        /// </summary>
        /// <param name="token">The token obtained from StartubItem.</param>
        /// <param name="writer">The destination.</param>
        [Obsolete(UseStateAPI, false)]
        public static void EndSubItem(SubItemToken token, ProtoWriter writer)
        {
            writer.DefaultState().EndSubItem(token, PrefixStyle.Base128);
        }

        private void PostSubItem(ref State state)
        {
            if (WireType != WireType.None) state.ThrowInvalidSerializationOperation();
            if (depth <= 0) state.ThrowInvalidSerializationOperation();
            if (depth-- > RecursionCheckDepth)
            {
                PopRecursionStack();
            }
            packedFieldNumber = 0; // ending the sub-item always wipes packed encoding
        }

        protected private ProtoWriter() { }

        /// <summary>
        /// Creates a new writer against a stream
        /// </summary>
        /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to serialize sub-objects</param>
        /// <param name="context">Additional context about this serialization operation</param>
        internal virtual void Init(TypeModel model, SerializationContext context)
        {
            OnInit();
            _position64 = 0;
            _needFlush = false;
            this.packedFieldNumber = 0;
            depth = 0;
            fieldNumber = 0;
            this.model = model;
            WireType = WireType.None;
            if (context == null) { context = SerializationContext.Default; }
            else { context.Freeze(); }
            Context = context;
        }

        /// <summary>
        /// Addition information about this serialization operation.
        /// </summary>
        public SerializationContext Context { get; private set; }

#if DEBUG
        int _usageCount;
        partial void OnDispose()
        {
            int count = System.Threading.Interlocked.Decrement(ref _usageCount);
            if (count != 0) ThrowHelper.ThrowInvalidOperationException($"Usage count - expected 0, was {count}");
        }
        partial void OnInit()
        {
            int count = System.Threading.Interlocked.Increment(ref _usageCount);
            if (count != 1) ThrowHelper.ThrowInvalidOperationException($"Usage count - expected 1, was {count}");
        }
#endif
        partial void OnDispose();
        partial void OnInit();

        protected private virtual void Dispose()
        {
            OnDispose();
            Cleanup();
        }

        protected private virtual void Cleanup()
        {
            if (depth == 0 && _needFlush && ImplDemandFlushOnDispose)
            {
                ThrowHelper.ThrowInvalidOperationException("Writer was diposed without being flushed; data may be lost - you should ensure that Flush (or Abandon) is called");
            }
            recursionStack?.Clear();
            NetCache.Clear();
            model = null;
            Context = null;
        }


        /// <summary>
        /// Writes a sub-item to the input writer
        /// </summary>
        protected internal virtual void WriteSubItem<T>(ref State state, T value, IProtoSerializer<T> serializer, PrefixStyle style, bool recursionCheck)
        {
#pragma warning disable CS0618 // StartSubItem/EndSubItem
            var tok = state.StartSubItem(TypeHelper<T>.IsObjectType & recursionCheck ? (object)value : null, style);
            (serializer ?? TypeModel.GetSerializer<T>(model)).Write(ref state, value);
            state.EndSubItem(tok, style);
#pragma warning restore CS0618
        }

        /// <summary>
        /// Writes a sub-item to the input writer
        /// </summary>
        protected internal virtual void WriteSubType<T>(ref State state, T value, IProtoSubTypeSerializer<T> serializer) where T : class
        {
#pragma warning disable CS0618 // StartSubItem/EndSubItem
            var tok = state.StartSubItem(null, PrefixStyle.Base128);
            serializer.WriteSubType(ref state, value);
            state.EndSubItem(tok, PrefixStyle.Base128);
#pragma warning restore CS0618
        }

        /// <summary>
        /// Abandon any pending unflushed data
        /// </summary>
        public void Abandon() { _needFlush = false; }

        private bool _needFlush;

#pragma warning disable RCS1163, IDE0060 // Remove unused parameter
        internal long GetPosition(ref State state) => _position64;
#pragma warning restore RCS1163, IDE0060 // Remove unused parameter

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
            DefaultState().Close();
        }

        internal int Depth => depth;

        internal void CheckClear(ref State state)
        {
            if (depth != 0 || !TryFlush(ref state)) ThrowHelper.ThrowInvalidOperationException($"The writer is in an incomplete state (depth: {depth}, type: {GetType().Name})");
            _needFlush = false; // because we ^^^ *JUST DID*
        }

        /// <summary>
        /// Get the TypeModel associated with this writer
        /// </summary>
        public TypeModel Model
        {
            get => model;
            internal set => model = value;
        }

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
        public static void WriteString(string value, ProtoWriter writer) => writer.DefaultState().WriteString(value);

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
            writer.DefaultState().WriteUInt64(value);
        }

        /// <summary>
        /// Writes a signed 64-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteInt64(long value, ProtoWriter writer)
        {
            writer.DefaultState().WriteInt64(value);
        }

        /// <summary>
        /// Writes an unsigned 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteUInt32(uint value, ProtoWriter writer)
        {
            writer.DefaultState().WriteUInt32(value);
        }

        /// <summary>
        /// Writes a signed 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteInt16(short value, ProtoWriter writer)
        {
            writer.DefaultState().WriteInt16(value);
        }

        /// <summary>
        /// Writes an unsigned 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteUInt16(ushort value, ProtoWriter writer)
        {
            writer.DefaultState().WriteUInt16(value);
        }

        /// <summary>
        /// Writes an unsigned 8-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteByte(byte value, ProtoWriter writer)
        {
            writer.DefaultState().WriteByte(value);
        }

        /// <summary>
        /// Writes a signed 8-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteSByte(sbyte value, ProtoWriter writer)
        {
            writer.DefaultState().WriteSByte(value);
        }

        /// <summary>
        /// Writes a signed 32-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteInt32(int value, ProtoWriter writer)
        {
            writer.DefaultState().WriteInt32(value);
        }

        /// <summary>
        /// Writes a double-precision number to the stream; supported wire-types: Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteDouble(double value, ProtoWriter writer)
        {
            writer.DefaultState().WriteDouble(value);
        }

        /// <summary>
        /// Writes a single-precision number to the stream; supported wire-types: Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteSingle(float value, ProtoWriter writer)
        {
            writer.DefaultState().WriteSingle(value);
        }

        /// <summary>
        /// Throws an exception indicating that the given enum cannot be mapped to a serialized value.
        /// </summary>
        public static void ThrowEnumException(ProtoWriter writer, object enumValue)
        {
            if (writer == null) ThrowHelper.ThrowArgumentNullException(nameof(writer));
#pragma warning disable RCS1097 // Remove redundant 'ToString' call.
            string rhs = enumValue == null ? "<null>" : (enumValue.GetType().FullName + "." + enumValue.ToString());
#pragma warning restore RCS1097 // Remove redundant 'ToString' call.
            ThrowHelper.ThrowProtoException("No wire-value is mapped to the enum " + rhs + " at position " + writer._position64.ToString());
        }

        internal static void ThrowException(ProtoWriter writer)
        {
            var state = writer == null ? default : writer.DefaultState();
            throw state.ThrowInvalidSerializationOperation();
        }

        /// <summary>
        /// Writes a boolean to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [Obsolete(UseStateAPI, false)]
        public static void WriteBoolean(bool value, ProtoWriter writer)
        {
            writer.DefaultState().WriteBoolean(value);
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
        /// Used for packed encoding; indicates that the next field should be skipped rather than
        /// a field header written. Note that the field number must match, else an exception is thrown
        /// when the attempt is made to write the (incorrect) field. The wire-type is taken from the
        /// subsequent call to WriteFieldHeader. Only primitive types can be packed.
        /// </summary>
        public static void SetPackedField(int fieldNumber, ProtoWriter writer)
        {
            if (fieldNumber <= 0) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(fieldNumber));
            if (writer == null) ThrowHelper.ThrowArgumentNullException(nameof(writer));
            writer.packedFieldNumber = fieldNumber;
        }

        /// <summary>
        /// Used for packed encoding; explicitly reset the packed field marker; this is not required
        /// if using StartSubItem/EndSubItem
        /// </summary>
        public static void ClearPackedField(int fieldNumber, ProtoWriter writer)
        {
            if (fieldNumber != writer.packedFieldNumber)
                ThrowHelper.ThrowInvalidOperationException("Field mismatch during packed encoding; expected " + writer.packedFieldNumber.ToString() + " but received " + fieldNumber.ToString());
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
            => writer.DefaultState().WriteType(value);
    }
}
