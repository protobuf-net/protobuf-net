
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
            public static void SerializeWithLengthPrefix(Stream destination, object instance, PrefixStyle style, int tag)
            {
                throw new NotImplementedException();
            }
            public static bool TryDeserializeWithLengthPrefix(Stream source, PrefixStyle style, TypeResolver typeReader, out object item)
            {
                throw new NotImplementedException();
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
