using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.IO;

namespace ProtoBuf
{
    partial class Serializer
    {
        /// <summary>
        /// Creates a new instance from a protocol-buffer stream
        /// </summary>
        /// <typeparam name="T">The type to be created.</typeparam>
        /// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
        /// <returns>A new, initialized instance.</returns>
        public static T Deserialize<T>(Stream source)
        {
            using var state = ProtoReader.State.Create(source, RuntimeTypeModel.Default);
            return state.DeserializeRootImpl<T>();
        }

        /// <summary>
		/// Creates a new instance from a protocol-buffer stream
		/// </summary>
		/// <param name="type">The type to be created.</param>
		/// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
		/// <returns>A new, initialized instance.</returns>
        public static object Deserialize(Type type, Stream source)
        {
            using var state = ProtoReader.State.Create(source, RuntimeTypeModel.Default, null, ProtoReader.TO_EOF);
            return state.DeserializeRootFallback(null, type);
        }

        /// <summary>
        /// Writes a protocol-buffer representation of the given type from the supplied buffer.
        /// </summary>
        public static T Deserialize<T>(ReadOnlyMemory<byte> source, T value = default, SerializationContext context = null)
        {
            using var state = ProtoReader.State.Create(source, RuntimeTypeModel.Default, context);
            return state.DeserializeRootImpl<T>(value);
        }

        /// <summary>
        /// Writes a protocol-buffer representation of the given type from the supplied buffer.
        /// </summary>
        public static T Deserialize<T>(ReadOnlySequence<byte> source, T value = default, SerializationContext context = null)
        {
            using var state = ProtoReader.State.Create(source, RuntimeTypeModel.Default, context);
            return state.DeserializeRootImpl<T>(value);
        }
    }
}
