
using ProtoBuf.Meta;
using System;
using System.IO;
using System.Collections.Generic;
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
        public static string GetProto<T>()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Create a deep clone of the supplied instance; any sub-items are also cloned.
        /// </summary>
        public static T DeepClone<T>(T instance)
        {
            return instance == null ? instance : (T)RuntimeTypeModel.Default.DeepClone(instance);
        }
        public static T Merge<T>(Stream source, T instance)
        {
            return (T)RuntimeTypeModel.Default.Deserialize(source, instance, typeof(T));
        }
        /// <summary>
        /// Creates a new instance from a protocol-buffer stream
        /// </summary>
        /// <typeparam name="T">The type to be created.</typeparam>
        /// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
        /// <returns>A new, initialized instance.</returns>
        public static T Deserialize<T>(Stream source)
        {
            return (T) RuntimeTypeModel.Default.Deserialize(source, null, typeof(T));
        }
        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream.
        /// </summary>
        /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
        /// <param name="destination">The destination stream to write to.</param>
        public static void Serialize<T>(Stream destination, T instance)
        {
            if(instance != null) {
                RuntimeTypeModel.Default.Serialize(destination, instance);
            }
        }
        public static TTo ChangeType<TFrom,TTo>(TFrom instance)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serialize<TFrom>(ms, instance);
                ms.Position = 0;
                return Deserialize<TTo>(ms);
            }
        }
#if PLAT_BINARYFORMATTER
        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied SerializationInfo.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
        /// <param name="info">The destination SerializationInfo to write to.</param>
        public static void Serialize<T>(System.Runtime.Serialization.SerializationInfo info, T instance) where T : class, System.Runtime.Serialization.ISerializable
        {
            // note: also tried byte[]... it doesn't perform hugely well with either (compared to regular serialization)
            if (info == null) throw new ArgumentNullException("info");
            if (instance == null) throw new ArgumentNullException("instance");
            if (instance.GetType() != typeof(T)) throw new ArgumentException("Incorrect type", "instance");
            using (MemoryStream ms = new MemoryStream())
            {
                Serialize<T>(ms, instance);
                info.AddValue(ProtoBinaryField, ms.ToArray());
            }
        }
#endif
        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied XmlWriter.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
        /// <param name="writer">The destination XmlWriter to write to.</param>
        public static void Serialize<T>(System.Xml.XmlWriter writer, T instance) where T : System.Xml.Serialization.IXmlSerializable
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (instance == null) throw new ArgumentNullException("instance");

            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, instance);
                writer.WriteBase64(ms.GetBuffer(), 0, (int)ms.Length);
            }
        }
        /// <summary>
        /// Applies a protocol-buffer from an XmlReader to an existing instance.
        /// </summary>
        /// <typeparam name="T">The type being merged.</typeparam>
        /// <param name="instance">The existing instance to be modified (cannot be null).</param>
        /// <param name="reader">The XmlReader containing the data to apply to the instance (cannot be null).</param>
        public static void Merge<T>(System.Xml.XmlReader reader, T instance) where T : System.Xml.Serialization.IXmlSerializable
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (instance == null) throw new ArgumentNullException("instance");

            const int LEN = 4096;
            byte[] buffer = new byte[LEN];
            int read;
            using (MemoryStream ms = new MemoryStream())
            {
                while ((read = reader.ReadElementContentAsBase64(buffer, 0, LEN)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                ms.Position = 0;
                Serializer.Merge(ms, instance);
            }
        }


        private const string ProtoBinaryField = "proto";
#if PLAT_BINARYFORMATTER
        /// <summary>
        /// Applies a protocol-buffer from a SerializationInfo to an existing instance.
        /// </summary>
        /// <typeparam name="T">The type being merged.</typeparam>
        /// <param name="instance">The existing instance to be modified (cannot be null).</param>
        /// <param name="info">The SerializationInfo containing the data to apply to the instance (cannot be null).</param>
        public static void Merge<T>(System.Runtime.Serialization.SerializationInfo info, T instance) where T : class, System.Runtime.Serialization.ISerializable
        {
            // note: also tried byte[]... it doesn't perform hugely well with either (compared to regular serialization)
            if (info == null) throw new ArgumentNullException("info");
            if (instance == null) throw new ArgumentNullException("instance");
            if (instance.GetType() != typeof(T)) throw new ArgumentException("Incorrect type", "instance");

            //string s = info.GetString(ProtoBinaryField);
            //using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(s)))
            byte[] buffer = (byte[])info.GetValue(ProtoBinaryField, typeof(byte[]));
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                T result = Merge<T>(ms, instance);
                if (!ReferenceEquals(result, instance))
                {
                    throw new ProtoException("Deserialization changed the instance; cannot succeed.");
                }
            }
        }
