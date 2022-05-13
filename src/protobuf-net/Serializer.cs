using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;

namespace ProtoBuf
{
    /// <summary>
    /// Provides protocol-buffer serialization capability for concrete, attributed types. This
    /// is a *default* model, but custom serializer models are also supported.
    /// </summary>
    /// <remarks>
    /// Protocol-buffer serialization is a compact binary format, designed to take
    /// advantage of sparse data and knowledge of specific data types; it is also
    /// extensible, allowing a type to be deserialized / merged even if some data is
    /// not recognised.
    /// </remarks>
    public static partial class Serializer
    {
        /// <summary>
        /// Suggest a .proto definition for the given type
        /// </summary>
        /// <typeparam name="T">The type to generate a .proto definition for</typeparam>
        /// <returns>The .proto definition as a string</returns>
        public static string GetProto<T>()
            => RuntimeTypeModel.Default.GetSchema(typeof(T), ProtoSyntax.Default);

        /// <summary>
        /// Suggest a .proto definition for the given type
        /// </summary>
        /// <typeparam name="T">The type to generate a .proto definition for</typeparam>
        /// <returns>The .proto definition as a string</returns>
        public static string GetProto<T>(ProtoSyntax syntax)
            => RuntimeTypeModel.Default.GetSchema(typeof(T), syntax);

        /// <summary>
        /// Suggest a .proto definition for the given type
        /// </summary>
        /// <returns>The .proto definition as a string</returns>
        public static string GetProto(SchemaGenerationOptions options)
            => RuntimeTypeModel.Default.GetSchema(options);

        /// <summary>
        /// Create a deep clone of the supplied instance; any sub-items are also cloned.
        /// </summary>
        public static T DeepClone<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(T instance, SerializationContext context)
            => RuntimeTypeModel.Default.DeepClone<T>(instance, context);

        /// <summary>
        /// Create a deep clone of the supplied instance; any sub-items are also cloned.
        /// </summary>
        public static T DeepClone<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(T instance, object userState = null)
            => RuntimeTypeModel.Default.DeepClone<T>(instance, userState);

        /// <summary>
        /// Calculates the length of a protocol-buffer payload for an item
        /// </summary>
        public static MeasureState<T> Measure<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(T value, object userState = null, long abortAfter = -1)
            => RuntimeTypeModel.Default.Measure<T>(value, userState, abortAfter);

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance.
        /// </summary>
        /// <typeparam name="T">The type being merged.</typeparam>
        /// <param name="instance">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public static T Merge<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(Stream source, T instance)
        {
            using var state = ProtoReader.State.Create(source, RuntimeTypeModel.Default);
            return state.DeserializeRootImpl<T>(instance);
        }

