
using ProtoBuf.Meta;
using System.IO;
using System;
namespace ProtoBuf
{
    public static partial class Serializer
    {
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
                return instance == null ? null : RuntimeTypeModel.Default.DeepClone(instance);
            }

            /// <summary>
            /// Writes a protocol-buffer representation of the given instance to the supplied stream.
            /// </summary>
            /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
            /// <param name="dest">The destination stream to write to.</param>
            public static void Serialize(Stream dest, object instance)
            {
                if (instance != null)
                {
                    RuntimeTypeModel.Default.Serialize(dest, instance);
                }
            }

            /// <summary>
            /// Creates a new instance from a protocol-buffer stream
            /// </summary>
            /// <param name="type">The type to be created.</param>
            /// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
            /// <returns>A new, initialized instance.</returns>
            public static object Deserialize(Type type, Stream source)
            {
                return RuntimeTypeModel.Default.Deserialize(source, null, type);
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
                throw new NotImplementedException();//TODO: NotImplementedException
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
            public static bool TryDeserializeWithLengthPrefix(Stream source, PrefixStyle style, TypeResolver resolver, out object value)
            {
                throw new NotImplementedException();//TODO: NotImplementedException
            }
            /// <summary>
            /// Indicates whether the supplied type is explicitly modelled by the model
            /// </summary>
            public static bool CanSerialize(Type type)
            {
                return RuntimeTypeModel.Default.IsDefined(type);
            }
            
        }
        /// <summary>
        /// Maps a field-number to a type
        /// </summary>
        public delegate Type TypeResolver(int fieldNumber);
    }
}
