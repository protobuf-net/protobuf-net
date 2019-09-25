using ProtoBuf.Internal;
using ProtoBuf.WellKnownTypes;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// Provides protobuf serialization support for a number of types
    /// </summary>
    public abstract class TypeModel
    {
        /// <summary>
        /// Should the <c>Kind</c> be included on date/time values?
        /// </summary>
        protected internal virtual bool SerializeDateTimeKind() { return false; }

        /// <summary>
        /// Resolve a System.Type to the compiler-specific type
        /// </summary>
        [Obsolete]
        protected internal Type MapType(Type type) => type;

#pragma warning disable RCS1163 // Unused parameter.
        /// <summary>
        /// Resolve a System.Type to the compiler-specific type
        /// </summary>
        [Obsolete]
        protected internal Type MapType(Type type, bool demand) => type;
#pragma warning restore RCS1163 // Unused parameter.

        internal static WireType GetWireType(TypeModel model, ProtoTypeCode code, DataFormat format, ref Type type, out bool isKnownType)
        {
            isKnownType = false;
            if (type.IsEnum)
            {
                if (model != null)
                    isKnownType = model.IsKnownType(ref type);
                return WireType.Varint;
            }
            switch (code)
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
                case ProtoTypeCode.TimeSpan:
                case ProtoTypeCode.Guid:
                case ProtoTypeCode.Uri:
                    return WireType.String;
            }

            if (model != null && (isKnownType = model.IsKnownType(ref type)))
            {
                return WireType.String;
            }
            return WireType.None;
        }

        /// <summary>
        /// Indicates whether a type is known to the model
        /// </summary>
        protected internal virtual bool IsKnownType<T>() => GetMessageSerializer<T>() != null || GetScalarSerializer<T>() != null;

        /// <summary>
        /// This is the more "complete" version of Serialize, which handles single instances of mapped types.
        /// The value is written as a complete field, including field-header and (for sub-objects) a
        /// length-prefix
        /// In addition to that, this provides support for:
        ///  - basic values; individual int / string / Guid / etc
        ///  - IEnumerable sequences of any type handled by TrySerializeAuxiliaryType
        ///  
        /// </summary>
        internal bool TrySerializeAuxiliaryType(ref ProtoWriter.State state, Type type, DataFormat format, int tag, object value, bool isInsideList, object parentList)
        {
            if (type == null) { type = value.GetType(); }

            ProtoTypeCode typecode = Helpers.GetTypeCode(type);
            // note the "ref type" here normalizes against proxies
            WireType wireType = GetWireType(this, typecode, format, ref type, out bool isKnown);

            if (isKnown)
            {   // write the header, but defer to the model
                if (type.IsEnum)
                { // no header
                    Serialize(ref state, type, value);
                    return true;
                }
                else
                {
                    state.WriteFieldHeader(tag, wireType);
                    switch (wireType)
                    {
                        case WireType.None:
                            state.ThrowInvalidSerializationOperation();
                            return false;
                        case WireType.StartGroup:
                        case WireType.String:
                            // needs a wrapping length etc
                            SubItemToken token = state.StartSubItem(value);
                            Serialize(ref state, type, value);
                            state.EndSubItem(token);
                            return true;
                        default:
                            Serialize(ref state, type, value);
                            return true;
                    }
                }
            }

            if (wireType != WireType.None)
            {
                state.WriteFieldHeader(tag, wireType);
            }
            switch (typecode)
            {
                case ProtoTypeCode.Int16: state.WriteInt16((short)value); return true;
                case ProtoTypeCode.Int32: state.WriteInt32((int)value); return true;
                case ProtoTypeCode.Int64: state.WriteInt64((long)value); return true;
                case ProtoTypeCode.UInt16: state.WriteUInt16((ushort)value); return true;
                case ProtoTypeCode.UInt32: state.WriteUInt32((uint)value); return true;
                case ProtoTypeCode.UInt64: state.WriteUInt64((ulong)value); return true;
                case ProtoTypeCode.Boolean: state.WriteBoolean((bool)value); return true;
                case ProtoTypeCode.SByte: state.WriteSByte((sbyte)value); return true;
                case ProtoTypeCode.Byte: state.WriteByte((byte)value); return true;
                case ProtoTypeCode.Char: state.WriteUInt16((ushort)(char)value); return true;
                case ProtoTypeCode.Double: state.WriteDouble((double)value); return true;
                case ProtoTypeCode.Single: state.WriteSingle((float)value); return true;
                case ProtoTypeCode.DateTime:
                    if (SerializeDateTimeKind())
                        BclHelpers.WriteDateTimeWithKind(ref state, (DateTime)value);
                    else
                        BclHelpers.WriteDateTime(ref state, (DateTime)value);
                    return true;
                case ProtoTypeCode.Decimal: BclHelpers.WriteDecimal(ref state, (decimal)value); return true;
                case ProtoTypeCode.String: state.WriteString((string)value); return true;
                case ProtoTypeCode.ByteArray: state.WriteBytes((byte[])value); return true;
                case ProtoTypeCode.TimeSpan: BclHelpers.WriteTimeSpan(ref state, (TimeSpan)value); return true;
                case ProtoTypeCode.Guid: BclHelpers.WriteGuid(ref state, (Guid)value); return true;
                case ProtoTypeCode.Uri: state.WriteString(((Uri)value).OriginalString); return true;
            }

            // by now, we should have covered all the simple cases; if we wrote a field-header, we have
            // forgotten something!
            Debug.Assert(wireType == WireType.None);

            // now attempt to handle sequences (including arrays and lists)
            if (value is IEnumerable sequence)
            {
                if (isInsideList) throw CreateNestedListsNotSupported(parentList?.GetType());
                foreach (object item in sequence)
                {
                    if (item == null) ThrowHelper.ThrowNullReferenceException();
                    if (!TrySerializeAuxiliaryType(ref state, null, format, tag, item, true, sequence))
                    {
                        ThrowUnexpectedType(item.GetType());
                    }
                }
                return true;
            }
            return false;
        }

        internal void SerializeCore(ref ProtoWriter.State state, object value)
        {
            if (value == null) ThrowHelper.ThrowArgumentNullException(nameof(value));
            Type type = value.GetType();
            if (state.IsKnownType(ref type))
            {
                Serialize(ref state, type, value);
            }
            else if (!TrySerializeAuxiliaryType(ref state, type, DataFormat.Default, TypeModel.ListItemTag, value, false, null))
            {
                ThrowUnexpectedType(type);
            }
        }

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream.
        /// </summary>
        /// <param name="value">The existing instance to be serialized (cannot be null).</param>
        /// <param name="dest">The destination stream to write to.</param>
        [Obsolete(PreferGenericAPI, DemandGenericAPI)]
        public void Serialize(Stream dest, object value)
        {
            var state = ProtoWriter.State.Create(dest, this);
            try
            {
                SerializeFallback(ref state, value);
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
        [Obsolete(PreferGenericAPI, DemandGenericAPI)]
        public void Serialize(Stream dest, object value, SerializationContext context)
        {
            var state = ProtoWriter.State.Create(dest, this, context);
            try
            {
                SerializeFallback(ref state, value);
            }
            finally
            {
                state.Dispose();
            }
        }

        internal void SerializeFallback(ref ProtoWriter.State state, object value)
        {
            if (!DynamicStub.TrySerialize(value.GetType(), this, ref state, value))
            {
                try
                {
                    state.SetRootObject(value);
                    SerializeCore(ref state, value);
                    state.Close();
                }
                catch
                {
                    state.Abandon();
                    throw;
                }
            }
        }

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream.
        /// </summary>
        /// <param name="value">The existing instance to be serialized (cannot be null).</param>
        /// <param name="dest">The destination stream to write to.</param>
        /// <param name="context">Additional information about this serialization operation.</param>
        public long Serialize<T>(Stream dest, T value, SerializationContext context = null)
        {
            var state = ProtoWriter.State.Create(dest, this, context);
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
        /// <param name="context">Additional information about this serialization operation.</param>
        public long Serialize<T>(IBufferWriter<byte> dest, T value, SerializationContext context = null)
        {
            var state = ProtoWriter.State.Create(dest, this, context);
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
        public long Measure<T>(T value, SerializationContext context = null)
        {
            var state = ProtoWriter.NullProtoWriter.CreateNullProtoWriter(this, context);
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
        /// <param name="dest">The destination writer to write to.</param>
        [Obsolete(ProtoReader.PreferStateAPI, false)]
        public void Serialize(ProtoWriter dest, object value)
        {
            ProtoWriter.State state = dest.DefaultState();
            SerializeFallback(ref state, value);
        }

        internal static long SerializeImpl<T>(ref ProtoWriter.State state, T value)
        {
            if (TypeHelper<T>.IsObjectType && value == null) return 0;
            if (TypeHelper<T>.UseFallback)
            {
                Debug.Assert(state.Model != null, "Model is null");
                long position = state.GetPosition();
                state.Model.SerializeFallback(ref state, value);
                return state.GetPosition() - position;
            }
            else
            {
                return state.Serialize<T>(value);
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
        public object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int fieldNumber)
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
        public object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int expectedField, TypeResolver resolver)
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
        public object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int expectedField, TypeResolver resolver, out int bytesRead)
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
        public object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int expectedField, TypeResolver resolver, out long bytesRead) => DeserializeWithLengthPrefix(source, value, type, style, expectedField, resolver, out bytesRead, out bool _, null);

        private object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int expectedField, TypeResolver resolver, out long bytesRead, out bool haveObject, SerializationContext context)
        {
            haveObject = false;
            bool skip;
            long len;
            bytesRead = 0;
            if (type == null && (style != PrefixStyle.Base128 || resolver == null))
            {
                ThrowHelper.ThrowInvalidOperationException("A type must be provided unless base-128 prefixing is being used in combination with a resolver");
            }
            do
            {
                bool expectPrefix = expectedField > 0 || resolver != null;
                len = ProtoReader.ReadLongLengthPrefix(source, expectPrefix, style, out int actualField, out int tmpBytesRead);
                if (tmpBytesRead == 0) return value;
                bytesRead += tmpBytesRead;
                if (len < 0) return value;

                switch (style)
                {
                    case PrefixStyle.Base128:
                        if (expectPrefix && expectedField == 0 && type == null && resolver != null)
                        {
                            type = resolver(actualField);
                            skip = type == null;
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
                if (IsKnownType(ref type) && !type.IsEnum)
                {
                    value = Deserialize(ref state, type, value);
                }
                else
                {
                    if (!(TryDeserializeAuxiliaryType(ref state, DataFormat.Default, TypeModel.ListItemTag, type, ref value, true, false, true, false, null) || len == 0))
                    {
                        TypeModel.ThrowUnexpectedType(type); // throws
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
        public IEnumerable DeserializeItems(System.IO.Stream source, Type type, PrefixStyle style, int expectedField, TypeResolver resolver)
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
        public IEnumerable DeserializeItems(System.IO.Stream source, Type type, PrefixStyle style, int expectedField, TypeResolver resolver, SerializationContext context)
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
        public IEnumerable<T> DeserializeItems<T>(Stream source, PrefixStyle style, int expectedField)
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
        public IEnumerable<T> DeserializeItems<T>(Stream source, PrefixStyle style, int expectedField, SerializationContext context)
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
        public void SerializeWithLengthPrefix(Stream dest, object value, Type type, PrefixStyle style, int fieldNumber)
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
        public void SerializeWithLengthPrefix(Stream dest, object value, Type type, PrefixStyle style, int fieldNumber, SerializationContext context)
        {
            if (type == null)
            {
                if (value == null) ThrowHelper.ThrowArgumentNullException(nameof(value));
                type = value.GetType();
            }

            var state = ProtoWriter.State.Create(dest, this, context);
            try
            {
                switch (style)
                {
                    case PrefixStyle.None:
                        Serialize(ref state, type, value);
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
        [Obsolete(PreferGenericAPI, DemandGenericAPI)]
        public object Deserialize(Stream source, object value, Type type)
        {
            using var state = ProtoReader.State.Create(source, this, null, ProtoReader.TO_EOF);
            return state.DeserializeFallback(value, type);
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
        [Obsolete(PreferGenericAPI, DemandGenericAPI)]
        public object Deserialize(Stream source, object value, Type type, SerializationContext context)
        {
            using var state = ProtoReader.State.Create(source, this, context, ProtoReader.TO_EOF);
            return state.DeserializeFallback(value, type);
        }

        internal const string PreferGenericAPI = "The non-generic API is sub-optimal; it is recommended to use the generic API whenever possible";

        //#if DEBUG
        //        internal const bool DemandGenericAPI = true;
        //#else
        internal const bool DemandGenericAPI = false;
        //#endif

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <typeparam name="T">The type (including inheritance) to consider.</typeparam>
        /// <param name="context">Additional information about this serialization operation.</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public T Deserialize<T>(Stream source, T value = default, SerializationContext context = null)
        {
            using var state = ProtoReader.State.Create(source, this, context);
            return state.DeserializeImpl<T>(value);
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <typeparam name="T">The type (including inheritance) to consider.</typeparam>
        /// <param name="context">Additional information about this serialization operation.</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public T Deserialize<T>(ReadOnlyMemory<byte> source, T value = default, SerializationContext context = null)
        {
            using var state = ProtoReader.State.Create(source, this, context);
            return state.DeserializeImpl<T>(value);
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <typeparam name="T">The type (including inheritance) to consider.</typeparam>
        /// <param name="context">Additional information about this serialization operation.</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public T Deserialize<T>(ReadOnlySequence<byte> source, T value = default, SerializationContext context = null)
        {
            using var state = ProtoReader.State.Create(source, this, context);
            return state.DeserializeImpl<T>(value);
        }

        internal bool PrepareDeserialize(object value, ref Type type)
        {
            if (type == null)
            {
                if (value == null)
                {
                    ThrowHelper.ThrowArgumentNullException(nameof(type));
                }
                else
                {
                    type = value.GetType();
                }
            }

            bool autoCreate = true;
            Type underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
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
        [Obsolete(TypeModel.PreferGenericAPI, TypeModel.DemandGenericAPI)]
        public object Deserialize(Stream source, object value, System.Type type, int length)
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
        [Obsolete(TypeModel.PreferGenericAPI, TypeModel.DemandGenericAPI)]
        public object Deserialize(Stream source, object value, System.Type type, long length)
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
        [Obsolete(TypeModel.PreferGenericAPI, TypeModel.DemandGenericAPI)]
        public object Deserialize(Stream source, object value, System.Type type, int length, SerializationContext context)
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
        [Obsolete(TypeModel.PreferGenericAPI, TypeModel.DemandGenericAPI)]
        public object Deserialize(Stream source, object value, System.Type type, long length, SerializationContext context)
        {
            bool autoCreate = PrepareDeserialize(value, ref type);
            var state = ProtoReader.State.Create(source, this, context, length);
            try
            {
                if (value != null) state.SetRootObject(value);
                object obj = DeserializeAny(ref state, type, value, autoCreate);
                state.CheckFullyConsumed();
                return obj;
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
        public object Deserialize(ProtoReader source, object value, Type type)
            => source.DefaultState().DeserializeFallback(value, type, this);

        internal object DeserializeAny(ref ProtoReader.State state, Type type, object value, bool noAutoCreate)
        {
            if (!DynamicStub.TryDeserialize(type, this, ref state, ref value))
            {
                if (IsKnownType(ref type) && !type.IsEnum)
                {
                    return Deserialize(ref state, type, value);
                }
                // this returns true to say we actively found something, but a value is assigned either way (or throws)
                TryDeserializeAuxiliaryType(ref state, DataFormat.Default, TypeModel.ListItemTag, type, ref value, true, false, noAutoCreate, false, null);
            }
            return value;
        }

        private static readonly System.Type ilist = typeof(IList);
        internal static MethodInfo ResolveListAdd(Type listType, Type itemType, out bool isList)
        {
            Type listTypeInfo = listType;
            isList = ilist.IsAssignableFrom(listTypeInfo);
            Type[] types = { itemType };
            MethodInfo add = Helpers.GetInstanceMethod(listTypeInfo, nameof(IList.Add), types);

            if (add == null)
            {   // fallback: look for ICollection<T>'s Add(typedObject) method
                bool forceList = listTypeInfo.IsInterface
                    && typeof(System.Collections.Generic.IEnumerable<>).MakeGenericType(types)
                    .IsAssignableFrom(listTypeInfo);

                Type constuctedListType = typeof(System.Collections.Generic.ICollection<>).MakeGenericType(types);
                if (forceList || constuctedListType.IsAssignableFrom(listTypeInfo))
                {
                    add = Helpers.GetInstanceMethod(constuctedListType, "Add", types);
                }
            }

            if (add == null)
            {
                foreach (Type interfaceType in listTypeInfo.GetInterfaces())
                {
                    if (interfaceType.Name == "IProducerConsumerCollection`1" && interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition().FullName == "System.Collections.Concurrent.IProducerConsumerCollection`1")
                    {
                        add = Helpers.GetInstanceMethod(interfaceType, "TryAdd", types);
                        if (add != null) break;
                    }
                }
            }

            if (add == null)
            {   // fallback: look for a public list.Add(object) method
                types[0] = typeof(object);
                add = Helpers.GetInstanceMethod(listTypeInfo, "Add", types);
            }
            if (add == null && isList)
            {   // fallback: look for IList's Add(object) method
                add = Helpers.GetInstanceMethod(ilist, "Add", types);
            }
            return add;
        }
        internal static Type GetListItemType(Type listType)
        {
            Debug.Assert(listType != null);

            if (listType == typeof(string) || listType.IsArray
                || !typeof(IEnumerable).IsAssignableFrom(listType)) { return null; }

            var candidates = new List<Type>();
            foreach (MethodInfo method in listType.GetMethods())
            {
                if (method.IsStatic || method.Name != "Add") continue;
                ParameterInfo[] parameters = method.GetParameters();
                Type paramType;
                if (parameters.Length == 1 && !candidates.Contains(paramType = parameters[0].ParameterType))
                {
                    candidates.Add(paramType);
                }
            }

            string name = listType.Name;
            bool isQueueStack = name != null && (name.IndexOf("Queue") >= 0 || name.IndexOf("Stack") >= 0);

            if (!isQueueStack)
            {
                TestEnumerableListPatterns(candidates, listType);
                foreach (Type iType in listType.GetInterfaces())
                {
                    TestEnumerableListPatterns(candidates, iType);
                }
            }

            // more convenient GetProperty overload not supported on all platforms
            foreach (PropertyInfo indexer in listType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (indexer.Name != "Item" || candidates.Contains(indexer.PropertyType)) continue;
                ParameterInfo[] args = indexer.GetIndexParameters();
                if (args.Length != 1 || args[0].ParameterType != typeof(int)) continue;
                candidates.Add(indexer.PropertyType);
            }

            switch (candidates.Count)
            {
                case 0:
                    return null;
                case 1:
                    if ((Type)candidates[0] == listType) return null; // recursive
                    return (Type)candidates[0];
                case 2:
                    if ((Type)candidates[0] != listType && CheckDictionaryAccessors((Type)candidates[0], (Type)candidates[1])) return (Type)candidates[0];
                    if ((Type)candidates[1] != listType && CheckDictionaryAccessors((Type)candidates[1], (Type)candidates[0])) return (Type)candidates[1];
                    break;
            }

            return null;
        }

        private static void TestEnumerableListPatterns(List<Type> candidates, Type iType)
        {
            if (iType.IsGenericType)
            {
                Type typeDef = iType.GetGenericTypeDefinition();
                if (typeDef == typeof(System.Collections.Generic.IEnumerable<>)
                    || typeDef == typeof(System.Collections.Generic.ICollection<>)
                    || typeDef.FullName == "System.Collections.Concurrent.IProducerConsumerCollection`1")
                {
                    Type[] iTypeArgs = iType.GetGenericArguments();
                    if (!candidates.Contains(iTypeArgs[0]))
                    {
                        candidates.Add(iTypeArgs[0]);
                    }
                }
            }
        }

        private static bool CheckDictionaryAccessors(Type pair, Type value)
        {
            return pair.IsGenericType && pair.GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>)
                && pair.GetGenericArguments()[1] == value;
        }

        private bool TryDeserializeList(ref ProtoReader.State state, DataFormat format, int tag, Type listType, Type itemType, ref object value)
        {
            MethodInfo addMethod = TypeModel.ResolveListAdd(listType, itemType, out bool isList);
            if (addMethod == null) ThrowHelper.ThrowNotSupportedException("Unknown list variant: " + listType.FullName);
            bool found = false;
            object nextItem = null;
            IList list = value as IList;
            object[] args = isList ? null : new object[1];
            var arraySurrogate = listType.IsArray ? (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType), nonPublic: true) : null;

            while (TryDeserializeAuxiliaryType(ref state, format, tag, itemType, ref nextItem, true, true, true, true, value ?? listType))
            {
                found = true;
                if (value == null && arraySurrogate == null)
                {
                    value = CreateListInstance(listType, itemType);
                    list = value as IList;
                }
                if (list != null)
                {
                    list.Add(nextItem);
                }
                else if (arraySurrogate != null)
                {
                    arraySurrogate.Add(nextItem);
                }
                else
                {
                    args[0] = nextItem;
                    addMethod.Invoke(value, args);
                }
                nextItem = null;
            }
            if (arraySurrogate != null)
            {
                Array newArray;
                if (value != null)
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
                || Helpers.GetConstructor(listType, Type.EmptyTypes, true) == null)
            {
                string fullName;
                bool handled = false;
                if (listType.IsInterface &&
                    (fullName = listType.FullName) != null && fullName.IndexOf("Dictionary") >= 0) // have to try to be frugal here...
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

        internal bool TryDeserializeAuxiliaryType(ref ProtoReader.SolidState state, DataFormat format, int tag, Type type, ref object value, bool skipOtherFields, bool asListItem, bool autoCreate, bool insideList, object parentListOrType)
        {
            var liquid = state.Liquify();
            var result = TryDeserializeAuxiliaryType(ref liquid, format, tag, type, ref value,
                skipOtherFields, asListItem, autoCreate, insideList, parentListOrType);
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
        internal bool TryDeserializeAuxiliaryType(ref ProtoReader.State state, DataFormat format, int tag, Type type, ref object value, bool skipOtherFields, bool asListItem, bool autoCreate, bool insideList, object parentListOrType)
        {
            if (type == null) ThrowHelper.ThrowArgumentNullException(nameof(type));
            Type itemType;
            ProtoTypeCode typecode = Helpers.GetTypeCode(type);
            WireType wiretype = GetWireType(this, typecode, format, ref type, out bool isKnown);

            bool found = false;
            if (wiretype == WireType.None)
            {
                itemType = GetListItemType(type);
                if (itemType == null && type.IsArray && type.GetArrayRank() == 1 && type != typeof(byte[]))
                {
                    itemType = type.GetElementType();
                }
                if (itemType != null)
                {
                    if (insideList) throw TypeModel.CreateNestedListsNotSupported((parentListOrType as Type) ?? (parentListOrType?.GetType()));
                    found = TryDeserializeList(ref state, format, tag, type, itemType, ref value);
                    if (!found && autoCreate)
                    {
                        value = CreateListInstance(type, itemType);
                    }
                    return found;
                }

                // otherwise, not a happy bunny...
                ThrowUnexpectedType(type);
            }

            // to treat correctly, should read all values

            while (true)
            {
                // for convenience (re complex exit conditions), additional exit test here:
                // if we've got the value, are only looking for one, and we aren't a list - then exit
#pragma warning disable RCS1218 // Simplify code branching.
                if (found && asListItem) break;
#pragma warning restore RCS1218 // Simplify code branching.

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

                if (isKnown)
                {
                    switch (wiretype)
                    {
                        case WireType.String:
                        case WireType.StartGroup:
                            SubItemToken token = state.StartSubItem();
                            value = Deserialize(ref state, type, value);
                            state.EndSubItem(token);
                            continue;
                        default:
                            value = Deserialize(ref state, type, value);
                            continue;
                    }
                }
                switch (typecode)
                {
                    case ProtoTypeCode.Int16: value = state.ReadInt16(); continue;
                    case ProtoTypeCode.Int32: value = state.ReadInt32(); continue;
                    case ProtoTypeCode.Int64: value = state.ReadInt64(); continue;
                    case ProtoTypeCode.UInt16: value = state.ReadUInt16(); continue;
                    case ProtoTypeCode.UInt32: value = state.ReadUInt32(); continue;
                    case ProtoTypeCode.UInt64: value = state.ReadUInt64(); continue;
                    case ProtoTypeCode.Boolean: value = state.ReadBoolean(); continue;
                    case ProtoTypeCode.SByte: value = state.ReadSByte(); continue;
                    case ProtoTypeCode.Byte: value = state.ReadByte(); continue;
                    case ProtoTypeCode.Char: value = (char)state.ReadUInt16(); continue;
                    case ProtoTypeCode.Double: value = state.ReadDouble(); continue;
                    case ProtoTypeCode.Single: value = state.ReadSingle(); continue;
                    case ProtoTypeCode.DateTime: value = BclHelpers.ReadDateTime(ref state); continue;
                    case ProtoTypeCode.Decimal: value = BclHelpers.ReadDecimal(ref state); continue;
                    case ProtoTypeCode.String: value = state.ReadString(); continue;
                    case ProtoTypeCode.ByteArray: value = state.AppendBytes((byte[])value); continue;
                    case ProtoTypeCode.TimeSpan: value = BclHelpers.ReadTimeSpan(ref state); continue;
                    case ProtoTypeCode.Guid: value = BclHelpers.ReadGuid(ref state); continue;
                    case ProtoTypeCode.Uri: value = new Uri(state.ReadString(), UriKind.RelativeOrAbsolute); continue;
                }
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
            // the point here is to allow:
            // 1. TypeModel.DefaultModel to set a non-null NullModel instance automagically
            // 2. RuntimeTypeModel.Default to override that, changing either a null or a NullModel
            // to the default RuntimeTypeModel
            // 3. there is no 3; those are the supported scenarios
            var fieldValue = Volatile.Read(ref s_defaultModel);
            if (fieldValue == null || fieldValue is NullModel)
            {
                if (newValue == null) newValue = NullModel.Instance;
                Interlocked.CompareExchange(ref s_defaultModel, newValue, fieldValue);
                fieldValue = Volatile.Read(ref s_defaultModel);
            }
            return fieldValue;
            
        }
        private static TypeModel s_defaultModel;
        internal static TypeModel DefaultModel => s_defaultModel ?? SetDefaultModel(null);

        private sealed class NullModel : TypeModel
        {
            private NullModel() { }
            public static NullModel Instance = new NullModel();
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
        /// Applies common proxy scenarios, resolving the actual type to consider
        /// </summary>
        protected internal static Type ResolveProxies(Type type)
        {
            if (type == null) return null;
            if (type.IsGenericParameter) return null;
            // Nullable<T>
            Type tmp = Nullable.GetUnderlyingType(type);
            if (tmp != null) return tmp;

            // EF POCO
            string fullName = type.FullName;
            if (fullName != null && fullName.StartsWith("System.Data.Entity.DynamicProxies."))
            {
                return type.BaseType;
            }

            // NHibernate
            Type[] interfaces = type.GetInterfaces();
            foreach (Type t in interfaces)
            {
                switch (t.FullName)
                {
                    case "NHibernate.Proxy.INHibernateProxy":
                    case "NHibernate.Proxy.DynamicProxy.IProxy":
                    case "NHibernate.Intercept.IFieldInterceptorAccessor":
                        return type.BaseType;
                }
            }
            return null;
        }

        /// <summary>
        /// Indicates whether the supplied type is explicitly modelled by the model
        /// </summary>
        public bool IsDefined(Type type) => IsKnownType(ref type);

        /// <summary>
        /// Get a typed serializer for <typeparamref name="T"/>
        /// </summary>
        protected internal virtual IMessageSerializer<T> GetMessageSerializer<T>()
            => this as IMessageSerializer<T>;

        /// <summary>
        /// Get a typed serializer for <typeparamref name="T"/>
        /// </summary>
        protected internal virtual IScalarSerializer<T> GetScalarSerializer<T>()
            => this as IScalarSerializer<T>;

        /// <summary>
        /// Get a factory for creating <typeparamref name="T"/> values
        /// </summary>
        protected internal virtual IFactory<T> GetFactory<T>()
            => this as IFactory<T>;

        /// <summary>
        /// Get a typed serializer for deserialzing <typeparamref name="T"/> as part of an inheritance model
        /// </summary>
        protected internal virtual ISubTypeSerializer<T> GetSubTypeSerializer<T>() where T : class
            => this as ISubTypeSerializer<T>;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IMessageSerializer<T> NoSerializer<T>(TypeModel model)
        {
            ThrowHelper.ThrowInvalidOperationException($"No serializer for type {(typeof(T))} is available for model {model?.ToString() ?? "(none)"}");
            return default;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ISubTypeSerializer<T> NoSubTypeSerializer<T>(TypeModel model) where T : class
        {
            ThrowHelper.ThrowInvalidOperationException($"No sub-type serializer for type {TypeHelper.CSName(typeof(T))} is available for model {model?.ToString() ?? "(none)"}");
            return default;
        }

        internal static T CreateInstance<T>(ISerializationContext context = null, IFactory<T> factory = null)
        {
            if (factory == null) factory = context?.Model?.GetFactory<T>();
            if (factory != null)
            {
                var val = factory.Create(context);
                if (TypeHelper<T>.IsObjectType)
                {
                    if (val != null) return val;
                }
                else
                {
                    return val;
                }
            }

            return Activator.CreateInstance<T>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static IMessageSerializer<T> GetSerializer<T>(TypeModel model)
           => model?.GetMessageSerializer<T>() ?? WellKnownSerializer.Instance as IMessageSerializer<T> ?? NoSerializer<T>(model);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ISubTypeSerializer<T> GetSubTypeSerializer<T>(TypeModel model) where T : class
           => model?.GetSubTypeSerializer<T>() ?? WellKnownSerializer.Instance as ISubTypeSerializer<T> ?? NoSubTypeSerializer<T>(model);

        /// <summary>
        /// Provides the key that represents a given type in the current model.
        /// The type is also normalized for proxies at the same time.
        /// </summary>
        protected internal bool IsKnownType(ref Type type)
        {
            if (type == null) return false;
            if (IsKnownTypeCore(type)) return true;

            return IsKnownTypeViaProxy(ref type);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool IsKnownTypeViaProxy(ref Type type)
        {
            Type normalized = ResolveProxies(type);
            if (normalized != null && normalized != type)
            {
                if (IsKnownTypeCore(normalized))
                {
                    type = normalized;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Indicates whether a type is known to the serializer
        /// </summary>
        private bool IsKnownTypeCore(Type type) => DynamicStub.IsKnownType(type, this);

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream.
        /// </summary>
        /// <param name="type">Represents the type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be serialized (cannot be null).</param>
        /// <param name="state">Write state</param>
        protected internal virtual void Serialize(ref ProtoWriter.State state, Type type, object value)
            => ThrowHelper.ThrowNotSupportedException($"{nameof(Serialize)} is not supported for {type} by {this}");

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="type">Represents the type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="state">Reader state</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        protected internal virtual object Deserialize(ref ProtoReader.State state, Type type, object value)
        {
            ThrowHelper.ThrowNotSupportedException($"{nameof(Deserialize)} is not supported for {type} by {this}");
            return default;
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
        public T DeepClone<T>(T value, SerializationContext context = null)
        {
            if (TypeHelper<T>.IsObjectType && value == null) return value;
            if (TypeHelper<T>.UseFallback)
            {
#pragma warning disable CS0618
                return (T)DeepClone((object)value);
#pragma warning restore CS0618
            }
            else
            {
                using var ms = new MemoryStream();
                Serialize<T>(ms, value, context);
                ms.Position = 0;
                return Deserialize<T>(ms, default, context);
            }
        }

        /// <summary>
        /// Create a deep clone of the supplied instance; any sub-items are also cloned.
        /// </summary>
        [Obsolete(PreferGenericAPI, false)]
        public object DeepClone(object value)
        {
            if (value == null) return null;
            Type type = value.GetType();
            if (DynamicStub.TryDeepClone(type, this, ref value))
            {
                return value;
            }
            else
            {
                if (IsKnownType(ref type) && !type.IsEnum)
                {
                    using MemoryStream ms = new MemoryStream();
                    var writeState = ProtoWriter.State.Create(ms, this, null);
                    try
                    {
                        writeState.SetRootObject(value);
                        try
                        {
                            Serialize(ref writeState, type, value);
                        }
                        catch
                        {
                            writeState.Abandon();
                            throw;
                        }
                        writeState.Close();
                    }
                    finally
                    {
                        writeState.Dispose();
                    }
                    ms.Position = 0;
                    var readState = ProtoReader.State.Create(ms, this, null, ProtoReader.TO_EOF);
                    try
                    {
                        return Deserialize(ref readState, type, null);
                    }
                    finally
                    {
                        readState.Dispose();
                    }
                }
                if (type == typeof(byte[]))
                {
                    byte[] orig = (byte[])value, clone = new byte[orig.Length];
                    Buffer.BlockCopy(orig, 0, clone, 0, orig.Length);
                    return clone;
                }
                else if (GetWireType(this, Helpers.GetTypeCode(type), DataFormat.Default, ref type, out bool isKnown) != WireType.None && !isKnown)
                {   // immutable; just return the original value
                    return value;
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    var writeState = ProtoWriter.State.Create(ms, this, null);
                    try
                    {
                        if (!TrySerializeAuxiliaryType(ref writeState, type, DataFormat.Default, TypeModel.ListItemTag, value, false, null)) ThrowUnexpectedType(type);
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
                        TryDeserializeAuxiliaryType(ref readState, DataFormat.Default, TypeModel.ListItemTag, type, ref value, true, false, true, false, null);
                    }
                    finally
                    {
                        readState.Dispose();
                    }

                    return value;
                }
            }
        }

        /// <summary>
        /// Indicates that while an inheritance tree exists, the exact type encountered was not
        /// specified in that hierarchy and cannot be processed.
        /// </summary>
        protected internal static void ThrowUnexpectedSubtype(Type expected, Type actual)
        {
            if (expected != TypeModel.ResolveProxies(actual))
            {
                ThrowHelper.ThrowInvalidOperationException("Unexpected sub-type: " + actual.FullName);
            }
        }

        /// <summary>
        /// Indicates that while an inheritance tree exists, the exact type encountered was not
        /// specified in that hierarchy and cannot be processed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowUnexpectedSubtype<T>(T value) where T : class
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
            => value != null && typeof(T) != value.GetType();

        /// <summary>
        /// Indicates that the given type was not expected, and cannot be processed.
        /// </summary>
        protected internal static void ThrowUnexpectedType(Type type)
        {
            string fullName = type == null ? "(unknown)" : type.FullName;

            if (type != null)
            {
                Type baseType = type.BaseType;
                if (baseType != null && baseType
                    .IsGenericType && baseType.GetGenericTypeDefinition().Name == "GeneratedMessage`2")
                {
                    ThrowHelper.ThrowInvalidOperationException(
                        "Are you mixing protobuf-net and protobuf-csharp-port? See https://stackoverflow.com/q/11564914/23354; type: " + fullName);
                }
            }

            ThrowHelper.ThrowInvalidOperationException("Type is not expected, and no contract can be inferred: " + fullName);
        }

        /// <summary>
        /// Global switch that determines whether a single instance of the same string should be used during deserialization.
        /// </summary>
        public bool InternStrings => GetInternStrings();

        /// <summary>
        /// Global switch that determines whether a single instance of the same string should be used during deserialization.
        /// </summary>
        protected internal virtual bool GetInternStrings() => false;

        internal static Exception CreateNestedListsNotSupported(Type type)
        {
            return new NotSupportedException("Nested or jagged lists and arrays are not supported: " + (type?.FullName ?? "(null)"));
        }

        /// <summary>
        /// Indicates that the given type cannot be constructed; it may still be possible to 
        /// deserialize into existing instances.
        /// </summary>
        public static void ThrowCannotCreateInstance(Type type)
        {
            ThrowHelper.ThrowProtoException("No parameterless constructor found for " + (type?.FullName ?? "(null)"));
        }

        internal static string SerializeType(TypeModel model, System.Type type)
        {
            if (model != null)
            {
                TypeFormatEventHandler handler = model.DynamicTypeFormatting;
                if (handler != null)
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
            if (model != null)
            {
                TypeFormatEventHandler handler = model.DynamicTypeFormatting;
                if (handler != null)
                {
                    TypeFormatEventArgs args = new TypeFormatEventArgs(value);
                    handler(model, args);
                    if (args.Type != null) return args.Type;
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
        public bool CanSerializeContractType(Type type) => CanSerialize(type, false, true, true);

        /// <summary>
        /// Returns true if the type supplied is a basic type with inbuilt handling,
        /// a recognised contract type, or a *list* of a basic / contract type. 
        /// </summary>
        public bool CanSerialize(Type type) => CanSerialize(type, true, true, true);

        /// <summary>
        /// Returns true if the type supplied is a basic type with inbuilt handling,
        /// or a *list* of a basic type with inbuilt handling
        /// </summary>
        public bool CanSerializeBasicType(Type type) => CanSerialize(type, true, false, true);

        private bool CanSerialize(Type type, bool allowBasic, bool allowContract, bool allowLists)
        {
            if (type == null) ThrowHelper.ThrowArgumentNullException(nameof(type));
            Type tmp = Nullable.GetUnderlyingType(type);
            if (tmp != null) type = tmp;

            // is it a basic type?
            ProtoTypeCode typeCode = Helpers.GetTypeCode(type);
            switch (typeCode)
            {
                case ProtoTypeCode.Empty:
                case ProtoTypeCode.Unknown:
                    break;
                default:
                    return allowBasic; // well-known basic type
            }

            if (IsKnownType(ref type)) return allowContract; // known contract type

            // is it a list?
            if (allowLists)
            {
                Type itemType = null;
                if (type.IsArray)
                {   // note we don't need to exclude byte[], as that is handled by GetTypeCode already
                    if (type.GetArrayRank() == 1) itemType = type.GetElementType();
                }
                else
                {
                    itemType = GetListItemType(type);
                }
                if (itemType != null) return CanSerialize(itemType, allowBasic, allowContract, false);
            }
            return false;
        }

        /// <summary>
        /// Suggest a .proto definition for the given type
        /// </summary>
        /// <param name="type">The type to generate a .proto definition for, or <c>null</c> to generate a .proto that represents the entire model</param>
        /// <returns>The .proto definition as a string</returns>
        public virtual string GetSchema(Type type) => GetSchema(type, ProtoSyntax.Proto2);

        /// <summary>
        /// Suggest a .proto definition for the given type
        /// </summary>
        /// <param name="type">The type to generate a .proto definition for, or <c>null</c> to generate a .proto that represents the entire model</param>
        /// <returns>The .proto definition as a string</returns>
        /// <param name="syntax">The .proto syntax to use for the operation</param>
        public virtual string GetSchema(Type type, ProtoSyntax syntax)
        {
            ThrowHelper.ThrowNotSupportedException();
            return default;
        }

#pragma warning disable RCS1159 // Use EventHandler<T>.
        /// <summary>
        /// Used to provide custom services for writing and parsing type names when using dynamic types. Both parsing and formatting
        /// are provided on a single API as it is essential that both are mapped identically at all times.
        /// </summary>
        public event TypeFormatEventHandler DynamicTypeFormatting;
#pragma warning restore RCS1159 // Use EventHandler<T>.

        /// <summary>
        /// Creates a new IFormatter that uses protocol-buffer [de]serialization.
        /// </summary>
        /// <returns>A new IFormatter to be used during [de]serialization.</returns>
        /// <param name="type">The type of object to be [de]deserialized by the formatter.</param>
        public System.Runtime.Serialization.IFormatter CreateFormatter(Type type)
        {
            return new Formatter(this, type);
        }

        internal sealed class Formatter : System.Runtime.Serialization.IFormatter
        {
            private readonly TypeModel model;
            private readonly Type type;
            internal Formatter(TypeModel model, Type type)
            {
                if (model == null) ThrowHelper.ThrowArgumentNullException(nameof(model));
                if (type == null) ThrowHelper.ThrowArgumentNullException(nameof(model));
                this.model = model;
                this.type = type;
            }

            public System.Runtime.Serialization.SerializationBinder Binder { get; set; }

            public System.Runtime.Serialization.StreamingContext Context { get; set; }

            public object Deserialize(Stream serializationStream)
            {
                using var state = ProtoReader.State.Create(serializationStream, model, Context);
                return state.DeserializeFallback(null, type);
            }

            public void Serialize(Stream serializationStream, object graph)
            {
                var state = ProtoWriter.State.Create(serializationStream, model, Context);
                try
                {
                    model.SerializeFallback(ref state, graph);
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

                if (type != null) return type;
            }
            catch { }
            try
            {
                int i = name.IndexOf(',');
                string fullName = (i > 0 ? name.Substring(0, i) : name).Trim();

                if (assembly == null) assembly = Assembly.GetCallingAssembly();

                Type type = assembly?.GetType(fullName);
                if (type != null) return type;
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