
﻿using ProtoBuf.Internal;
using ProtoBuf.Serializers;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace ProtoBuf.Meta
{
    internal static class TypeModelExtensions
    {
        [MethodImpl(ProtoReader.HotPath)]
        internal static bool HasOption(this TypeModel model, TypeModel.TypeModelOptions options)
        {
            var modelOptions = model is null ? TypeModel.DefaultOptions : model.Options;
            return (modelOptions & options) != 0;
        }


        [MethodImpl(ProtoReader.HotPath)]
        internal static bool OmitsOption(this TypeModel model, TypeModel.TypeModelOptions options)
        {
            var modelOptions = model is null ? TypeModel.DefaultOptions : model.Options;
            return (modelOptions & options) == 0;
        }
    }

    /// <summary>
    /// Provides protobuf serialization support for a number of types
    /// </summary>
    public abstract partial class TypeModel
    {
        // config options

        /// <summary>
        /// Gets or sets the buffer-size to use when writing messages via <see cref="IBufferWriter{T}"/>
        /// </summary>
        public int BufferSize
        {
            get => _bufferSize;
            set => _bufferSize = value <= 0 ? BufferPool.BUFFER_LENGTH : value; // use default if invalid
        }
        /// <summary>
        /// Gets or sets the max serialization/deserialization depth
        /// </summary>
        public int MaxDepth
        {
            get => _maxDepth;
            set => _maxDepth = value <= 0 ? DefaultMaxDepth : value; // use default if invalid
        }

        private int _bufferSize = BufferPool.BUFFER_LENGTH, _maxDepth = DefaultMaxDepth;
        internal const int DefaultMaxDepth = 512;

        /// <summary>
        /// Gets a cached serializer for a type, as offered by a given provider
        /// </summary>
        [MethodImpl(ProtoReader.HotPath)]
        protected static ISerializer<T> GetSerializer<[DynamicallyAccessedMembers(DynamicAccess.Serializer)] TProvider, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            where TProvider : class
            => SerializerCache<TProvider, T>.InstanceField;

        /// <summary>
        /// Specifies optional behaviors associated with a type model
        /// </summary>
        [Flags]
        public enum TypeModelOptions
        {
            /// <summary>
            /// No additional options
            /// </summary>
            None = 0,
            /// <summary>
            /// Should the deserializer attempt to avoid duplicate copies of the same string?
            /// </summary>
            InternStrings = 1 << 0,
            /// <summary>
            /// Should the <c>Kind</c> be included on date/time values?
            /// </summary>
            IncludeDateTimeKind = 1 << 1,
            /// <summary>
            /// Should zero-length packed arrays be serialized? (this is the v2 behavior, but skipping them is more efficient)
            /// </summary>
            SkipZeroLengthPackedArrays = 1 << 2,
            /// <summary>
            /// Should root-values allow "packed" encoding? (v2 does not support this)
            /// </summary>
            AllowPackedEncodingAtRoot = 1 << 3,
        }

        /// <summary>
        /// Specifies optional behaviors associated with this model
        /// </summary>
        public virtual TypeModelOptions Options => DefaultOptions;

        internal const TypeModelOptions DefaultOptions = 0; // important: WriteConstructorsAndOverrides only overrides if different

        /// <summary>
        /// Resolve a System.Type to the compiler-specific type
        /// </summary>
        [Obsolete("This API is no longer required and may be removed in a future release")]
        protected internal Type MapType(Type type) => type;

        /// <summary>
        /// Resolve a System.Type to the compiler-specific type
        /// </summary>
        [Obsolete("This API is no longer required and may be removed in a future release")]
        protected internal Type MapType(Type type, bool demand) => type;

        [SuppressMessage("Style", "IDE0066:Convert switch statement to expression", Justification = "Readability")]
        internal static WireType GetWireType(TypeModel model, DataFormat format, Type type)
        {
            if (type.IsEnum) return WireType.Varint;

            if (model is not null && model.CanSerializeContractType(type))
            {
                return format == DataFormat.Group ? WireType.StartGroup : WireType.String;
            }

            switch (Helpers.GetTypeCode(type))
            {
                case ProtoTypeCode.Int64:
                case ProtoTypeCode.UInt64:
                    return format == DataFormat.FixedSize ? WireType.Fixed64 : WireType.Varint;
                case ProtoTypeCode.Int16:
                case ProtoTypeCode.Int32:
                case ProtoTypeCode.UInt16:
                case ProtoTypeCode.UInt32:
                case ProtoTypeCode.Boolean:
                case ProtoTypeCode.SByte:
                case ProtoTypeCode.Byte:
                case ProtoTypeCode.Char:
                    return format == DataFormat.FixedSize ? WireType.Fixed32 : WireType.Varint;
                case ProtoTypeCode.Double:
                    return WireType.Fixed64;
                case ProtoTypeCode.Single:
                    return WireType.Fixed32;
                case ProtoTypeCode.String:
                case ProtoTypeCode.DateTime:
                case ProtoTypeCode.Decimal:
                case ProtoTypeCode.ByteArray:
                case ProtoTypeCode.ByteArraySegment:
                case ProtoTypeCode.ByteMemory:
                case ProtoTypeCode.ByteReadOnlyMemory:
                case ProtoTypeCode.TimeSpan:
                case ProtoTypeCode.Guid:
                case ProtoTypeCode.Uri:
                    return WireType.String;
            }
            return WireType.None;
        }
        /// <summary>        /// Indicates whether a type is known to the model
        /// </summary>
        internal virtual bool IsKnownType<T>(CompatibilityLevel ambient)
            => (TypeHelper<T>.IsReferenceType | !TypeHelper<T>.CanBeNull) // don't claim T?
            && GetSerializerCore<T>(ambient) is object;

        internal const SerializerFeatures FromAux = (SerializerFeatures)(1 << 30);

        /// <summary>
        /// This is the more "complete" version of Serialize, which handles single instances of mapped types.
        /// The value is written as a complete field, including field-header and (for sub-objects) a
        /// length-prefix
        /// In addition to that, this provides support for:
        ///  - basic values; individual int / string / Guid / etc
        ///  - IEnumerable sequences of any type handled by TrySerializeAuxiliaryType
        ///  
        /// </summary>
        internal bool TrySerializeAuxiliaryType(ref ProtoWriter.State state, Type type, DataFormat format, int tag, object value, bool isInsideList, object parentList, bool isRoot)
        {
            PrepareDeserialize(value, ref type);

            WireType wireType = GetWireType(this, format, type);
            if (DynamicStub.CanSerialize(type, this, out var features))
            {
                var scope = NormalizeAuxScope(features, isInsideList, type, isRoot);
                try
                {
                    if (!DynamicStub.TrySerializeAny(tag, wireType.AsFeatures() | FromAux, type, this, ref state, value))
                        ThrowUnexpectedType(type, this);
                }
                catch (Exception ex)
                {
                    ThrowHelper.ThrowProtoException(ex.Message + $"; scope: {scope}, features: {features}; type: {type.NormalizeName()}", ex);
                }
                return true;
            }

            // now attempt to handle sequences (including arrays and lists)
            if (value is IEnumerable sequence)
            {
                if (isInsideList) ThrowNestedListsNotSupported(parentList?.GetType());
                foreach (object item in sequence)
                {
                    if (item is null) ThrowHelper.ThrowNullReferenceException();
                    if (!TrySerializeAuxiliaryType(ref state, null, format, tag, item, true, sequence, isRoot))
                    {
                        ThrowUnexpectedType(item.GetType(), this);
                    }
                }
                return true;
            }
            return false;
        }

        static ObjectScope NormalizeAuxScope(SerializerFeatures features, bool isInsideList, Type type, bool isRoot)
        {
            switch (features.GetCategory())
            {
                case SerializerFeatures.CategoryRepeated:
                    if (isInsideList) ThrowNestedListsNotSupported(type);
                    ThrowHelper.ThrowNotSupportedException("A repeated type was not expected as an aux type: " + type.NormalizeName());
                    return ObjectScope.NakedMessage;
                case SerializerFeatures.CategoryMessage:
                    return ObjectScope.WrappedMessage;
                case SerializerFeatures.CategoryMessageWrappedAtRoot:
                    return (isInsideList | isRoot) ? ObjectScope.WrappedMessage : ObjectScope.LikeRoot;
                case SerializerFeatures.CategoryScalar:
                    return ObjectScope.Scalar;
                default:
                    features.ThrowInvalidCategory();
                    return default;
            }
        }

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream.
        /// </summary>
        /// <param name="value">The existing instance to be serialized (cannot be null).</param>
        /// <param name="dest">The destination stream to write to.</param>
        public void Serialize(Stream dest, object value)
        {
            var state = ProtoWriter.State.Create(dest, this);
            try
            {
                SerializeRootFallback(ref state, value);
            }
            finally
            {
                state.Dispose();
            }
        }

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream.
        /// </summary>
        /// <param name="value">The existing instance to be serialized (cannot be null).</param>
        /// <param name="dest">The destination stream to write to.</param>
        /// <param name="context">Additional information about this serialization operation.</param>
        public void Serialize(Stream dest, object value, SerializationContext context)
        {
            var state = ProtoWriter.State.Create(dest, this, context);
            try
            {
                SerializeRootFallback(ref state, value);
            }
            finally
            {
                state.Dispose();
            }
        }

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied writer.
        /// </summary>
        /// <param name="value">The existing instance to be serialized (cannot be null).</param>
        /// <param name="dest">The destination stream to write to.</param>
        /// <param name="userState">Additional information about this serialization operation.</param>
        public void Serialize(IBufferWriter<byte> dest, object value, object userState = default)
        {
            var state = ProtoWriter.State.Create(dest, this, userState);
            try
            {
                SerializeRootFallback(ref state, value);
            }
            finally
            {
                state.Dispose();
            }
        }

        internal void SerializeRootFallback(ref ProtoWriter.State state, object value)
        {
            var type = value.GetType();
            try
            {
                if (!DynamicStub.TrySerializeRoot(type, this, ref state, value))
                {
#if FEAT_DYNAMIC_REF
                    state.SetRootObject(value);
#endif
                    if (!TrySerializeAuxiliaryType(ref state, type, DataFormat.Default, TypeModel.ListItemTag, value, false, null, isRoot: true))
                    {
                        ThrowUnexpectedType(type, this);
                    }
                    state.Close();
                }
            }
            catch
            {
                state.Abandon();
                throw;
            }
        }

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream.
        /// </summary>
        /// <param name="value">The existing instance to be serialized (cannot be null).</param>
        /// <param name="dest">The destination stream to write to.</param>
        /// <param name="userState">Additional information about this serialization operation.</param>
        public long Serialize<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(Stream dest, T value, object userState = null)
        {
            var state = ProtoWriter.State.Create(dest, this, userState);
            try
            {
                return SerializeImpl<T>(ref state, value);
            }
            finally
            {
                state.Dispose();
            }
        }

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied writer.
        /// </summary>
        /// <param name="value">The existing instance to be serialized (cannot be null).</param>
        /// <param name="dest">The destination stream to write to.</param>
        /// <param name="userState">Additional information about this serialization operation.</param>
        public long Serialize<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(IBufferWriter<byte> dest, T value, object userState = null)
        {
            var state = ProtoWriter.State.Create(dest, this, userState);
            try
            {
                return SerializeImpl<T>(ref state, value);
            }
            finally
            {
                state.Dispose();
            }
        }

        /// <summary>
        /// Calculates the length of a protocol-buffer payload for an item
        /// </summary>
        public MeasureState<T> Measure<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(T value, object userState = null, long abortAfter = -1)
            => new MeasureState<T>(this, value, userState, abortAfter);

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied writer.
        /// </summary>
        /// <param name="value">The existing instance to be serialized (cannot be null).</param>
        /// <param name="dest">The destination writer to write to.</param>
        [Obsolete(ProtoReader.PreferStateAPI, false)]
        public void Serialize(ProtoWriter dest, object value)
        {
            ProtoWriter.State state = dest.DefaultState();
            SerializeRootFallback(ref state, value);
        }

        internal static long SerializeImpl<T>(ref ProtoWriter.State state, T value)
        {
            if (TypeHelper<T>.CanBeNull && TypeHelper<T>.ValueChecker.IsNull(value)) return 0;

            var serializer = TryGetSerializer<T>(state.Model);
            if (serializer is null)
            {
                Debug.Assert(state.Model is not null, "Model is null");
                long position = state.GetPosition();
                state.Model.SerializeRootFallback(ref state, value);
                return state.GetPosition() - position;
            }
            else
            {
                return state.SerializeRoot<T>(value, serializer);
            }
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (or null), using length-prefixed
        /// data - useful with network IO.
        /// </summary>
        /// <param name="type">The type being merged.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <param name="fieldNumber">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public object DeserializeWithLengthPrefix(Stream source, object value, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, PrefixStyle style, int fieldNumber)
            => DeserializeWithLengthPrefix(source, value, type, style, fieldNumber, null, out long _);

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (or null), using length-prefixed
        /// data - useful with network IO.
        /// </summary>
        /// <param name="type">The type being merged.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <param name="expectedField">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
        /// <param name="resolver">Used to resolve types on a per-field basis.</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public object DeserializeWithLengthPrefix(Stream source, object value, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, PrefixStyle style, int expectedField, TypeResolver resolver)
            => DeserializeWithLengthPrefix(source, value, type, style, expectedField, resolver, out long _);

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (or null), using length-prefixed
        /// data - useful with network IO.
        /// </summary>
        /// <param name="type">The type being merged.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <param name="expectedField">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
        /// <param name="resolver">Used to resolve types on a per-field basis.</param>
        /// <param name="bytesRead">Returns the number of bytes consumed by this operation (includes length-prefix overheads and any skipped data).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public object DeserializeWithLengthPrefix(Stream source, object value, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, PrefixStyle style, int expectedField, TypeResolver resolver, out int bytesRead)
        {
            object result = DeserializeWithLengthPrefix(source, value, type, style, expectedField, resolver, out long bytesRead64, out bool _, null);
            bytesRead = checked((int)bytesRead64);
            return result;
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (or null), using length-prefixed
        /// data - useful with network IO.
        /// </summary>
        /// <param name="type">The type being merged.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <param name="expectedField">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
        /// <param name="resolver">Used to resolve types on a per-field basis.</param>
        /// <param name="bytesRead">Returns the number of bytes consumed by this operation (includes length-prefix overheads and any skipped data).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public object DeserializeWithLengthPrefix(Stream source, object value, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, PrefixStyle style, int expectedField, TypeResolver resolver, out long bytesRead) => DeserializeWithLengthPrefix(source, value, type, style, expectedField, resolver, out bytesRead, out bool _, null);

        private object DeserializeWithLengthPrefix(Stream source, object value, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, PrefixStyle style, int expectedField, TypeResolver resolver, out long bytesRead, out bool haveObject, SerializationContext context)
        {
            haveObject = false;
            bool skip;
            long len;
            bytesRead = 0;
            if (type is null && (style != PrefixStyle.Base128 || resolver is null))
            {
                ThrowHelper.ThrowInvalidOperationException("A type must be provided unless base-128 prefixing is being used in combination with a resolver");
            }
            do
            {
                bool expectPrefix = expectedField > 0 || resolver is not null;
                len = ProtoReader.ReadLongLengthPrefix(source, expectPrefix, style, out int actualField, out int tmpBytesRead);
                if (tmpBytesRead == 0) return value;
                bytesRead += tmpBytesRead;
                if (len < 0) return value;

                switch (style)
                {
                    case PrefixStyle.Base128:
                        if (expectPrefix && expectedField == 0 && type is null && resolver is not null)
                        {
                            type = resolver(actualField);
                            skip = type is null;
                        }
                        else { skip = expectedField != actualField; }
                        break;
                    default:
                        skip = false;
                        break;
                }

                if (skip)
                {
                    if (len == long.MaxValue) ThrowHelper.ThrowInvalidOperationException();
                    ProtoReader.Seek(source, len, null);
                    bytesRead += len;
                }
            } while (skip);

            var state = ProtoReader.State.Create(source, this, context, len);
            try
            {
                if (IsDefined(type) && !type.IsEnum)
                {
                    value = Deserialize(ObjectScope.LikeRoot, ref state, type, value);
                }
                else
                {
                    if (!(TryDeserializeAuxiliaryType(ref state, DataFormat.Default, TypeModel.ListItemTag, type, ref value, true, false, true, false, null, isRoot: true) || len == 0))
                    {
                        TypeModel.ThrowUnexpectedType(type, this); // throws
                    }
                }
                bytesRead += state.GetPosition();
            }
            finally
            {
                state.Dispose();
            }
            haveObject = true;
            return value;
        }

        /// <summary>
        /// Reads a sequence of consecutive length-prefixed items from a stream, using
        /// either base-128 or fixed-length prefixes. Base-128 prefixes with a tag
        /// are directly comparable to serializing multiple items in succession
        /// (use the <see cref="TypeModel.ListItemTag"/> tag to emulate the implicit behavior
        /// when serializing a list/array). When a tag is
        /// specified, any records with different tags are silently omitted. The
        /// tag is ignored. The tag is ignores for fixed-length prefixes.
        /// </summary>
        /// <param name="source">The binary stream containing the serialized records.</param>
        /// <param name="style">The prefix style used in the data.</param>
        /// <param name="expectedField">The tag of records to return (if non-positive, then no tag is
        /// expected and all records are returned).</param>
        /// <param name="resolver">On a field-by-field basis, the type of object to deserialize (can be null if "type" is specified). </param>
        /// <param name="type">The type of object to deserialize (can be null if "resolver" is specified).</param>
        /// <returns>The sequence of deserialized objects.</returns>
        public IEnumerable DeserializeItems(System.IO.Stream source, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, PrefixStyle style, int expectedField, TypeResolver resolver)
        {
            return DeserializeItems(source, type, style, expectedField, resolver, null);
        }
        /// <summary>
        /// Reads a sequence of consecutive length-prefixed items from a stream, using
        /// either base-128 or fixed-length prefixes. Base-128 prefixes with a tag
        /// are directly comparable to serializing multiple items in succession
        /// (use the <see cref="TypeModel.ListItemTag"/> tag to emulate the implicit behavior
        /// when serializing a list/array). When a tag is
        /// specified, any records with different tags are silently omitted. The
        /// tag is ignored. The tag is ignores for fixed-length prefixes.
        /// </summary>
        /// <param name="source">The binary stream containing the serialized records.</param>
        /// <param name="style">The prefix style used in the data.</param>
        /// <param name="expectedField">The tag of records to return (if non-positive, then no tag is
        /// expected and all records are returned).</param>
        /// <param name="resolver">On a field-by-field basis, the type of object to deserialize (can be null if "type" is specified). </param>
        /// <param name="type">The type of object to deserialize (can be null if "resolver" is specified).</param>
        /// <returns>The sequence of deserialized objects.</returns>
        /// <param name="context">Additional information about this serialization operation.</param>
        public IEnumerable DeserializeItems(System.IO.Stream source, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, PrefixStyle style, int expectedField, TypeResolver resolver, SerializationContext context)
        {
            return new DeserializeItemsIterator(this, source, type, style, expectedField, resolver, context);
        }

        /// <summary>
        /// Reads a sequence of consecutive length-prefixed items from a stream, using
        /// either base-128 or fixed-length prefixes. Base-128 prefixes with a tag
        /// are directly comparable to serializing multiple items in succession
        /// (use the <see cref="TypeModel.ListItemTag"/> tag to emulate the implicit behavior
        /// when serializing a list/array). When a tag is
        /// specified, any records with different tags are silently omitted. The
        /// tag is ignored. The tag is ignores for fixed-length prefixes.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize.</typeparam>
        /// <param name="source">The binary stream containing the serialized records.</param>
        /// <param name="style">The prefix style used in the data.</param>
        /// <param name="expectedField">The tag of records to return (if non-positive, then no tag is
        /// expected and all records are returned).</param>
        /// <returns>The sequence of deserialized objects.</returns>
        public IEnumerable<T> DeserializeItems<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(Stream source, PrefixStyle style, int expectedField)
        {
            return DeserializeItems<T>(source, style, expectedField, null);
        }
        /// <summary>
        /// Reads a sequence of consecutive length-prefixed items from a stream, using
        /// either base-128 or fixed-length prefixes. Base-128 prefixes with a tag
        /// are directly comparable to serializing multiple items in succession
        /// (use the <see cref="TypeModel.ListItemTag"/> tag to emulate the implicit behavior
        /// when serializing a list/array). When a tag is
        /// specified, any records with different tags are silently omitted. The
        /// tag is ignored. The tag is ignores for fixed-length prefixes.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize.</typeparam>
        /// <param name="source">The binary stream containing the serialized records.</param>
        /// <param name="style">The prefix style used in the data.</param>
        /// <param name="expectedField">The tag of records to return (if non-positive, then no tag is
        /// expected and all records are returned).</param>
        /// <returns>The sequence of deserialized objects.</returns>
        /// <param name="context">Additional information about this serialization operation.</param>
        public IEnumerable<T> DeserializeItems<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(Stream source, PrefixStyle style, int expectedField, SerializationContext context)
        {
            return new DeserializeItemsIterator<T>(this, source, style, expectedField, context);
        }

        private sealed class DeserializeItemsIterator<T> : DeserializeItemsIterator,
            IEnumerator<T>,
            IEnumerable<T>
        {
            IEnumerator<T> IEnumerable<T>.GetEnumerator() { return this; }
            public new T Current { get { return (T)base.Current; } }
            void IDisposable.Dispose() { }
            public DeserializeItemsIterator(TypeModel model, Stream source, PrefixStyle style, int expectedField, SerializationContext context)
                : base(model, source, typeof(T), style, expectedField, null, context) { }
        }

        private class DeserializeItemsIterator : IEnumerator, IEnumerable
        {
            IEnumerator IEnumerable.GetEnumerator() { return this; }
            private bool haveObject;
            private object current;
            public bool MoveNext()
            {
                if (haveObject)
                {
                    current = model.DeserializeWithLengthPrefix(source, null, type, style, expectedField, resolver, out long _, out haveObject, context);
                }
                return haveObject;
            }
            void IEnumerator.Reset() { ThrowHelper.ThrowNotSupportedException(); }
            public object Current { get { return current; } }
            private readonly Stream source;
            private readonly Type type;
            private readonly PrefixStyle style;
            private readonly int expectedField;
            private readonly TypeResolver resolver;
            private readonly TypeModel model;
            private readonly SerializationContext context;
            public DeserializeItemsIterator(TypeModel model, Stream source, Type type, PrefixStyle style, int expectedField, TypeResolver resolver, SerializationContext context)
            {
                haveObject = true;
                this.source = source;
                this.type = type;
                this.style = style;
                this.expectedField = expectedField;
                this.resolver = resolver;
                this.model = model;
                this.context = context;
            }
        }

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream,
        /// with a length-prefix. This is useful for socket programming,
        /// as DeserializeWithLengthPrefix can be used to read the single object back
        /// from an ongoing stream.
        /// </summary>
        /// <param name="type">The type being serialized.</param>
        /// <param name="value">The existing instance to be serialized (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <param name="dest">The destination stream to write to.</param>
        /// <param name="fieldNumber">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
        public void SerializeWithLengthPrefix(Stream dest, object value, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, PrefixStyle style, int fieldNumber)
        {
            SerializeWithLengthPrefix(dest, value, type, style, fieldNumber, null);
        }

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream,
        /// with a length-prefix. This is useful for socket programming,
        /// as DeserializeWithLengthPrefix can be used to read the single object back
        /// from an ongoing stream.
        /// </summary>
        /// <param name="type">The type being serialized.</param>
        /// <param name="value">The existing instance to be serialized (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <param name="dest">The destination stream to write to.</param>
        /// <param name="fieldNumber">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
        /// <param name="context">Additional information about this serialization operation.</param>
        public void SerializeWithLengthPrefix(Stream dest, object value, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, PrefixStyle style, int fieldNumber, SerializationContext context)
        {
            if (type is null)
            {
                if (value is null) ThrowHelper.ThrowArgumentNullException(nameof(value));
                type = value.GetType();
            }

            var state = ProtoWriter.State.Create(dest, this, context);
            try
            {
                switch (style)
                {
                    case PrefixStyle.None:
                        if (!DynamicStub.TrySerializeRoot(type, this, ref state, value))
                            ThrowUnexpectedType(type, this);
                        break;
                    case PrefixStyle.Base128:
                    case PrefixStyle.Fixed32:
                    case PrefixStyle.Fixed32BigEndian:
                        state.WriteObject(value, type, style, fieldNumber);
                        break;
                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(style));
                        break;
                }
                state.Flush();
                state.Close();
            }
            catch
            {
                state.Abandon();
                throw;
            }
            finally
            {
                state.Dispose();
            }
        }
        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="type">The type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public object Deserialize(Stream source, object value, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type)
        {
            using var state = ProtoReader.State.Create(source, this, null, ProtoReader.TO_EOF);
            return state.DeserializeRootFallback(value, type);
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="type">The type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        /// <param name="context">Additional information about this serialization operation.</param>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public object Deserialize(Stream source, object value, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, SerializationContext context)
        {
            using var state = ProtoReader.State.Create(source, this, context, ProtoReader.TO_EOF);
            return state.DeserializeRootFallback(value, type);
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <typeparam name="T">The type (including inheritance) to consider.</typeparam>
        /// <param name="userState">Additional information about this serialization operation.</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public T Deserialize<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(Stream source, T value = default, object userState = null)
        {
            using var state = ProtoReader.State.Create(source, this, userState);
            return state.DeserializeRootImpl<T>(value);
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <typeparam name="T">The type (including inheritance) to consider.</typeparam>
        /// <param name="userState">Additional information about this serialization operation.</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public T Deserialize<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(ReadOnlyMemory<byte> source, T value = default, object userState = null)
        {
            using var state = ProtoReader.State.Create(source, this, userState);
            return state.DeserializeRootImpl<T>(value);
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <typeparam name="T">The type (including inheritance) to consider.</typeparam>
        /// <param name="userState">Additional information about this serialization operation.</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public unsafe T Deserialize<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(ReadOnlySpan<byte> source, T value = default, object userState = null)
        {
            // as an implementation detail, we sometimes need to be able to use iterator blocks etc - which
            // means we need to be able to persist the span as a memory; the only way to do this
            // *safely and reliably* is to pint the span for the duration of the deserialize, and throw the
            // pointer into a custom MemoryManager<byte> (pool the manager to reduce allocs)
            fixed (byte* ptr = source)
            {
                FixedMemoryManager wrapper = null;
                ProtoReader.State state = default;
                try
                {
                    wrapper = Pool<FixedMemoryManager>.TryGet() ?? new FixedMemoryManager();
                    state = ProtoReader.State.Create(wrapper.Init(ptr, source.Length), this, userState);
                    return state.DeserializeRootImpl<T>(value);
                }
                finally
                {
                    state.Dispose();
                    Pool<FixedMemoryManager>.Put(wrapper);
                }
            }
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <typeparam name="T">The type (including inheritance) to consider.</typeparam>
        /// <param name="userState">Additional information about this serialization operation.</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public T Deserialize<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(ReadOnlySequence<byte> source, T value = default, object userState = null)
        {
            using var state = ProtoReader.State.Create(source, this, userState);
            return state.DeserializeRootImpl<T>(value);
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="type">The type (including inheritance) to consider.</param>
        /// <param name="userState">Additional information about this serialization operation.</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="length">The number of bytes to consider (no limit if omitted).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public object Deserialize([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, Stream source, object value = default, object userState = null, long length = ProtoReader.TO_EOF)
        {
            using var state = ProtoReader.State.Create(source, this, userState, length);
            return state.DeserializeRootFallback(value, type);
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="type">The type (including inheritance) to consider.</param>
        /// <param name="userState">Additional information about this serialization operation.</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public object Deserialize([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, ReadOnlyMemory<byte> source, object value = default, object userState = null)
        {
            using var state = ProtoReader.State.Create(source, this, userState);
            return state.DeserializeRootFallback(value, type);
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="type">The type (including inheritance) to consider.</param>
        /// <param name="userState">Additional information about this serialization operation.</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public unsafe object Deserialize([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, ReadOnlySpan<byte> source, object value = default, object userState = null)
        {
            // as an implementation detail, we sometimes need to be able to use iterator blocks etc - which
            // means we need to be able to persist the span as a memory; the only way to do this
            // *safely and reliably* is to pint the span for the duration of the deserialize, and throw the
            // pointer into a custom MemoryManager<byte> (pool the manager to reduce allocs)
            fixed (byte* ptr = source)
            {
                FixedMemoryManager wrapper = null;
                ProtoReader.State state = default;
                try
                {
                    wrapper = Pool<FixedMemoryManager>.TryGet() ?? new FixedMemoryManager();
                    state = ProtoReader.State.Create(wrapper.Init(ptr, source.Length), this, userState);
                    return state.DeserializeRootFallback(value, type);
                }
                finally
                {
                    state.Dispose();
                    Pool<FixedMemoryManager>.Put(wrapper);
                }
            }
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="type">The type (including inheritance) to consider.</param>
        /// <param name="userState">Additional information about this serialization operation.</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public object Deserialize([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, ReadOnlySequence<byte> source, object value = default, object userState = null)
        {
            using var state = ProtoReader.State.Create(source, this, userState);
            return state.DeserializeRootFallback(value, type);
        }

        internal static bool PrepareDeserialize(object value, ref Type type)
        {
            if (type is null || type == typeof(object))
            {
                if (value is null) ThrowHelper.ThrowArgumentNullException(nameof(type));
                type = value.GetType();
            }

            bool autoCreate = true;
            Type underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType is null)
            {
                type = DynamicStub.GetEffectiveType(type);
            }
            else
            {
                type = underlyingType;
                autoCreate = false;
            }
            return autoCreate;
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="type">The type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="length">The number of bytes to consume.</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public object Deserialize(Stream source, object value, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, int length)
            => Deserialize(source, value, type, length, null);

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="type">The type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="length">The number of bytes to consume.</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public object Deserialize(Stream source, object value, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, long length)
            => Deserialize(source, value, type, length, null);

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="type">The type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="length">The number of bytes to consume (or -1 to read to the end of the stream).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        /// <param name="context">Additional information about this serialization operation.</param>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public object Deserialize(Stream source, object value, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, int length, SerializationContext context)
            => Deserialize(source, value, type, length == int.MaxValue ? long.MaxValue : (long)length, context);

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="type">The type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="length">The number of bytes to consume (or -1 to read to the end of the stream).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        /// <param name="context">Additional information about this serialization operation.</param>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public object Deserialize(Stream source, object value, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, long length, SerializationContext context)
        {
            var state = ProtoReader.State.Create(source, this, context, length);
            try
            {
                bool autoCreate = PrepareDeserialize(value, ref type);
                if (!DynamicStub.TryDeserializeRoot(type, this, ref state, ref value, autoCreate))
                {
                    value = state.DeserializeRootFallback(value, type);
                }
                return value;
            }
            finally
            {
                state.Dispose();
            }
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="type">The type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary payload  to apply to the instance.</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        /// <param name="userState">Additional information about this serialization operation.</param>
        public object Deserialize(ReadOnlyMemory<byte> source, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, object value = default, object userState = default)
        {
            var state = ProtoReader.State.Create(source, this, userState);
            try
            {
                bool autoCreate = PrepareDeserialize(value, ref type);
                if (!DynamicStub.TryDeserializeRoot(type, this, ref state, ref value, autoCreate))
                {
                    value = state.DeserializeRootFallback(value, type);
                }
                return value;
            }
            finally
            {
                state.Dispose();
            }
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="type">The type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary payload  to apply to the instance.</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        /// <param name="userState">Additional information about this serialization operation.</param>
        public object Deserialize(ReadOnlySequence<byte> source, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, object value = default, object userState = default)
        {
            var state = ProtoReader.State.Create(source, this, userState);
            try
            {
                bool autoCreate = PrepareDeserialize(value, ref type);
                if (!DynamicStub.TryDeserializeRoot(type, this, ref state, ref value, autoCreate))
                {
                    value = state.DeserializeRootFallback(value, type);
                }
                return value;
            }
            finally
            {
                state.Dispose();
            }
        }

        /// <summary>
        /// Applies a protocol-buffer reader to an existing instance (which may be null).
        /// </summary>
        /// <param name="type">The type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The reader to apply to the instance (cannot be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        [Obsolete(ProtoReader.PreferStateAPI, false)]
        public object Deserialize(ProtoReader source, object value, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type)
            => source.DefaultState().DeserializeRootFallbackWithModel(value, type, this);

        internal object DeserializeRootAny(ref ProtoReader.State state, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, object value, bool autoCreate)
        {
            if (!DynamicStub.TryDeserializeRoot(type, this, ref state, ref value, autoCreate))
            {
                // this returns true to say we actively found something, but a value is assigned either way (or throws)
                TryDeserializeAuxiliaryType(ref state, DataFormat.Default, TypeModel.ListItemTag, type, ref value, true, false, autoCreate, false, null, isRoot: true);
            }
            return value;
        }

        private bool TryDeserializeList(ref ProtoReader.State state, DataFormat format, int tag, Type listType, Type itemType, ref object value, bool isRoot)
        {
            bool found = false;
            object nextItem = null;
            IList list = value as IList;

            var arraySurrogate = list is null ? (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType), nonPublic: true) : null;

            while (TryDeserializeAuxiliaryType(ref state, format, tag, itemType, ref nextItem, true, true, true, true, value ?? listType, isRoot))
            {
                found = true;
                if (value is null && arraySurrogate is null)
                {
                    value = CreateListInstance(listType, itemType);
                    list = value as IList;
                }
                if (list is object)
                {
                    list.Add(nextItem);
                }
                else
                {
                    arraySurrogate.Add(nextItem);
                }
                nextItem = null;
            }
            if (arraySurrogate is object)
            {
                Array newArray;
                if (value is not null)
                {
                    if (arraySurrogate.Count == 0)
                    {   // we'll stay with what we had, thanks
                    }
                    else
                    {
                        Array existing = (Array)value;
                        newArray = Array.CreateInstance(itemType, existing.Length + arraySurrogate.Count);
                        Array.Copy(existing, newArray, existing.Length);
                        arraySurrogate.CopyTo(newArray, existing.Length);
                        value = newArray;
                    }
                }
                else
                {
                    newArray = Array.CreateInstance(itemType, arraySurrogate.Count);
                    arraySurrogate.CopyTo(newArray, 0);
                    value = newArray;
                }
            }
            return found;
        }

        private static object CreateListInstance(Type listType, Type itemType)
        {
            Type concreteListType = listType;

            if (listType.IsArray)
            {
                return Array.CreateInstance(itemType, 0);
            }

            if (!listType.IsClass || listType.IsAbstract
                || Helpers.GetConstructor(listType, Type.EmptyTypes, true) is null)
            {
                string fullName;
                bool handled = false;
                if (listType.IsInterface &&
                    (fullName = listType.FullName) is not null && fullName.Contains("Dictionary")) // have to try to be frugal here...
                {

                    if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IDictionary<,>))
                    {
                        Type[] genericTypes = listType.GetGenericArguments();
                        concreteListType = typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(genericTypes);
                        handled = true;
                    }

                    if (!handled && listType == typeof(IDictionary))
                    {
                        concreteListType = typeof(Hashtable);
                        handled = true;
                    }
                }

                if (!handled)
                {
                    concreteListType = typeof(System.Collections.Generic.List<>).MakeGenericType(itemType);
                    handled = true;
                }

                if (!handled)
                {
                    concreteListType = typeof(ArrayList);
#pragma warning disable IDE0059 // unnecessary assignment; I can reason better with it here, in case we need to add more scenarios
                    handled = true;
#pragma warning restore IDE0059
                }
            }
            return Activator.CreateInstance(concreteListType, nonPublic: true);
        }

        internal bool TryDeserializeAuxiliaryType(ref ProtoReader.SolidState state, DataFormat format, int tag, Type type, ref object value, bool skipOtherFields, bool asListItem, bool autoCreate, bool insideList, object parentListOrType, bool isRoot)
        {
            var liquid = state.Liquify();
            var result = TryDeserializeAuxiliaryType(ref liquid, format, tag, type, ref value,
                skipOtherFields, asListItem, autoCreate, insideList, parentListOrType, isRoot);
            state = liquid.Solidify();
            return result;
        }
        /// <summary>
        /// <para>
        /// This is the more "complete" version of Deserialize, which handles single instances of mapped types.
        /// The value is read as a complete field, including field-header and (for sub-objects) a
        /// length-prefix..kmc  
        /// </para>
        /// <para>
        /// In addition to that, this provides support for:
        ///  - basic values; individual int / string / Guid / etc
        ///  - IList sets of any type handled by TryDeserializeAuxiliaryType
        /// </para>
        /// </summary>
        internal bool TryDeserializeAuxiliaryType(ref ProtoReader.State state, DataFormat format, int tag, Type type, ref object value, bool skipOtherFields, bool asListItem, bool autoCreate, bool insideList, object parentListOrType, bool isRoot)
        {
            if (type is null) ThrowHelper.ThrowArgumentNullException(nameof(type));
            WireType wiretype = GetWireType(this, format, type);

            bool found = false;
            if (wiretype == WireType.None)
            {
#pragma warning disable CS0618 // don't matter; we want to kill the entire aux flow
                if (!TypeHelper.ResolveUniqueEnumerableT(type, out Type itemType))
#pragma warning restore CS0618
                    itemType = null;

                if (itemType is null && type.IsArray && type.GetArrayRank() == 1 && type != typeof(byte[]))
                {
                    itemType = type.GetElementType();
                }
                if (itemType is not null)
                {
                    if (insideList) TypeModel.ThrowNestedListsNotSupported((parentListOrType as Type) ?? (parentListOrType?.GetType()));
                    found = TryDeserializeList(ref state, format, tag, type, itemType, ref value, isRoot);
                    if (!found && autoCreate)
                    {
                        value = CreateListInstance(type, itemType);
                    }
                    return found;
                }

                // otherwise, not a happy bunny...
                ThrowUnexpectedType(type, this);
            }

            if (!DynamicStub.CanSerialize(type, this, out var features))
                ThrowHelper.ThrowInvalidOperationException($"Unable to deserialize aux type: " + type.NormalizeName());

            // to treat correctly, should read all values
            while (true)
            {
                // for convenience (re complex exit conditions), additional exit test here:
                // if we've got the value, are only looking for one, and we aren't a list - then exit
                if (found && asListItem) break;

                // read the next item
                int fieldNumber = state.ReadFieldHeader();
                if (fieldNumber <= 0) break;
                if (fieldNumber != tag)
                {
                    if (skipOtherFields)
                    {
                        state.SkipField();
                        continue;
                    }
                    state.ThrowInvalidOperationException($"Expected field {tag}, but found {fieldNumber}");
                }
                found = true;
                state.Hint(wiretype); // handle signed data etc

                // this calls back into DynamicStub.TryDeserialize (with success assertion),
                // so will handle primitives etc
                var scope = NormalizeAuxScope(features, insideList, type, isRoot);
                value = Deserialize(scope, ref state, type, value);
            }
            if (!found && !asListItem && autoCreate)
            {
                if (type != typeof(string))
                {
                    value = Activator.CreateInstance(type, nonPublic: true);
                }
            }
            return found;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static TypeModel SetDefaultModel(TypeModel newValue)
        {
            switch (newValue)
            {
                case null:
                case NullModel _:
                    // set to the null instance, but only if the field is null
                    Interlocked.CompareExchange(ref s_defaultModel, NullModel.Singleton, null);
                    break;
                default:
                    // something more exotic? (presumably RuntimeTypeModel); yeah, OK
                    Interlocked.Exchange(ref s_defaultModel, newValue);
                    break;
            }
            return Volatile.Read(ref s_defaultModel);
        }
        private static TypeModel s_defaultModel;

        internal static void ResetDefaultModel()
            => Volatile.Write(ref s_defaultModel, null);

        internal static TypeModel DefaultModel => s_defaultModel ?? SetDefaultModel(null);

        internal sealed class NullModel : TypeModel
        {
            private NullModel() { }
            private static readonly NullModel s_Singleton = new NullModel();

            public static TypeModel Singleton
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                get => s_Singleton;
            }
            protected override ISerializer<T> GetSerializer<T>() => null;
        }

        /// <summary>
        /// Creates a new runtime model, to which the caller
        /// can add support for a range of types. A model
        /// can be used "as is", or can be compiled for
        /// optimal performance.
        /// </summary>
        [Obsolete("Use RuntimeTypeModel.Create", true)]
        public static TypeModel Create()
        {
            ThrowHelper.ThrowNotSupportedException();
            return default;
        }

        /// <summary>
        /// Create a model that serializes all types from an
        /// assembly specified by type
        /// </summary>
        [Obsolete("Use RuntimeTypeModel.CreateForAssembly", true)]
        public static TypeModel CreateForAssembly<T>()
        {
            ThrowHelper.ThrowNotSupportedException();
            return default;
        }

        /// <summary>
        /// Create a model that serializes all types from an
        /// assembly specified by type
        /// </summary>
        [Obsolete("Use RuntimeTypeModel.CreateForAssembly", true)]
        public static TypeModel CreateForAssembly(Type type)
        {
            ThrowHelper.ThrowNotSupportedException();
            return default;
        }

        /// <summary>
        /// Create a model that serializes all types from an assembly
        /// </summary>
        [Obsolete("Use RuntimeTypeModel.CreateForAssembly", true)]
        public static TypeModel CreateForAssembly(Assembly assembly)
        {
            ThrowHelper.ThrowNotSupportedException();
            return default;
        }

        /// <summary>
        /// Indicates whether the supplied type is explicitly modelled by the model
        /// </summary>
        public bool IsDefined(Type type) => IsDefined(type, default);

        /// <summary>
        /// Indicates whether the supplied type is explicitly modelled by the model
        /// </summary>
        internal bool IsDefined(Type type, CompatibilityLevel ambient) => type is not null && DynamicStub.IsKnownType(type, this, ambient);

        /// <summary>
        /// Get a typed serializer for <typeparamref name="T"/>
        /// </summary>
        protected virtual ISerializer<T> GetSerializer<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => this as ISerializer<T>;

        internal virtual ISerializer<T> GetSerializerCore<T>(CompatibilityLevel ambient)
            => GetSerializer<T>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ISerializer<T> NoSerializer<T>(TypeModel model)
        {
            string suffix = null;
            if (model is NullModel)
            {
                suffix = "; you may need to ensure that RuntimeTypeModel.Initialize has been invoked";
            }
            ThrowHelper.ThrowInvalidOperationException($"No serializer for type {typeof(T).NormalizeName()} is available for model {model?.ToString() ?? "(none)"}{suffix}");
            return default;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ISubTypeSerializer<T> NoSubTypeSerializer<T>(TypeModel model) where T : class
        {
            ThrowHelper.ThrowInvalidOperationException($"No sub-type serializer for type {typeof(T).NormalizeName()} is available for model {model?.ToString() ?? "(none)"}");
            return default;
        }

        internal static T CreateInstance<T>(ISerializationContext context, ISerializer<T> serializer = null)
        {
            if (TypeHelper<T>.IsReferenceType)
            {
                serializer ??= TypeModel.TryGetSerializer<T>(context?.Model);
                T obj = default;
                if (serializer is IFactory<T> factory) obj = factory.Create(context);

                // note we already know this is a ref-type
                obj ??= ActivatorCreate<T>();
                return obj;
            }
            else
            {
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static T ActivatorCreate<T>()
        {
            try
            {
                return (T)Activator.CreateInstance(typeof(T), nonPublic: true);
            }
            catch (MissingMethodException mme)
            {
                TypeModel.ThrowCannotCreateInstance(typeof(T), mme);
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ISerializer<T> GetSerializer<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(TypeModel model, CompatibilityLevel ambient = default)
           => SerializerCache<PrimaryTypeProvider, T>.InstanceField
            ?? model?.GetSerializerCore<T>(ambient)
            ?? NoSerializer<T>(model);

        /// <summary>
        /// Gets the inbuilt serializer relevant to a specific <see cref="CompatibilityLevel"/> (and <see cref="DataFormat"/>).
        /// Returns null if there is no defined inbuilt serializer.
        /// </summary>
#if DEBUG   // I always want these explicitly specified in the library code; so: enforce that
        public static ISerializer<T> GetInbuiltSerializer<T>(CompatibilityLevel compatibilityLevel, DataFormat dataFormat)
#else
        public static ISerializer<T> GetInbuiltSerializer<T>(CompatibilityLevel compatibilityLevel = default, DataFormat dataFormat = DataFormat.Default)
#endif
        {
            ISerializer<T> serializer;
            if (compatibilityLevel >= CompatibilityLevel.Level300)
            {
                if (dataFormat == DataFormat.FixedSize)
                {
                    serializer = SerializerCache<Level300FixedSerializer, T>.InstanceField;
                    if (serializer is object) return serializer;
                }
                serializer = SerializerCache<Level300DefaultSerializer, T>.InstanceField;
                if (serializer is object) return serializer;
            }
#pragma warning disable CS0618
            else if (compatibilityLevel >= CompatibilityLevel.Level240 || dataFormat == DataFormat.WellKnown)
#pragma warning restore CS0618
            {
                serializer = SerializerCache<Level240DefaultSerializer, T>.InstanceField;
                if (serializer is object) return serializer;
            }

            return SerializerCache<PrimaryTypeProvider, T>.InstanceField;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static IRepeatedSerializer<T> GetRepeatedSerializer<T>(TypeModel model)
        {
            if (model?.GetSerializer<T>() is IRepeatedSerializer<T> serializer) return serializer;
            NoSerializer<T>(model);
            return default;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ISerializer<T> TryGetSerializer<T>(TypeModel model)
          => SerializerCache<PrimaryTypeProvider, T>.InstanceField
            ?? model?.GetSerializer<T>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ISubTypeSerializer<T> GetSubTypeSerializer<T>(TypeModel model) where T : class
           => model?.GetSerializer<T>() as ISubTypeSerializer<T>
            ?? NoSubTypeSerializer<T>(model);

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="type">Represents the type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="state">Reader state</param>
        /// <param name="scope">The style of serialization to adopt</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        internal object Deserialize(ObjectScope scope, ref ProtoReader.State state, Type type, object value)
        {
            if (!DynamicStub.TryDeserialize(scope, type, this, ref state, ref value))
            {
                ThrowHelper.ThrowNotSupportedException($"{nameof(Deserialize)} is not supported for {type.NormalizeName()} by {this}");
            }
            return value;
        }

        /// <summary>
        /// Indicates the type of callback to be used
        /// </summary>
        protected internal enum CallbackType
        {
            /// <summary>
            /// Invoked before an object is serialized
            /// </summary>
            BeforeSerialize,
            /// <summary>
            /// Invoked after an object is serialized
            /// </summary>
            AfterSerialize,
            /// <summary>
            /// Invoked before an object is deserialized (or when a new instance is created)
            /// </summary>            
            BeforeDeserialize,
            /// <summary>
            /// Invoked after an object is deserialized
            /// </summary>
            AfterDeserialize
        }

        /// <summary>
        /// Create a deep clone of the supplied instance; any sub-items are also cloned.
        /// </summary>
        public T DeepClone<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(T value, object userState = null)
        {
#if PLAT_ISREF
            if (!System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                return value; // whether it is trivial or complex, we already have a full clone of a value-type
#endif
            if (TypeHelper<T>.CanBeNull && TypeHelper<T>.ValueChecker.IsNull(value)) return value;

            var serializer = TryGetSerializer<T>(this);
            if (serializer is null)
            {
                return (T)DeepCloneFallback(typeof(T), value);
            }
            else if ((serializer.Features & SerializerFeatures.CategoryScalar) != 0)
            {
                // scalars should be immutable; if not: that's on you!
                return value;
            }
            else
            {
                using var ms = new MemoryStream();
                Serialize<T>(ms, value, userState);
                ms.Position = 0;
                return Deserialize<T>(ms, default, userState);
            }
        }

        /// <summary>
        /// Create a deep clone of the supplied instance; any sub-items are also cloned.
        /// </summary>
        public object DeepClone(object value)
        {
            if (value is null) return null;
            Type type = value.GetType();
            return DynamicStub.TryDeepClone(type, this, ref value)
                ? value : DeepCloneFallback(type, value);
        }

        private object DeepCloneFallback(Type type, object value)
        {
            // must be some kind of aux scenario, then
            using MemoryStream ms = new MemoryStream();
            var writeState = ProtoWriter.State.Create(ms, this, null);
            PrepareDeserialize(value, ref type);
            try
            {
                if (!TrySerializeAuxiliaryType(ref writeState, type, DataFormat.Default, TypeModel.ListItemTag, value, false, null, isRoot: true))
                    ThrowUnexpectedType(type, this);
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
            ms.Position = 0;
            var readState = ProtoReader.State.Create(ms, this, null, ProtoReader.TO_EOF);
            try
            {
                value = null; // start from scratch!
                TryDeserializeAuxiliaryType(ref readState, DataFormat.Default, TypeModel.ListItemTag, type, ref value, true, false, true, false, null, isRoot: true);
            }
            finally
            {
                readState.Dispose();
            }

            return value;
        }

        /// <summary>
        /// Indicates that while an inheritance tree exists, the exact type encountered was not
        /// specified in that hierarchy and cannot be processed.
        /// </summary>
        protected internal static void ThrowUnexpectedSubtype(Type expected, Type actual)
        {
            if (!DynamicStub.IsTypeEquivalent(expected, actual))
            {
                ThrowHelper.ThrowInvalidOperationException("Unexpected sub-type: " + actual.FullName);
            }
        }

        /// <summary>
        /// Indicates that while an inheritance tree exists, the exact type encountered was not
        /// specified in that hierarchy and cannot be processed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowUnexpectedSubtype<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(T value) where T : class
        {
            if (IsSubType<T>(value)) ThrowUnexpectedSubtype(typeof(T), value.GetType());
        }

        /// <summary>
        /// Indicates that while an inheritance tree exists, the exact type encountered was not
        /// specified in that hierarchy and cannot be processed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowUnexpectedSubtype<T, TConstruct>(T value)
            where T : class
            where TConstruct : class, T
        {
            if (IsSubType<T>(value) && value.GetType() != typeof(TConstruct))
                ThrowUnexpectedSubtype(typeof(T), value.GetType());
        }

        /// <summary>
        /// Returns whether the object provided is a subtype of the expected type
        /// </summary>
        public static bool IsSubType<T>(T value) where T : class
            => value is object && typeof(T) != value.GetType();

        /// <summary>
        /// Indicates that the given type was not expected, and cannot be processed.
        /// </summary>
        protected internal static void ThrowUnexpectedType(Type type, TypeModel model)
        {
            string fullName = type is null ? "(unknown)" : type.FullName;

            if (type is not null)
            {
                Type baseType = type.BaseType;
                if (baseType is not null && baseType
                    .IsGenericType && baseType.GetGenericTypeDefinition().Name == "GeneratedMessage`2")
                {
                    ThrowHelper.ThrowInvalidOperationException(
                        "Are you mixing protobuf-net and protobuf-csharp-port? See https://stackoverflow.com/q/11564914/23354; type: " + fullName);
                }
            }

            try
            {
                ThrowHelper.ThrowInvalidOperationException("Type is not expected, and no contract can be inferred: " + fullName);
            }
            catch (Exception ex) when (model is not null)
            {
                ex.Data["TypeModel"] = model.ToString();
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNestedListsNotSupported(Type type)
            => ThrowHelper.ThrowNestedDataNotSupported(type);

        /// <summary>
        /// Indicates that the given type cannot be constructed; it may still be possible to 
        /// deserialize into existing instances.
        /// </summary>
        public static void ThrowCannotCreateInstance(Type type, Exception inner = null)
        {
            ThrowHelper.ThrowProtoException("No parameterless constructor found for " + (type?.FullName ?? "(null)"), inner);
        }

        internal static string SerializeType(TypeModel model, System.Type type)
        {
            if (model is not null)
            {
                TypeFormatEventHandler handler = model.DynamicTypeFormatting;
                if (handler is not null)
                {
                    TypeFormatEventArgs args = new TypeFormatEventArgs(type);
                    handler(model, args);
                    if (!string.IsNullOrEmpty(args.FormattedName)) return args.FormattedName;
                }
            }
            return type.AssemblyQualifiedName;
        }

        internal static Type DeserializeType(TypeModel model, string value)
        {
            if (model is not null)
            {
                TypeFormatEventHandler handler = model.DynamicTypeFormatting;
                if (handler is not null)
                {
                    TypeFormatEventArgs args = new TypeFormatEventArgs(value);
                    handler(model, args);
                    if (args.Type is not null) return args.Type;
                }
            }
            return Type.GetType(value);
        }

        /// <summary>
        /// Returns true if the type supplied is either a recognised contract type,
        /// or a *list* of a recognised contract type. 
        /// </summary>
        /// <remarks>Note that primitives always return false, even though the engine
        /// will, if forced, try to serialize such</remarks>
        /// <returns>True if this type is recognised as a serializable entity, else false</returns>
        public bool CanSerializeContractType(Type type) => CanSerialize(type, false, true, true, out _);

        /// <summary>
        /// Returns true if the type supplied is a basic type with inbuilt handling,
        /// a recognised contract type, or a *list* of a basic / contract type. 
        /// </summary>
        public bool CanSerialize(Type type) => CanSerialize(type, true, true, true, out _);

        /// <summary>
        /// Returns true if the type supplied is a basic type with inbuilt handling,
        /// or a *list* of a basic type with inbuilt handling
        /// </summary>
        public bool CanSerializeBasicType(Type type) => CanSerialize(type, true, false, true, out _);


        internal bool CanSerialize(Type type, bool allowBasic, bool allowContract, bool allowLists, out SerializerFeatures category)
        {
            if (type is null) ThrowHelper.ThrowArgumentNullException(nameof(type));

            static bool CheckIfNullableT(ref Type type)
            {
                Type tmp = Nullable.GetUnderlyingType(type);
                if (tmp is not null)
                {
                    type = tmp;
                    return true;
                }
                return false;
            }

            do
            {
                if (DynamicStub.CanSerialize(type, this, out var features))
                {
                    category = features.GetCategory();
                    switch (category)
                    {
                        case SerializerFeatures.CategoryRepeated:
                            return allowLists && DoCheckLists(type, this, allowBasic, allowContract);
                        case SerializerFeatures.CategoryMessage:
                            return allowContract;
                        case SerializerFeatures.CategoryScalar:
                        case SerializerFeatures.CategoryMessageWrappedAtRoot:
                            return allowBasic;
                    }
                }
            } while (CheckIfNullableT(ref type));

            static bool DoCheckLists(Type type, TypeModel model, bool allowBasic, bool allowContract)
            {
                // is it a list?
#pragma warning disable CS0618 // this is a legit usage
                return TypeHelper.ResolveUniqueEnumerableT(type, out var itemType)
                    && model.CanSerialize(itemType, allowBasic, allowContract, false, out _);
#pragma warning restore CS0618
            }
            category = default;
            return false;
        }

        /// <summary>
        /// Suggest a .proto definition for the given type
        /// </summary>
        /// <param name="type">The type to generate a .proto definition for, or <c>null</c> to generate a .proto that represents the entire model</param>
        /// <returns>The .proto definition as a string</returns>
        public string GetSchema([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type) => GetSchema(type, ProtoSyntax.Default);

        /// <summary>
        /// Suggest a .proto definition for the given type
        /// </summary>
        /// <param name="type">The type to generate a .proto definition for, or <c>null</c> to generate a .proto that represents the entire model</param>
        /// <returns>The .proto definition as a string</returns>
        /// <param name="syntax">The .proto syntax to use for the operation</param>
        public string GetSchema([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, ProtoSyntax syntax)
        {
            SchemaGenerationOptions options;
            if (type is null && syntax == ProtoSyntax.Default)
            {
                options = SchemaGenerationOptions.Default;
            }
            else
            {
                options = new SchemaGenerationOptions { Syntax = syntax };
                if (type is not null) options.Types.Add(type);
            }
            return GetSchema(options);
        }


        /// <summary>
        /// Suggest a .proto definition for the given configuration
        /// </summary>
        /// <returns>The .proto definition as a string</returns>
        /// <param name="options">Options for schema generation</param>
        public virtual string GetSchema(SchemaGenerationOptions options)
        {
            ThrowHelper.ThrowNotSupportedException();
            return default;
        }

        /// <summary>
        /// Used to provide custom services for writing and parsing type names when using dynamic types. Both parsing and formatting
        /// are provided on a single API as it is essential that both are mapped identically at all times.
        /// </summary>
        public event TypeFormatEventHandler DynamicTypeFormatting;

        /// <summary>
        /// Creates a new IFormatter that uses protocol-buffer [de]serialization.
        /// </summary>
        /// <returns>A new IFormatter to be used during [de]serialization.</returns>
        /// <param name="type">The type of object to be [de]deserialized by the formatter.</param>
        public System.Runtime.Serialization.IFormatter CreateFormatter([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type)
        {
            return new Formatter(this, type);
        }

        internal sealed class Formatter : System.Runtime.Serialization.IFormatter
        {
            private readonly TypeModel model;
            private readonly Type type;
            internal Formatter(TypeModel model, Type type)
            {
                if (model is null) ThrowHelper.ThrowArgumentNullException(nameof(model));
                if (type is null) ThrowHelper.ThrowArgumentNullException(nameof(model));
                this.model = model;
                this.type = type;
            }

            public System.Runtime.Serialization.SerializationBinder Binder { get; set; }

            public System.Runtime.Serialization.StreamingContext Context { get; set; }

            public object Deserialize(Stream serializationStream)
            {
                using var state = ProtoReader.State.Create(serializationStream, model, Context);
                return state.DeserializeRootFallback(null, type);
            }

            public void Serialize(Stream serializationStream, object graph)
            {
                var state = ProtoWriter.State.Create(serializationStream, model, Context);
                try
                {
                    model.SerializeRootFallback(ref state, graph);
                }
                finally
                {
                    state.Dispose();
                }
            }

            public System.Runtime.Serialization.ISurrogateSelector SurrogateSelector { get; set; }
        }

#if DEBUG // this is used by some unit tests only, to ensure no buffering when buffering is disabled
        /// <summary>
        /// If true, buffering of nested objects is disabled
        /// </summary>
        public bool ForwardsOnly { get; set; }
#endif

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static Type ResolveKnownType(string name, Assembly assembly)
        {
            if (string.IsNullOrEmpty(name)) return null;
            try
            {
                Type type = Type.GetType(name);

                if (type is not null) return type;
            }
            catch { }
            try
            {
                int i = name.IndexOf(',');
                string fullName = (i > 0 ? name.Substring(0, i) : name).Trim();

                assembly ??= Assembly.GetCallingAssembly();

                Type type = assembly?.GetType(fullName);
                if (type is not null) return type;
            }
            catch { }
            return null;
        }

        /// <summary>
        /// The field number that is used as a default when serializing/deserializing a list of objects.
        /// The data is treated as repeated message with field number 1.
        /// </summary>
        public const int ListItemTag = 1;
    }
}