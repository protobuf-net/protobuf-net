using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace ProtoBuf
{
    abstract class PropertyBase<TEntity, TValue> : IProperty<TEntity> where TEntity : class, new()
    {
        internal delegate TValue Getter(TEntity instance);
        internal delegate void Setter(TEntity instance, TValue value);

        protected static ISerializer<T> GetSerializer<T>(PropertyInfo property)
        {
            ProtoMemberAttribute attrib = AttributeUtils.GetAttribute<ProtoMemberAttribute>(property);
            DataFormat format = attrib == null ? DataFormat.Default : attrib.DataFormat;
            bool signed = format == DataFormat.Default;

            ISerializer<T> result = signed ? SerializerCache<T>.Signed : SerializerCache<T>.Unsigned;

            if (result == null)
            {
                if (Serializer.IsEntityType(typeof(T)))
                {
                    result = (ISerializer<T>)Activator.CreateInstance(
                        typeof(EntitySerializer<>).MakeGenericType(typeof(T)));
                    SimpleSerializers.Set<T>(result);
                }
                else
                {
                    throw new SerializationException(string.Format(
                        "Unable to determine serializer for {0}.{1}",
                        property.DeclaringType, property.Name));
                }
            }
            return result;
        }
        public int Tag { get { return tag; } }
        private readonly int tag;
        private readonly PropertyInfo property;
        public PropertyInfo Property { get { return property; } }
        private readonly Getter getter;
        private readonly Setter setter;
        protected PropertyBase(PropertyInfo property)
        {
            if (property == null) throw new ArgumentNullException("property");
            this.property = property;
            tag = Serializer.GetTag(property);
            if (tag <= 0) throw new ArgumentOutOfRangeException("Positive tag required");

            MethodInfo method;
            if (property.CanRead && (method = property.GetGetMethod(true)) != null)
            {
                getter = (Getter)Delegate.CreateDelegate(typeof(Getter), method);
            }
            if (property.CanWrite && (method = property.GetSetMethod(true)) != null)
            {
                setter = (Setter)Delegate.CreateDelegate(typeof(Setter), method);
            }
        }
        protected TValue GetValue(TEntity instance) { return getter(instance); }
        protected void SetValue(TEntity instance, TValue value) { setter(instance, value); }
        public abstract void Deserialize(TEntity instance, SerializationContext context);
        public abstract int Serialize(TEntity instance, SerializationContext context);
        public abstract int GetLength(TEntity instance, SerializationContext context);
        public abstract WireType WireType { get; }
        public abstract string DefinedType { get; }

        private int Prefix { get { return (Tag << 3) | ((int)WireType & 7); } }
        protected int GetPrefixLength()
        {
            return Int32VariantSerializer.GetLength(Prefix);
        }
        protected int WritePrefixToStream(SerializationContext context)
        {
            return Int32VariantSerializer.WriteToStream(Prefix, context);
        }

        protected int GetLength<TActualValue>(TActualValue value, ISerializer<TActualValue> serializer, SerializationContext context)
        {
            int len = serializer.GetLength(value, context);
            if (len == 0) return 0;
            return GetPrefixLength() + len;
        }
        protected int Serialize<TActualValue>(TActualValue value, ISerializer<TActualValue> serializer, SerializationContext context)
        {
            //TODO: add a "ShouldSerialize" instead of this
            int expectedLen = serializer.GetLength(value, context);
            if (expectedLen == 0) return 0;
            int prefixLen = WritePrefixToStream(context),
                actualLen = serializer.Serialize(value, context);

            Serializer.VerifyBytesWritten(expectedLen, actualLen);
            return prefixLen + actualLen;
        }
    }
}
