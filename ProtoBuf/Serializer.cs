using System;
using System.Runtime.Serialization;
using System.Reflection;
using System.IO;

namespace ProtoBuf
{
    /// <summary>
    /// Provides protocol-buffer serialization capability for concrete, attributed types. 
    /// </summary>
    /// <remarks>
    /// Protocol-buffer serialization is a compact binary format, designed to take
    /// advantage of sparse data and knowledge of specific data types; it is also
    /// extensible, allowing a type to be deserialized / merged even if some data is
    /// not recognised.
    /// </remarks>
    public static class Serializer
    {
        internal static void VerifyBytesWritten(int expected, int actual)
        {
            if (actual != expected) throw new SerializationException(string.Format(
                "Wrote {0} bytes, but expected to write {1}.", actual, expected));
        }
        internal static bool IsEntityType(Type type)
        {
            return type.IsClass && !type.IsAbstract
                    && type != typeof(string) && !type.IsArray
                    && type.GetConstructor(Type.EmptyTypes) != null
                    && AttributeUtils.GetAttribute<DataContractAttribute>(type) != null;
        }
        internal static int GetTag(PropertyInfo property)
        {
            DataMemberAttribute dm = AttributeUtils.GetAttribute<DataMemberAttribute>(property);
            return dm == null ? -1 : dm.Order;
        }
        /// <summary>
        /// Creates a new instance from a protocol-buffer stream
        /// </summary>
        /// <typeparam name="T">The type to be created.</typeparam>
        /// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
        /// <returns>A new, initialized instance.</returns>
        public static T Deserialize<T>(Stream source) where T : class, new()
        {
            T instance = new T();
            Serializer<T>.Deserialize(instance, source);
            return instance;
        }
        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance.
        /// </summary>
        /// <typeparam name="T">The type being merged.</typeparam>
        /// <param name="instance">The existing instance to be modified (cannot be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        public static void Merge<T>(T instance, Stream source) where T : class, new()
        {
            Serializer<T>.Deserialize(instance, source);
        }
        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
        /// <param name="destination">The destination stream to write to.</param>
        public static void Serialize<T>(T instance, Stream destination) where T : class, new()
        {
            Serializer<T>.Serialize(instance, destination);
        }

        const string PROTO_BINARY_FIELD = "proto";
        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied SerializationInfo.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
        /// <param name="info">The destination SerializationInfo to write to.</param>
        public static void Serialize<T>(T instance, SerializationInfo info) where T : class, ISerializable, new()
        {
            // note: also tried byte[]... it doesn't perform hugely well with either (compared to regular serialization)
            if (info == null) throw new ArgumentNullException("info");
            using(MemoryStream ms = new MemoryStream())
            {
                Serialize<T>(instance, ms);
                string s = Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
                info.AddValue(PROTO_BINARY_FIELD, s);
            }
        }
        /// <summary>
        /// Applies a protocol-buffer from a SerializationInfo to an existing instance.
        /// </summary>
        /// <typeparam name="T">The type being merged.</typeparam>
        /// <param name="instance">The existing instance to be modified (cannot be null).</param>
        /// <param name="info">The SerializationInfo containing the data to apply to the instance (cannot be null).</param>
        public static void Merge<T>(T instance, SerializationInfo info) where T : class, ISerializable, new()
        {
            // note: also tried byte[]... it doesn't perform hugely well with either (compared to regular serialization)
            if (info == null) throw new ArgumentNullException("info");
            string s = info.GetString(PROTO_BINARY_FIELD);
            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(s)))
            {
                Merge<T>(instance, ms);
            }
        }
        /// <summary>
        /// Suggest a .proto definition for the given type
        /// </summary>
        /// <typeparam name="T">The type to generate a .proto definition for</typeparam>
        /// <returns>The .proto definition as a string</returns>
        public static string GetProto<T>() where T : class, new()
        {
            return Serializer<T>.GetProto();
        }
    }
}
