using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ProtoBuf.Internal;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

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
        private const MethodImplOptions HotPath = ProtoReader.HotPath;

        internal const string PreferWriteMessage = "If possible, please use the WriteMessage API; this API may not work correctly with all writers";

        private TypeModel model;
        private int packedFieldNumber;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize - no intention of supporting finalizers here
        void IDisposable.Dispose() => Dispose();
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize

#if FEAT_DYNAMIC_REF
        /// <summary>
        /// Write an encapsulated sub-object, using the supplied unique key (reprasenting a type).
        /// </summary>
        /// <param name="value">The object to write.</param>
        /// <param name="type">The key that uniquely identifies the type within the model.</param>
        /// <param name="writer">The destination.</param>
        [MethodImpl(HotPath)]
        public static void WriteObject(object value, Type type, ProtoWriter writer)
            => writer.DefaultState().WriteObject(value, type);
#endif

        private protected readonly NetObjectCache netCache;

        private int fieldNumber;

        internal WireType WireType { get; set; }

        /// <summary>
        /// Writes a field-header, indicating the format of the next data we plan to write.
        /// </summary>
        [MethodImpl(HotPath)]
        public static void WriteFieldHeader(int fieldNumber, WireType wireType, ProtoWriter writer)
            => writer.DefaultState().WriteFieldHeader(fieldNumber, wireType);

        /// <summary>
        /// Writes a byte-array to the stream; supported wire-types: String
        /// </summary>
        [MethodImpl(HotPath)]
        public static void WriteBytes(byte[] data, ProtoWriter writer)
            => writer.DefaultState().WriteBytes(data);

        /// <summary>
        /// Writes a byte-array to the stream; supported wire-types: String
        /// </summary>
        [MethodImpl(HotPath)]
        public static void WriteBytes(byte[] data, int offset, int length, ProtoWriter writer)
            => writer.DefaultState().WriteBytes(new ReadOnlyMemory<byte>(data, offset, length));

        private int _depth = 0;
        private const int RecursionCheckDepth = 25;

        /// <summary>
        /// Indicates the start of a nested record.
        /// </summary>
        /// <param name="instance">The instance to write.</param>
        /// <param name="writer">The destination.</param>
        /// <returns>A token representing the state of the stream; this token is given to EndSubItem.</returns>
        [MethodImpl(HotPath)]
        [Obsolete(PreferWriteMessage, false)]
        public static SubItemToken StartSubItem(object instance, ProtoWriter writer)
            => writer.DefaultState().StartSubItem(instance, PrefixStyle.Base128);

        private void PreSubItem(ref State state, object instance)
        {
            if (_depth < 0) state.ThrowInvalidSerializationOperation();
            if (++_depth >= (model is null ? TypeModel.DefaultMaxDepth : model.MaxDepth))
            {
                state.ThrowTooDeep(_depth);
            }
            if (_depth > RecursionCheckDepth)
            {
                CheckRecursionStackAndPush(instance);
            }
            if (packedFieldNumber != 0) ThrowHelper.ThrowInvalidOperationException("Cannot begin a sub-item while performing packed encoding");
        }

        private List<object> recursionStack;
        private void CheckRecursionStackAndPush(object instance)
        {
            if (recursionStack is null) { recursionStack = new List<object>(); }
            else if (instance is not null)
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
        /// <param name="token">The token obtained from StartSubItem.</param>
        /// <param name="writer">The destination.</param>
        [MethodImpl(HotPath)]
        [Obsolete(PreferWriteMessage, false)]
        public static void EndSubItem(SubItemToken token, ProtoWriter writer)
            => writer.DefaultState().EndSubItem(token, PrefixStyle.Base128);

        private void PostSubItem(ref State state)
        {
            if (WireType != WireType.None) state.ThrowInvalidSerializationOperation();
            if (_depth <= 0) state.ThrowInvalidSerializationOperation();
            if (_depth-- > RecursionCheckDepth)
            {
                PopRecursionStack();
            }
            packedFieldNumber = 0; // ending the sub-item always wipes packed encoding
        }

        protected private ProtoWriter()
            => netCache = new NetObjectCache();

        protected private ProtoWriter(NetObjectCache knownObjects)
            => netCache = knownObjects;

        /// <summary>
        /// Creates a new writer against a stream
        /// </summary>
        /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to serialize sub-objects</param>
        /// <param name="userState">Additional context about this serialization operation</param>
        /// <param name="impactCount">Whether this initialization should impact usage counters (to check for double-usage)</param>
        internal virtual void Init(TypeModel model, object userState, bool impactCount)
        {
            OnInit(impactCount);
            _position64 = 0;
            _needFlush = false;
            this.packedFieldNumber = 0;
            _depth = 0;
            fieldNumber = 0;
            this.model = model;
            WireType = WireType.None;
            if (userState is SerializationContext context) context.Freeze();
            UserState = userState;
        }

        [StructLayout(LayoutKind.Auto)]
        internal readonly struct WriteState
        {
            internal WriteState(long position, int fieldNumber, WireType wireType)
            {
                Position = position;
                FieldNumber = fieldNumber;
                WireType = wireType;
            }
            internal readonly long Position;
            internal readonly WireType WireType;
            internal readonly int FieldNumber;
        }
        internal WriteState ResetWriteState()
        {
            var state = new WriteState(_position64, fieldNumber, WireType);
            _position64 = 0;
            fieldNumber = 0;
            WireType = WireType.None;
            return state;
        }
        internal void SetWriteState(WriteState state)
        {
            _position64 = state.Position;
            fieldNumber = state.FieldNumber;
            WireType = state.WireType;
        }

        /// <summary>
        /// Addition information about this serialization operation.
        /// </summary>
        [Obsolete("Prefer " + nameof(UserState))]
        public SerializationContext Context => SerializationContext.AsSerializationContext(this);

        /// <summary>
        /// Addition information about this serialization operation.
        /// </summary>
        public object UserState { get; private set; }

#if DEBUG || TRACK_USAGE
        int _usageCount;
        partial void OnDispose()
        {
            int count = System.Threading.Interlocked.Decrement(ref _usageCount);
            if (count != 0) ThrowHelper.ThrowInvalidOperationException($"Usage count - expected 0, was {count}");
        }
        partial void OnInit(bool impactCount)
        {
            if (impactCount)
            {
                int count = System.Threading.Interlocked.Increment(ref _usageCount);
                if (count != 1) ThrowHelper.ThrowInvalidOperationException($"Usage count - expected 1, was {count}");
            }
            else
            {
                _usageCount = 1;
            }
        }
#endif

        partial void OnDispose();
        partial void OnInit(bool impactCount);

        internal virtual void Dispose()
        {
            OnDispose();
            Cleanup();
        }

        protected private virtual void Cleanup()
        {
            if (_depth == 0 && _needFlush && ImplDemandFlushOnDispose)
            {
                ThrowHelper.ThrowInvalidOperationException("Writer was disposed without being flushed; data may be lost - you should ensure that Flush (or Abandon) is called");
            }
            recursionStack?.Clear();
            ClearKnownObjects();
            model = null;
            UserState = null;
        }

        protected private virtual void ClearKnownObjects()
            => netCache?.Clear();


        /// <summary>
        /// Writes a sub-item to the input writer
        /// </summary>
        protected internal virtual void WriteMessage<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(ref State state, T value, ISerializer<T> serializer, PrefixStyle style, bool recursionCheck)
        {
#pragma warning disable CS0618 // StartSubItem/EndSubItem
            var tok = state.StartSubItem(TypeHelper<T>.IsReferenceType & recursionCheck ? (object)value : null, style);
            (serializer ?? TypeModel.GetSerializer<T>(model)).Write(ref state, value);
            state.EndSubItem(tok, style);
#pragma warning restore CS0618
        }

        internal virtual void WriteWrappedCollection<TCollection, TItem>(ref State state, SerializerFeatures features, TCollection values, RepeatedSerializer<TCollection, TItem> serializer, ISerializer<TItem> valueSerializer)
        {
#pragma warning disable CS0618 // StartSubItem/EndSubItem
            var tok = state.StartSubItem(null);
            serializer.WriteRepeated(ref state, TypeModel.ListItemTag, features, values, valueSerializer);
            state.EndSubItem(tok);
#pragma warning restore CS0618
        }

        internal virtual void WriteWrappedMap<TCollection, TKey, TValue>(ref State state, SerializerFeatures features, TCollection values, MapSerializer<TCollection, TKey, TValue> serializer, SerializerFeatures keyFeatures, SerializerFeatures valueFeatures, ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer)
        {
#pragma warning disable CS0618 // StartSubItem/EndSubItem
            var tok = state.StartSubItem(null);
            serializer.WriteMap(ref state, TypeModel.ListItemTag, features, values, keyFeatures, valueFeatures, keySerializer, valueSerializer);
            state.EndSubItem(tok);
#pragma warning restore CS0618
        }

        internal void WriteEmptyWrappedItem(ref State state)
        {
            // semantically, this is the same as the two lines below; just:
            // without the indirection and without needing to have
            // writer-specific implementations (since we can go forwards-only)
            //
            // var tok = state.StartSubItem(null, PrefixStyle.Base128);
            // state.EndSubItem(tok);

            switch (WireType)
            {
                case WireType.String:
                    AdvanceAndReset(ImplWriteVarint32(ref state, 0));
                    break;
                case WireType.StartGroup:
                    state.WriteHeaderCore(state.FieldNumber, WireType.EndGroup);
                    WireType = WireType.None; // reset
                    break;
                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(WireType));
                    break;
            }
        }

        internal virtual void WriteWrappedItem<T>(ref State state, SerializerFeatures features, T value, ISerializer<T> serializer)
        {
#pragma warning disable CS0618 // we don't want to use WriteMessage here; this is a pseudo message layer
            var tok = state.StartSubItem(TypeHelper<T>.IsReferenceType & features.ApplyRecursionCheck() ? (object)value : null, PrefixStyle.Base128);
            state.WriteAny<T>(TypeModel.ListItemTag, features, value, serializer);
            state.EndSubItem(tok);
#pragma warning restore CS0618
        }

        /// <summary>
        /// Writes a sub-item to the input writer
        /// </summary>
        protected internal virtual void WriteSubType<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(ref State state, T value, ISubTypeSerializer<T> serializer) where T : class
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
        internal void AdvanceAndReset(int count)
        {
            _position64 += count;
            WireType = WireType.None;
        }
        internal void AdvanceAndReset(long count)
        {
            _position64 += count;
            WireType = WireType.None;
        }

        /// <summary>
        /// Flushes data to the underlying stream, and releases any resources. The underlying stream is *not* disposed
        /// by this operation.
        /// </summary>
        [MethodImpl(HotPath)]
        public void Close() => DefaultState().Close();

        internal int Depth => _depth;

        [MethodImpl(HotPath)]
        internal void CheckClear(ref State state)
        {
            if (_depth != 0 || !TryFlush(ref state)) ThrowHelper.ThrowInvalidOperationException($"The writer is in an incomplete state (depth: {_depth}, type: {GetType().Name}, field: {fieldNumber}, wire-type: {WireType}, position: {state.GetPosition()})");
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

        /// <summary>
        /// The encoding used by the writer
        /// </summary>
        internal protected static readonly UTF8Encoding UTF8 = new UTF8Encoding();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Zig(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong Zig(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }

        /// <summary>
        /// Writes a string to the stream; supported wire-types: String
        /// </summary>
        [MethodImpl(HotPath)]
        public static void WriteString(string value, ProtoWriter writer) => writer.DefaultState().WriteString(value);

        protected private abstract void ImplWriteString(ref State state, string value, int expectedBytes);
        protected private abstract int ImplWriteVarint32(ref State state, uint value);
        internal abstract int ImplWriteVarint64(ref State state, ulong value);
        protected private abstract void ImplWriteFixed32(ref State state, uint value);
        protected private abstract void ImplWriteFixed64(ref State state, ulong value);
        protected private abstract void ImplWriteBytes(ref State state, ReadOnlySpan<byte> data);
        protected private abstract void ImplWriteBytes(ref State state, ReadOnlySequence<byte> data);
        protected private abstract void ImplCopyRawFromStream(ref State state, Stream source);
        private protected abstract SubItemToken ImplStartLengthPrefixedSubItem(ref State state, object instance, PrefixStyle style);
        protected private abstract void ImplEndLengthPrefixedSubItem(ref State state, SubItemToken token, PrefixStyle style);
        protected private abstract bool ImplDemandFlushOnDispose { get; }

        /// <summary>
        /// Writes any buffered data (if possible) to the underlying stream.
        /// </summary>
        /// <param name="state">writer state</param>
        /// <remarks>It is not always possible to fully flush, since some sequences
        /// may require values to be back-filled into the byte-stream.</remarks>
        private protected abstract bool TryFlush(ref State state);

        /// <summary>
        /// Writes an unsigned 64-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [MethodImpl(HotPath)]
        public static void WriteUInt64(ulong value, ProtoWriter writer)
            => writer.DefaultState().WriteUInt64(value);

        /// <summary>
        /// Writes a signed 64-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        [MethodImpl(HotPath)]
        public static void WriteInt64(long value, ProtoWriter writer)
            => writer.DefaultState().WriteInt64(value);

        /// <summary>
        /// Writes an unsigned 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [MethodImpl(HotPath)]
        public static void WriteUInt32(uint value, ProtoWriter writer)
            => writer.DefaultState().WriteUInt32(value);

        /// <summary>
        /// Writes a signed 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        [MethodImpl(HotPath)]
        public static void WriteInt16(short value, ProtoWriter writer)
            => writer.DefaultState().WriteInt16(value);

        /// <summary>
        /// Writes an unsigned 16-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [MethodImpl(HotPath)]
        public static void WriteUInt16(ushort value, ProtoWriter writer)
            => writer.DefaultState().WriteUInt16(value);

        /// <summary>
        /// Writes an unsigned 8-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [MethodImpl(HotPath)]
        public static void WriteByte(byte value, ProtoWriter writer)
            => writer.DefaultState().WriteByte(value);

        /// <summary>
        /// Writes a signed 8-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        [MethodImpl(HotPath)]
        public static void WriteSByte(sbyte value, ProtoWriter writer)
            => writer.DefaultState().WriteSByte(value);

        /// <summary>
        /// Writes a signed 32-bit integer to the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        public static void WriteInt32(int value, ProtoWriter writer)
            => writer.DefaultState().WriteInt32(value);

        /// <summary>
        /// Writes a double-precision number to the stream; supported wire-types: Fixed32, Fixed64
        /// </summary>
        [MethodImpl(HotPath)]
        public static void WriteDouble(double value, ProtoWriter writer)
            => writer.DefaultState().WriteDouble(value);

        /// <summary>
        /// Writes a single-precision number to the stream; supported wire-types: Fixed32, Fixed64
        /// </summary>
        [MethodImpl(HotPath)]
        public static void WriteSingle(float value, ProtoWriter writer)
            => writer.DefaultState().WriteSingle(value);

        /// <summary>
        /// Throws an exception indicating that the given enum cannot be mapped to a serialized value.
        /// </summary>
        [MethodImpl(HotPath)]
        public static void ThrowEnumException(ProtoWriter writer, object enumValue)
            => writer.DefaultState().ThrowEnumException(enumValue);

        /// <summary>
        /// Writes a boolean to the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        [MethodImpl(HotPath)]
        public static void WriteBoolean(bool value, ProtoWriter writer)
            => writer.DefaultState().WriteBoolean(value);

        /// <summary>
        /// Copies any extension data stored for the instance to the underlying stream
        /// </summary>
        [MethodImpl(HotPath)]
        public static void AppendExtensionData(IExtensible instance, ProtoWriter writer)
            => writer.DefaultState().AppendExtensionData(instance);


        /// <summary>
        /// Used for packed encoding; indicates that the next field should be skipped rather than
        /// a field header written. Note that the field number must match, else an exception is thrown
        /// when the attempt is made to write the (incorrect) field. The wire-type is taken from the
        /// subsequent call to WriteFieldHeader. Only primitive types can be packed.
        /// </summary>
        [MethodImpl(HotPath)]
        public static void SetPackedField(int fieldNumber, ProtoWriter writer)
            => writer.DefaultState().SetPackedField(fieldNumber);

        /// <summary>
        /// Used for packed encoding; explicitly reset the packed field marker; this is not required
        /// if using StartSubItem/EndSubItem
        /// </summary>
        [MethodImpl(HotPath)]
        public static void ClearPackedField(int fieldNumber, ProtoWriter writer)
            => writer.DefaultState().ClearPackedField(fieldNumber);

        /// <summary>
        /// Used for packed encoding; writes the length prefix using fixed sizes rather than using
        /// buffering. Only valid for fixed-32 and fixed-64 encoding.
        /// </summary>
        [MethodImpl(HotPath)]
        public static void WritePackedPrefix(int elementCount, WireType wireType, ProtoWriter writer)
            => writer.DefaultState().WritePackedPrefix(elementCount, wireType);

        internal string SerializeType(Type type)
        {
            return TypeModel.SerializeType(model, type);
        }

        /// <summary>
        /// Buffer size to use when writing; if non-positive, an internal default is used.
        /// </summary>
        /// <remarks>Not all writer implementations make use of this API</remarks>
        [Obsolete("Please migrate to " + nameof(TypeModel) + "." + nameof(TypeModel.BufferSize))]
        public static int BufferSize
        {
            get => TypeModel.DefaultModel.BufferSize;
            set => TypeModel.DefaultModel.BufferSize = value;
        }

#if FEAT_DYNAMIC_REF
        /// <summary>
        /// Specifies a known root object to use during reference-tracked serialization
        /// </summary>
        [MethodImpl(HotPath)]
        public void SetRootObject(object value) => netCache.SetKeyedObject(NetObjectCache.Root, value);

        /// <summary>
        /// Specifies a known root object to use during reference-tracked serialization
        /// </summary>
        [MethodImpl(HotPath)]
        internal int AddObjectKey(object value, out bool existing)
        {
            AssertTrackedObjects();
            return netCache.AddObjectKey(value, out existing);
        }

        [MethodImpl(HotPath)]
        internal void AssertTrackedObjects()
        {
            if (!(this is StreamProtoWriter)) ThrowHelper.ThrowTrackedObjects(this);
        }
#endif

        /// <summary>
        /// Writes a Type to the stream, using the model's DynamicTypeFormatting if appropriate; supported wire-types: String
        /// </summary>
        [MethodImpl(HotPath)]
        public static void WriteType(Type value, ProtoWriter writer)
            => writer.DefaultState().WriteType(value);

        internal static long MeasureRepeated<TCollection, TItem>(NullProtoWriter writer, int fieldNumber, SerializerFeatures features, TCollection values, RepeatedSerializer<TCollection, TItem> serializer, ISerializer<TItem> valueSerializer)
        {
            long length;
            object obj = default;
            if (TypeHelper<TCollection>.IsReferenceType)
            {
                obj = values;
                if (obj is null) return 0;
                if (writer.netCache.TryGetKnownLength(obj, null, out length))
                    return length;
            }

            // do the actual work
            var oldState = writer.ResetWriteState();
            var nulState = new State(writer);
            serializer.WriteRepeated(ref nulState, fieldNumber, features, values, valueSerializer);
            length = nulState.GetPosition();
            writer.SetWriteState(oldState); // make sure we leave it how we found it

            // cache it if we can
            if (obj is not null)
            {   // we know it isn't null; we'd have exited above
                writer.netCache.SetKnownLength(obj, null, length);
            }
            return length;
        }

        internal static long MeasureMap<TCollection, TKey, TValue>(NullProtoWriter writer, int fieldNumber, SerializerFeatures features, TCollection values, MapSerializer<TCollection, TKey,TValue> serializer, SerializerFeatures keyFeatures, SerializerFeatures valueFeatures, ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer)
        {
            long length;
            object obj = default;
            if (TypeHelper<TCollection>.IsReferenceType)
            {
                obj = values;
                if (obj is null) return 0;
                if (writer.netCache.TryGetKnownLength(obj, null, out length))
                    return length;
            }

            // do the actual work
            var oldState = writer.ResetWriteState();
            var nulState = new State(writer);
            serializer.WriteMap(ref nulState, fieldNumber, features, values, keyFeatures, valueFeatures, keySerializer, valueSerializer);
            length = nulState.GetPosition();
            writer.SetWriteState(oldState); // make sure we leave it how we found it

            // cache it if we can
            if (obj is not null)
            {   // we know it isn't null; we'd have exited above
                writer.netCache.SetKnownLength(obj, null, length);
            }
            return length;
        }

        internal static long MeasureAny<T>(NullProtoWriter writer, int fieldNumber, SerializerFeatures features, T value, ISerializer<T> serializer)
        {
            // note: not using cache here is intentional, since we're calling from the wrapped-item path; we don't
            // want to get confused between the wrapped and non-wrapped variants

            var oldState = writer.ResetWriteState();
            var nulState = new State(writer);
            nulState.WriteAny<T>(fieldNumber, features, value, serializer);
            var length = nulState.GetPosition();
            writer.SetWriteState(oldState); // make sure we leave it how we found it

            return length;
        }

        internal static long Measure<T>(NullProtoWriter writer, T value, ISerializer<T> serializer)
        {
            long length;
            object obj = default;
            if (TypeHelper<T>.IsReferenceType)
            {
                obj = value;
                if (obj is null) return 0;
                if (writer.netCache.TryGetKnownLength(obj, null, out length))
                    return length;
            }

            // do the actual work
            var oldState = writer.ResetWriteState();
            var nulState = new State(writer);
            serializer.Write(ref nulState, value);
            length = nulState.GetPosition();
            writer.SetWriteState(oldState); // make sure we leave it how we found it

            // cache it if we can
            if (obj is not null)
            {   // we know it isn't null; we'd have exited above
                writer.netCache.SetKnownLength(obj, null, length);
            }
            return length;
        }

        internal static long Measure<T>(NullProtoWriter writer, T value, ISubTypeSerializer<T> serializer) where T : class
        {
            object obj = value;
            if (obj is null) return 0;
            if (writer.netCache.TryGetKnownLength(obj, typeof(T), out var length))
            {
                return length;
            }

            var oldState = writer.ResetWriteState();
            var nulState = new State(writer);
            serializer.WriteSubType(ref nulState, value);
            length = nulState.GetPosition();
            writer.SetWriteState(oldState); // make sure we leave it how we found it
            writer.netCache.SetKnownLength(obj, typeof(T), length);
            return length;
        }
    }
}
