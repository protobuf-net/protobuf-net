using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
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
        public static T Deserialize<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(Stream source)
        {
            using var state = ProtoReader.State.Create(source, RuntimeTypeModel.Default);
            return state.DeserializeRootImpl<T>();
        }

        /// <summary>
        /// Creates a new instance from a protocol-buffer stream
        /// </summary>
        /// <typeparam name="T">The type to be created.</typeparam>
        /// <returns>A new, initialized instance.</returns>
        public static T Deserialize<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(Stream source, T value, SerializationContext context, long length = ProtoReader.TO_EOF)
        {
            using var state = ProtoReader.State.Create(source, RuntimeTypeModel.Default, context, length);
            return state.DeserializeRootImpl<T>(value);
        }

        /// <summary>
        /// Creates a new instance from a protocol-buffer stream
        /// </summary>
        /// <typeparam name="T">The type to be created.</typeparam>
        /// <returns>A new, initialized instance.</returns>
        public static T Deserialize<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(Stream source, T value = default, object userState = default, long length = ProtoReader.TO_EOF)
        {
            using var state = ProtoReader.State.Create(source, RuntimeTypeModel.Default, userState, length);
            return state.DeserializeRootImpl<T>(value);
        }

        /// <summary>
		/// Creates a new instance from a protocol-buffer stream
		/// </summary>
		/// <param name="type">The type to be created.</param>
		/// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
		/// <returns>A new, initialized instance.</returns>
        public static object Deserialize([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type, Stream source)
        {
            using var state = ProtoReader.State.Create(source, RuntimeTypeModel.Default, null, ProtoReader.TO_EOF);
            return state.DeserializeRootFallback(null, type);
        }

        /// <summary>
        /// Creates a new instance from a protocol-buffer stream
        /// </summary>
        public static T Deserialize<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(ReadOnlyMemory<byte> source, T value = default, object userState = null)
            => RuntimeTypeModel.Default.Deserialize<T>(source, value, userState);

        /// <summary>
        /// Creates a new instance from a protocol-buffer stream
        /// </summary>
        public static T Deserialize<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(ReadOnlySequence<byte> source, T value = default, object userState = null)
            => RuntimeTypeModel.Default.Deserialize<T>(source, value, userState);

        /// <summary>
        /// Creates a new instance from a protocol-buffer stream
        /// </summary>
        public static T Deserialize<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(ReadOnlySpan<byte> source, T value = default, object userState = null)
            => RuntimeTypeModel.Default.Deserialize<T>(source, value, userState);
    }
}
