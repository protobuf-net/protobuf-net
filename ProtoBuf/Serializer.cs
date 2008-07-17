using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;

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
                    && (AttributeUtils.GetAttribute<ProtoContractAttribute>(type) != null
                        || AttributeUtils.GetAttribute<DataContractAttribute>(type) != null
                        || AttributeUtils.GetAttribute<XmlTypeAttribute>(type) != null);
        }
        /// <summary>
        /// Supports various different property metadata patterns:
        /// [ProtoMember] is the most specific, allowing the data-format to be set.
        /// [DataMember], [XmlElement] are supported for compatibility.
        /// In any event, there must be a unique positive Tag/Order.
        /// </summary>
        internal static bool TryGetTag(PropertyInfo property, out int tag, out string name, out DataFormat format, out bool isRequired)
        {
            name = property.Name;
            format = DataFormat.Default;
            tag = -1;
            isRequired = false;
            ProtoMemberAttribute pm = AttributeUtils.GetAttribute<ProtoMemberAttribute>(property);
            if (pm != null) {
                format = pm.DataFormat;
                if(!string.IsNullOrEmpty(pm.Name)) name = pm.Name;
                tag = pm.Tag;
                isRequired = pm.IsRequired;
                return tag > 0;
            }

            DataMemberAttribute dm = AttributeUtils.GetAttribute<DataMemberAttribute>(property);
            if (dm != null)
            {
                if (!string.IsNullOrEmpty(dm.Name)) name = dm.Name;
                tag = dm.Order;
                isRequired = dm.IsRequired;
                return tag > 0;
            }
            
            XmlElementAttribute xe = AttributeUtils.GetAttribute<XmlElementAttribute>(property);
            if (xe != null)
            {
                if (!string.IsNullOrEmpty(xe.ElementName)) name = xe.ElementName;
                tag = xe.Order;
                return tag > 0;
            }

            return false;
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
        /// Create a deep clone of the supplied instance; any sub-items are also cloned.
        /// </summary>
        /// <typeparam name="T">The type being cloned.</typeparam>
        /// <param name="instance">The existing instance to be cloned (cannot be null).</param>
        /// <returns>A new copy, cloned from the supplied instance.</returns>
        public static T DeepClone<T>(T instance) where T : class, new()
        {
            if (instance == null) throw new ArgumentNullException("instance");
            using (MemoryStream ms = new MemoryStream())
            {
                Serialize<T>(instance, ms);
                ms.Position = 0;
                return Deserialize<T>(ms);
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
