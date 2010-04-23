using System;
using System.IO;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// Provides protobuf serialization support for a number of types
    /// </summary>
    public abstract class TypeModel
    {
        public void Serialize(Stream dest, object value)
        {
            if (value == null) throw new ArgumentNullException("value");
            Type type = value.GetType();
            int key = GetKey(type);
            if (key < 0)
            {   // TODO: add special cases here, IEnumerable<T> etc
                ThrowUnexpectedType(type);
            }
            using (ProtoWriter writer = new ProtoWriter(dest, this))
            {
                Serialize(key, value, writer);
                writer.Close();
            }
        }
        public object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int fieldNumber)
        {
            return DeserializeWithLengthPrefix(source, value, type, style, fieldNumber, null);
        }
        public object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int expectedField, Serializer.TypeResolver resolver)
        {
            bool skip;
            int len;
            do
            {
                int actualField;
                bool expectPrefix = expectedField > 0 || resolver != null;
                len = ProtoReader.ReadLengthPrefix(source, expectPrefix, style, out actualField);
                if (len < 0) return value;

                if (expectedField == 0 && type == null && resolver != null)
                {
                    type = resolver(actualField);
                    skip = type == null;
                }
                else { skip = expectedField != actualField; }

                if (skip)
                {
                    if (len == int.MaxValue) throw new InvalidOperationException();
                    ProtoReader.Seek(source, len, null);
                }
            } while (skip);

            int key = GetKey(type);
            if (key < 0) throw new InvalidOperationException();
            using (ProtoReader reader = new ProtoReader(source, this, len))
            {
                return Deserialize(key, value, reader);
            }

        }
        public void SerializeWithLengthPrefix(Stream dest, object value, Type type, PrefixStyle style, int fieldNumber)
        {
            if (type == null)
            {
                if(value == null) throw new ArgumentNullException("value");
                type = value.GetType();
            }
            int key = GetKey(type);
            using (ProtoWriter writer = new ProtoWriter(dest, this))
            {
                switch (style)
                {
                    case PrefixStyle.None:
                        Serialize(key, value, writer);
                        break;
                    case PrefixStyle.Base128:
                    case PrefixStyle.Fixed32:
                    case PrefixStyle.Fixed32BigEndian:
                        ProtoWriter.WriteObject(value, key, writer, style, fieldNumber);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("style");
                }
                writer.Close();
            }
        }
        public object Deserialize(Stream source, object value, Type type)
        {
            if (type == null)
            {
                if (value == null)
                {
                    throw new ArgumentNullException("type");
                } else {
                    type = value.GetType();
                }
            }
#if !NO_GENERICS
            type = Nullable.GetUnderlyingType(type) ?? type;
#endif
            int key = GetKey(type);
            using (ProtoReader reader = new ProtoReader(source, this))
            {
                return Deserialize(key, value, reader);
            }
        }
#if !NO_RUNTIME
        public static RuntimeTypeModel Create()
        {
            return new RuntimeTypeModel(false);
        }
#endif

        /// <summary>
        /// Applies common proxy scenarios, resolving the actual type to consider
        /// </summary>
        protected internal static Type ResolveProxies(Type type)
        {
            if (type == null) return null;
            
            // NHibernate
            if (type.GetInterface("NHibernate.Proxy.INHibernateProxy", false) != null) return type.BaseType;

            return null;
        }


        protected internal int GetKey(Type type)
        {
            int key = GetKeyImpl(type);
            if (key < 0)
            {
                type = ResolveProxies(type);
                if (type != null) key = GetKeyImpl(type);
            }
            return key;
        }

        protected abstract int GetKeyImpl(Type type);
        protected internal abstract void Serialize(int key, object value, ProtoWriter dest);
        protected internal abstract object Deserialize(int key, object value, ProtoReader source);
        
        //internal ProtoSerializer Create(IProtoSerializer head)
        //{
        //    return new RuntimeSerializer(head, this);
        //}
        //internal ProtoSerializer Compile

        protected internal enum CallbackType
        {
            BeforeSerialize, AfterSerialize, BeforeDeserialize, AfterDeserialize
        }

        public object DeepClone(object value)
        {
            if (value == null) return null;
            int key = GetKey(value.GetType());
            
            using (MemoryStream ms = new MemoryStream())
            {
                using(ProtoWriter writer = new ProtoWriter(ms, this))
                {
                    Serialize(key, value, writer);
                    writer.Close();
                }
                ms.Position = 0;
                using (ProtoReader reader = new ProtoReader(ms, this))
                {
                    return Deserialize(key, null, reader);
                }
            }
        }
        protected internal static void ThrowUnexpectedSubtype(Type expected, Type actual)
        {
            if (expected != TypeModel.ResolveProxies(actual))
            {
                throw new InvalidOperationException("Unexpected sub-type: " + actual.FullName);
            }
        }
        protected static void ThrowUnexpectedType(Type type)
        {
            throw new InvalidOperationException("Type is not expected, and no contract can be inferred: " + type.FullName);
        }
        protected internal static void ThrowCannotCreateInstance(Type type)
        {
            throw new InvalidOperationException("Cannot create an instance of type; no suitable constructor found: " + type.FullName);
        }
    }

}
