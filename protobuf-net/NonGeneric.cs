
using ProtoBuf.Meta;
using System.IO;
using System;
namespace ProtoBuf
{
    public static partial class Serializer
    {
        public static class NonGeneric
        {
            public static object DeepClone(object instance)
            {
                return instance == null ? null : RuntimeTypeModel.Default.DeepClone(instance);
            }

            public static void Serialize(Stream dest, object instance)
            {
                if (instance != null)
                {
                    RuntimeTypeModel.Default.Serialize(dest, instance);
                }
            }

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
            public static bool CanSerialize(Type type)
            {
                throw new NotImplementedException();
            }
            
        }
        /// <summary>
        /// Maps a field-number to a type
        /// </summary>
        public delegate Type TypeResolver(int fieldNumber);
    }
}