        /// <summary>
        /// Serializes a given instance and deserializes it as a different type;
        /// this can be used to translate between wire-compatible objects (where
        /// two .NET types represent the same data), or to promote/demote a type
        /// through an inheritance hierarchy.
        /// </summary>
        /// <remarks>No assumption of compatibility is made between the types.</remarks>
        /// <typeparam name="TFrom">The type of the object being copied.</typeparam>
        /// <typeparam name="TTo">The type of the new object to be created.</typeparam>
        /// <param name="instance">The existing instance to use as a template.</param>
        /// <returns>A new instane of type TNewType, with the data from TOldType.</returns>
        public static TTo ChangeType<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] TFrom, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] TTo>(TFrom instance)
        {
            using var ms = new MemoryStream();
            Serialize<TFrom>(ms, instance);
            ms.Position = 0;
            return Deserialize<TTo>(ms);
        }

        /// <summary>
        /// Applies a protocol-buffer from an XmlReader to an existing instance.
        /// </summary>
        /// <typeparam name="T">The type being merged.</typeparam>
        /// <param name="instance">The existing instance to be modified (cannot be null).</param>
        /// <param name="reader">The XmlReader containing the data to apply to the instance (cannot be null).</param>
        public static void Merge<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(System.Xml.XmlReader reader, T instance) where T : System.Xml.Serialization.IXmlSerializable
        {
            if (reader is null) throw new ArgumentNullException(nameof(reader));
            if (instance is null) throw new ArgumentNullException(nameof(instance));
            const int LEN = 4096;
            byte[] buffer = new byte[LEN];
            int read;
            using MemoryStream ms = new MemoryStream();
            int depth = reader.Depth;
            while (reader.Read() && reader.Depth > depth)
            {
                if (reader.NodeType == System.Xml.XmlNodeType.Text)
                {
                    while ((read = reader.ReadContentAsBase64(buffer, 0, LEN)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                    if (reader.Depth <= depth) break;
                }
            }
            ms.Position = 0;
            Serializer.Merge(ms, instance);
        }

        private const string ProtoBinaryField = "proto";

        /// <summary>
        /// Applies a protocol-buffer from a SerializationInfo to an existing instance.
        /// </summary>
        /// <typeparam name="T">The type being merged.</typeparam>
        /// <param name="instance">The existing instance to be modified (cannot be null).</param>
        /// <param name="info">The SerializationInfo containing the data to apply to the instance (cannot be null).</param>
        public static void Merge<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(SerializationInfo info, T instance) where T : class, ISerializable
        {
            Merge<T>(info, new StreamingContext(StreamingContextStates.Persistence), instance);
        }
        /// <summary>
        /// Applies a protocol-buffer from a SerializationInfo to an existing instance.
        /// </summary>
        /// <typeparam name="T">The type being merged.</typeparam>
        /// <param name="instance">The existing instance to be modified (cannot be null).</param>
        /// <param name="info">The SerializationInfo containing the data to apply to the instance (cannot be null).</param>
        /// <param name="context">Additional information about this serialization operation.</param>
        public static void Merge<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(SerializationInfo info, StreamingContext context, T instance)
            where T : class, ISerializable
        {
            // note: also tried byte[]... it doesn't perform hugely well with either (compared to regular serialization)
            if (info is null) throw new ArgumentNullException(nameof(info));
            if (instance is null) throw new ArgumentNullException(nameof(instance));
            if (instance.GetType() != typeof(T)) throw new ArgumentException("Incorrect type", nameof(instance));

            byte[] buffer = (byte[])info.GetValue(ProtoBinaryField, typeof(byte[]));
            using MemoryStream ms = new MemoryStream(buffer);
            T result = RuntimeTypeModel.Default.Deserialize<T>(ms, instance, context.Context);
            if (!ReferenceEquals(result, instance))
            {
                throw new ProtoException("Deserialization changed the instance; cannot succeed.");
            }
        }

        /// <summary>
        /// Precompiles the serializer for a given type.
        /// </summary>
        public static void PrepareSerializer<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => RuntimeTypeModel.Default[typeof(T)].CompileInPlace();

        /// <summary>
        /// Creates a new IFormatter that uses protocol-buffer [de]serialization.
        /// </summary>
        /// <typeparam name="T">The type of object to be [de]deserialized by the formatter.</typeparam>
        /// <returns>A new IFormatter to be used during [de]serialization.</returns>
        public static System.Runtime.Serialization.IFormatter CreateFormatter<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
        {
            return RuntimeTypeModel.Default.CreateFormatter(typeof(T));
        }

        /// <summary>
        /// Reads a sequence of consecutive length-prefixed items from a stream, using
        /// either base-128 or fixed-length prefixes. Base-128 prefixes with a tag
        /// are directly comparable to serializing multiple items in succession
        /// (use the <see cref="ListItemTag"/> tag to emulate the implicit behavior
        /// when serializing a list/array). When a tag is
        /// specified, any records with different tags are silently omitted. The
        /// tag is ignored. The tag is ignored for fixed-length prefixes.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize.</typeparam>
        /// <param name="source">The binary stream containing the serialized records.</param>
        /// <param name="style">The prefix style used in the data.</param>
        /// <param name="fieldNumber">The tag of records to return (if non-positive, then no tag is
        /// expected and all records are returned).</param>
        /// <returns>The sequence of deserialized objects.</returns>
        public static IEnumerable<T> DeserializeItems<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(Stream source, PrefixStyle style, int fieldNumber)
        {
            return RuntimeTypeModel.Default.DeserializeItems<T>(source, style, fieldNumber);
        }

        /// <summary>
        /// Creates a new instance from a protocol-buffer stream that has a length-prefix
        /// on data (to assist with network IO).
        /// </summary>
        /// <typeparam name="T">The type to be created.</typeparam>
        /// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <returns>A new, initialized instance.</returns>
        public static T DeserializeWithLengthPrefix<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(Stream source, PrefixStyle style)
        {
            return DeserializeWithLengthPrefix<T>(source, style, 0);
        }

        /// <summary>
        /// Creates a new instance from a protocol-buffer stream that has a length-prefix
        /// on data (to assist with network IO).
        /// </summary>
        /// <typeparam name="T">The type to be created.</typeparam>
        /// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <param name="fieldNumber">The expected tag of the item (only used with base-128 prefix style).</param>
        /// <returns>A new, initialized instance.</returns>
        public static T DeserializeWithLengthPrefix<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(Stream source, PrefixStyle style, int fieldNumber)
        {
            return (T)RuntimeTypeModel.Default.DeserializeWithLengthPrefix(source, null, typeof(T), style, fieldNumber);
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance, using length-prefixed
        /// data - useful with network IO.
        /// </summary>
        /// <typeparam name="T">The type being merged.</typeparam>
        /// <param name="instance">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public static T MergeWithLengthPrefix<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(Stream source, T instance, PrefixStyle style)
        {
            return (T)RuntimeTypeModel.Default.DeserializeWithLengthPrefix(source, instance, typeof(T), style, 0);
        }

        /// <summary>Indicates the number of bytes expected for the next message.</summary>
        /// <param name="source">The stream containing the data to investigate for a length.</param>
        /// <param name="style">The algorithm used to encode the length.</param>
        /// <param name="length">The length of the message, if it could be identified.</param>
        /// <returns>True if a length could be obtained, false otherwise.</returns>
        public static bool TryReadLengthPrefix(Stream source, PrefixStyle style, out int length)
        {
            length = ProtoReader.ReadLengthPrefix(source, false, style, out int _, out int bytesRead);
            return bytesRead > 0;
        }

        /// <summary>Indicates the number of bytes expected for the next message.</summary>
        /// <param name="buffer">The buffer containing the data to investigate for a length.</param>
        /// <param name="index">The offset of the first byte to read from the buffer.</param>
        /// <param name="count">The number of bytes to read from the buffer.</param>
        /// <param name="style">The algorithm used to encode the length.</param>
        /// <param name="length">The length of the message, if it could be identified.</param>
        /// <returns>True if a length could be obtained, false otherwise.</returns>
        public static bool TryReadLengthPrefix(byte[] buffer, int index, int count, PrefixStyle style, out int length)
        {
            using Stream source = new MemoryStream(buffer, index, count);
            return TryReadLengthPrefix(source, style, out length);
        }

        /// <summary>
        /// The field number that is used as a default when serializing/deserializing a list of objects.
        /// The data is treated as repeated message with field number 1.
        /// </summary>
        public const int ListItemTag = TypeModel.ListItemTag;

        /// <summary>
        /// Provides non-generic access to the default serializer.
        /// </summary>
        public static class NonGeneric
        {
            /// <summary>
            /// Create a deep clone of the supplied instance; any sub-items are also cloned.
            /// </summary>
            public static object DeepClone(object instance)
            {
                return instance is null ? null : RuntimeTypeModel.Default.DeepClone(instance);
            }

            /// <summary>
            /// Writes a protocol-buffer representation of the given instance to the supplied stream.
            /// </summary>
            /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
            /// <param name="dest">The destination stream to write to.</param>
            public static void Serialize(Stream dest, object instance)
            {
                if (instance is not null)
                {
                    var state = ProtoWriter.State.Create(dest, RuntimeTypeModel.Default);
                    try
                    {
                        state.Model.SerializeRootFallback(ref state, instance);
                    }
                    finally
                    {
                        state.Dispose();
                    }
                }
            }

            /// <summary>
            /// Creates a new instance from a protocol-buffer stream
            /// </summary>
            /// <param name="type">The type to be created.</param>
            /// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
            /// <returns>A new, initialized instance.</returns>
            public static object Deserialize([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, Stream source)
                => RuntimeTypeModel.Default.Deserialize(type, source);

            /// <summary>
            /// Creates a new instance from a protocol-buffer stream
            /// </summary>
            public static object Deserialize([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, Stream source, object instance = null, object userState = null, long length = ProtoReader.TO_EOF)
                => RuntimeTypeModel.Default.Deserialize(type, source, instance, userState, length);

            /// <summary>
            /// Creates a new instance from a protocol-buffer stream
            /// </summary>
            public static object Deserialize([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, ReadOnlyMemory<byte> source, object instance = null, object userState = null)
                => RuntimeTypeModel.Default.Deserialize(type, source, instance, userState);

            /// <summary>
            /// Creates a new instance from a protocol-buffer stream
            /// </summary>
            public static object Deserialize([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, ReadOnlySequence<byte> source, object instance = null, object userState = null)
                => RuntimeTypeModel.Default.Deserialize(type, source, instance, userState);

            /// <summary>
            /// Creates a new instance from a protocol-buffer stream
            /// </summary>
            public static object Deserialize([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, ReadOnlySpan<byte> source, object instance = null, object userState = null)
                => RuntimeTypeModel.Default.Deserialize(type, source, instance, userState);

            /// <summary>Applies a protocol-buffer stream to an existing instance.</summary>
            /// <param name="instance">The existing instance to be modified (cannot be null).</param>
            /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
            /// <returns>The updated instance</returns>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            public static object Merge(Stream source, object instance)
            {
                if (instance is null) throw new ArgumentNullException(nameof(instance));
                using var state = ProtoReader.State.Create(source, RuntimeTypeModel.Default);
                return state.DeserializeRootFallback(instance, instance.GetType());
            }

            /// <summary>
            /// Writes a protocol-buffer representation of the given instance to the supplied stream,
            /// with a length-prefix. This is useful for socket programming,
            /// as DeserializeWithLengthPrefix/MergeWithLengthPrefix can be used to read the single object back
            /// from an ongoing stream.
            /// </summary>
            /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
            /// <param name="style">How to encode the length prefix.</param>
            /// <param name="destination">The destination stream to write to.</param>
            /// <param name="fieldNumber">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
            public static void SerializeWithLengthPrefix(Stream destination, object instance, PrefixStyle style, int fieldNumber)
            {
                if (instance is null) throw new ArgumentNullException(nameof(instance));
                RuntimeTypeModel.Default.SerializeWithLengthPrefix(destination, instance, instance.GetType(), style, fieldNumber);
            }
            /// <summary>
            /// Applies a protocol-buffer stream to an existing instance (or null), using length-prefixed
            /// data - useful with network IO.
            /// </summary>
            /// <param name="value">The existing instance to be modified (can be null).</param>
            /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
            /// <param name="style">How to encode the length prefix.</param>
            /// <param name="resolver">Used to resolve types on a per-field basis.</param>
            /// <returns>The updated instance; this may be different to the instance argument if
            /// either the original instance was null, or the stream defines a known sub-type of the
            /// original instance.</returns>
            public static bool TryDeserializeWithLengthPrefix(Stream source, PrefixStyle style, ProtoBuf.TypeResolver resolver, out object value)
            {
                value = RuntimeTypeModel.Default.DeserializeWithLengthPrefix(source, null, null, style, 0, resolver);
                return value is not null;
            }

            /// <summary>
            /// Indicates whether the supplied type is explicitly modelled by the model
            /// </summary>
            public static bool CanSerialize(Type type) => RuntimeTypeModel.Default.IsDefined(type);

            /// <summary>
            /// Precompiles the serializer for a given type.
            /// </summary>
            public static void PrepareSerializer([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type)
            {
                RuntimeTypeModel.Default[type].CompileInPlace();
            }
        }

        /// <summary>
        /// Global switches that change the behavior of protobuf-net
        /// </summary>
        public static class GlobalOptions
        {
            /// <summary>
            /// <see cref="RuntimeTypeModel.InferTagFromNameDefault"/>
            /// </summary>
            [Obsolete("Please use RuntimeTypeModel.Default.InferTagFromNameDefault instead (or on a per-model basis)", false)]
            public static bool InferTagFromName
            {
                get { return RuntimeTypeModel.Default.InferTagFromNameDefault; }
                set { RuntimeTypeModel.Default.InferTagFromNameDefault = value; }
            }

            private static ProtoSyntax _defaultSyntax = ProtoSyntax.Proto3;

            /// <summary>
            /// Gets or sets the default .proto syntax to be used
            /// </summary>
            public static ProtoSyntax DefaultSyntax
            {
                get => _defaultSyntax;
                set
                {
                    switch (value)
                    {
                        case ProtoSyntax.Proto2:
                        case ProtoSyntax.Proto3:
                            _defaultSyntax = value;
                            break;
                        default:
                            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(DefaultSyntax));
                            break;
                    }
                }
            }

            internal static ProtoSyntax Normalize(ProtoSyntax syntax) => syntax switch
            {
                ProtoSyntax.Proto2 => syntax,
                ProtoSyntax.Proto3 => syntax,
                _ => DefaultSyntax,
            };
        }

        /// <summary>
        /// Releases any internal buffers that have been reserved for efficiency; this does not affect any serialization
        /// operations; simply: it can be used (optionally) to release the buffers for garbage collection (at the expense
        /// of having to re-allocate a new buffer for the next operation, rather than re-use prior buffers).
        /// </summary>
        [Obsolete("This API is no longer required and may be removed in a future release")]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static void FlushPool() { }


        /// <summary>
        /// Maps a field-number to a type
        /// </summary>
        [Obsolete("Please use ProtoBuf.TypeResolver", true)]
        public delegate Type TypeResolver(int fieldNumber);
    }
}
