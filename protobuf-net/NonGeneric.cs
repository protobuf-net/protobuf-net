using System;
using System.IO;
using System.Reflection;

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
            /// Creates a new instance from a protocol-buffer stream
            /// </summary>
            /// <param name="type">The type to be created.</param>
            /// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
            /// <returns>A new, initialized instance.</returns>
            public static object Deserialize(Type type, Stream source)
            {
                if (type == null) throw new ArgumentNullException("type");
                if (source == null) throw new ArgumentNullException("source");
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
                    ParameterInfo[] p;
                    if (method.Name == "DeepClone" && method.IsGenericMethod
                        && (p = method.GetParameters()).Length == 1)
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
