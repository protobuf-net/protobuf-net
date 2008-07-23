using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace ProtoBuf
{
    abstract class PropertyBase<TEntity, TValue> : IProperty<TEntity> where TEntity : class, new()
    {
        internal delegate TValue Getter(TEntity instance);
        internal delegate void Setter(TEntity instance, TValue value);

        public virtual object DefaultValue
        {
            get
            {
                DefaultValueAttribute dva = AttributeUtils.GetAttribute<DefaultValueAttribute>(property);
                return dva == null ? null : dva.Value;
            }
        }
        public virtual string Description
        {
            get
            {
                DescriptionAttribute da = AttributeUtils.GetAttribute<DescriptionAttribute>(property);
                return da == null ? null : da.Description;
            }
        }
        protected static ISerializer<T> GetSerializer<T>(PropertyInfo property)
        {
            ProtoMemberAttribute attrib = AttributeUtils.GetAttribute<ProtoMemberAttribute>(property);
            DataFormat format = attrib == null ? DataFormat.Default : attrib.DataFormat;

            return SerializerCache<T>.GetSerializer(format);
        }

        private readonly int tag;
        public int Tag { get { return tag; } }
        private readonly string name;
        public string Name { get { return name; } }
        private readonly bool isRequired;
        public bool IsRequired { get { return isRequired; } }
        private readonly DataFormat dataFormat;
        public DataFormat DataFormat { get { return dataFormat; } }


        private readonly PropertyInfo property;
        public PropertyInfo Property { get { return property; } }
        private readonly Getter getter;
        private readonly Setter setter;
        protected PropertyBase(PropertyInfo property)
        {
            if (property == null) throw new ArgumentNullException("property");
            this.property = property;
            if (!Serializer.TryGetTag(property, out tag, out name, out dataFormat, out isRequired))
            {
                throw new ArgumentOutOfRangeException(string.Format(
                    "Property is valid for proto-serialization: {0}.{1}",
                    property.DeclaringType.Name, property.Name));
            }

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

        protected int GetPrefixLength()
        {
            return Serializer.GetPrefixLength(Tag, WireType);
        }
        protected int WriteFieldToken(SerializationContext context)
        {
            return Serializer.WriteFieldToken(Tag, WireType, context);
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
            int prefixLen = WriteFieldToken(context),
                actualLen = serializer.Serialize(value, context);

            Serializer.VerifyBytesWritten(expectedLen, actualLen);
            return prefixLen + actualLen;
        }
    }
}