#endif

        public static void PrepareSerializer<T>() where T : class
        { 
#if FEAT_COMPILER
            RuntimeTypeModel.Default[typeof(T)].CompileInPlace();
#endif
        }

        /// <summary>
        /// The field number that is used as a default when serializing/deserializing a list of objects.
        /// The data is treated as repeated message with field number 1.
        /// </summary>
        public const int ListItemTag = 1;
#if PLAT_BINARYFORMATTER
        public static System.Runtime.Serialization.IFormatter CreateFormatter<T>()
        { throw new NotImplementedException(); }
#endif
        public static IEnumerable<T> DeserializeItems<T>(Stream source, PrefixStyle style, int fieldNumber)
        { throw new NotImplementedException(); }

        public static T DeserializeWithLengthPrefix<T>(Stream source, PrefixStyle style)
        {
            return DeserializeWithLengthPrefix<T>(source, style, 0);
        }
        public static T DeserializeWithLengthPrefix<T>(Stream source, PrefixStyle style, int fieldNumber)
        {
            return (T)RuntimeTypeModel.Default.DeserializeWithLengthPrefix(source, null, typeof(T), style, fieldNumber);
        }
        
        public static T MergeWithLengthPrefix<T>(Stream source, T instance, PrefixStyle style)
        {
            return (T)RuntimeTypeModel.Default.DeserializeWithLengthPrefix(source, instance, typeof(T), style, 0);
        }

        public static void SerializeWithLengthPrefix<T>(Stream destination, T instance, PrefixStyle style)
        {
            SerializeWithLengthPrefix<T>(destination, instance, style, 0);
        }
        public static void SerializeWithLengthPrefix<T>(Stream destination, T instance, PrefixStyle style, int fieldNumber)
        {
            RuntimeTypeModel.Default.SerializeWithLengthPrefix(destination, instance, typeof(T), style, fieldNumber);
        }
        /// <summary>Indicates the number of bytes expected for the next message.</summary>
        /// <param name="source">The stream containing the data to investigate for a length.</param>
        /// <param name="style">The algorithm used to encode the length.</param>
        /// <param name="length">The length of the message, if it could be identified.</param>
        /// <returns>True if a length could be obtained, false otherwise.</returns>
        public static bool TryReadLengthPrefix(Stream source, PrefixStyle style, out int length)
        {
            throw new NotImplementedException();
            //uint len;
            //bool result;
            //switch (style)
            //{
            //    case PrefixStyle.Fixed32:
            //        result = SerializationContext.TryDecodeUInt32Fixed(source, out len);
            //        break;
            //    case PrefixStyle.Base128:
            //        result = SerializationContext.TryDecodeUInt32(source, out len);
            //        break;
            //    default:
            //        throw new ArgumentOutOfRangeException("style", "Invalid prefix style: " + style);
            //}
            //length = (int)len;
            //return result;
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
            using (Stream source = new MemoryStream(buffer, index, count))
            {
                return TryReadLengthPrefix(source, style, out length);
            }
        }

    }
}
