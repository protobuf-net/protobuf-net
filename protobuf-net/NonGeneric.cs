using System;
using System.IO;
using System.Reflection;
using ProtoBuf.Property;

namespace ProtoBuf
{
    public static partial class Serializer
    {
        /// <summary>
        /// Provides non-generic, reflection-based access to Serializer functionality
        /// </summary>
        public static class NonGeneric
        {
            /// <summary>
            /// Writes a protocol-buffer representation of the given instance to the supplied stream.
            /// </summary>
            /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
            /// <param name="style">How to encode the length prefix.</param>
            /// <param name="destination">The destination stream to write to.</param>
            /// <param name="tag">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
            public static void SerializeWithLengthPrefix(Stream destination, object instance, PrefixStyle style, int tag)
            {
                if (destination == null) throw new ArgumentNullException("destination");
                if (instance == null) return; // nothing to do
                foreach (MethodInfo method in typeof(Serializer).GetMethods(
                    BindingFlags.Static | BindingFlags.Public))
                {
                    ParameterInfo[] p;
                    if (method.Name == "SerializeWithLengthPrefix" && method.IsGenericMethod)
                    {
                        MethodInfo genericMethod = method.MakeGenericMethod(instance.GetType());
                        if((p = genericMethod.GetParameters()).Length == 4
                            && p[0].ParameterType == typeof(Stream)
                            && p[2].ParameterType == typeof(PrefixStyle)
                            && p[3].ParameterType == typeof(int))
                        {
                            genericMethod.Invoke(
                               null, new object[] { destination, instance, style, tag });
                            return;
                        }                        
                    }
                }
                throw new ProtoException("Unable to resolve SerializeWithLengthPrefix method");
            }

            /// <summary>
            /// Can the given type be meaningfully with protobuf-net?
            /// </summary>
            public static bool CanSerialize(Type type)
            {
                if (type == null) throw new ArgumentNullException("type");
                if (type.IsValueType) return false;

                // serialize as item?
                if (Serializer.IsEntityType(type)) return true;

                // serialize as list?
                bool enumOnly;
                Type itemType = PropertyFactory.GetListType(type, out enumOnly);
                if (itemType != null
                    && (!enumOnly || Serializer.HasAddMethod(type, itemType))
                    && Serializer.IsEntityType(itemType)) return true;
                return false;
            }

            /// <summary>
            /// Writes a protocol-buffer representation of the given instance to the supplied stream.
            /// </summary>
            /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
            /// <param name="destination">The destination stream to write to.</param>
            public static void Serialize(Stream destination, object instance)
            {
                if(destination == null) throw new ArgumentNullException("destination");
                if(instance == null) return; // nothing to do
                foreach (MethodInfo method in typeof(Serializer).GetMethods(
                    BindingFlags.Static | BindingFlags.Public))
                {
                    ParameterInfo[] p;
                    if (method.Name == "Serialize" && method.IsGenericMethod
                        && (p = method.GetParameters()).Length == 2
                        && p[0].ParameterType == typeof(Stream))
                    {
                        method.MakeGenericMethod(instance.GetType()).Invoke(
                            null, new object[] { destination, instance });
                        return;
                    }
                }
                throw new ProtoException("Unable to resolve Serialize method");
            }

            /// <summary>
            /// Deserialize object of unknown types from in input stream.
            /// </summary>
            /// <param name="source">The input stream.</param>
            /// <param name="style">The prefix style used to encode the lengths.</param>
            /// <param name="typeReader">The caller must provide a mechanism to resolve a Type from
            /// the tags encountered in the stream. If the delegate returns null, then the instance
            /// is skipped - otherwise, the object is deserialized according to type.</param>
            /// <param name="item">The deserialized instance, or null if the stream terminated.</param>
            /// <returns>True if an object was idenfified; false if the stream terminated. Note
            /// that unexpected types are skipped.</returns>
            public static bool TryDeserializeWithLengthPrefix(Stream source, PrefixStyle style,
                Getter<int,Type> typeReader, out object item)
            {
                uint len;
                Type itemType = null;
                Getter<int, bool> processField = null;
                if(typeReader != null) processField  = delegate(int checkTag)
                {
                    itemType = typeReader(checkTag);
                    return itemType != null;
                };
                if(!Serializer.TryReadPrefixLength(source, style, 1, out len, processField))
                {
                    item = null;
                    return false;
                }

                if (len == uint.MaxValue)
                {
                    item = NonGeneric.Deserialize(itemType, source);
                }
                else
                {
                    using (SubStream subStream = new SubStream(source, len, false))
                    {
                        item = NonGeneric.Deserialize(itemType, subStream);
                    }
                }
                return true;
            }

            /// <summary>
            /// Creates a new instance from a protocol-buffer stream
            /// </summary>
            /// <param name="type">The type to be created.</param>
            /// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
            /// <returns>A new, initialized instance.</returns>
            public static object Deserialize(Type type, Stream source)
            {
                if (type == null) throw new ArgumentNullException("type");
                if (source == null) throw new ArgumentNullException("source");
                if (type.IsByRef) type = type.GetElementType();

                foreach (MethodInfo method in typeof(Serializer).GetMethods(
                    BindingFlags.Static | BindingFlags.Public))
                {
                    ParameterInfo[] p;
                    if (method.Name == "Deserialize" && method.IsGenericMethod
                        && (p = method.GetParameters()).Length == 1
                        && p[0].ParameterType == typeof(Stream))
                    {
                        return method.MakeGenericMethod(type).Invoke(
                            null, new object[] { source});
                    }
                }
                throw new ProtoException("Unable to resolve Deserialize method");
            }

            /// <summary>
            /// Create a deep clone of the supplied instance; any sub-items are also cloned.
            /// </summary>
            /// <param name="instance">The existing instance to be cloned.</param>
            /// <returns>A new copy, cloned from the supplied instance.</returns>
            public static object DeepClone(object instance)
            {
                if (instance == null) return null; // nothing to do
                foreach (MethodInfo method in typeof(Serializer).GetMethods(
                    BindingFlags.Static | BindingFlags.Public))
                {
                    if (method.Name == "DeepClone" && method.IsGenericMethod
                        && method.GetParameters().Length == 1)
                    {
                        return method.MakeGenericMethod(instance.GetType()).Invoke(
                            null, new object[] { instance });
                    }
                }
                throw new ProtoException("Unable to resolve DeepClone method");
            }
        }
    }
}
