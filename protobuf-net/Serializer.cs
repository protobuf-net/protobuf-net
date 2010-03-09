
using ProtoBuf.Meta;
using System;
using System.IO;
using System.Collections.Generic;
namespace ProtoBuf
{
    public static partial class Serializer
    {
        public static string GetProto<T>()
        {
            throw new NotImplementedException();
        }
        public static T DeepClone<T>(T instance)
        {
            return instance == null ? instance : (T)RuntimeTypeModel.Default.DeepClone(instance);
        }
        public static T Merge<T>(Stream source, T instance)
        {
            return (T)RuntimeTypeModel.Default.Deserialize(source, instance, typeof(T));
        }
        public static T Deserialize<T>(Stream source)
        {
            return (T)RuntimeTypeModel.Default.Deserialize(source, null, typeof(T));
        }
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
        /*
        internal static void CheckTagNotInUse(Type type, int tag)
        {
            if (RuntimeTypeModel.Default.IsDefined(type, tag))
            {
                throw new InvalidOperationException();
            }
        }*/

        public static void PrepareSerializer<T>() where T : class
        { RuntimeTypeModel.Default[typeof(T)].CompileInPlace(); }

        public const int ListItemTag = 1;

        public static System.Runtime.Serialization.IFormatter CreateFormatter<T>()
        { throw new NotImplementedException(); }
        public static IEnumerable<T> DeserializeItems<T>(Stream source, PrefixStyle style, int tag)
        { throw new NotImplementedException(); }

        public static T DeserializeWithLengthPrefix<T>(Stream source, PrefixStyle style)
        { throw new NotImplementedException(); }
        public static T DeserializeWithLengthPrefix<T>(Stream source, PrefixStyle style, int tag)
        { throw new NotImplementedException(); }
        
        public static T MergeWithLengthPrefix<T>(Stream source, T instance, PrefixStyle style)
        { throw new NotImplementedException(); }

        public static void SerializeWithLengthPrefix<T>(Stream destination, T instance, PrefixStyle style)
        { throw new NotImplementedException(); }
        public static void SerializeWithLengthPrefix<T>(Stream destination, T instance, PrefixStyle style, int tag)
        { throw new NotImplementedException(); }
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
