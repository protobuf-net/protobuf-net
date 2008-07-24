using System;
using System.ComponentModel;
using System.Reflection;
using System.Diagnostics;
using System.Text;

namespace ProtoBuf
{
    internal abstract class PropertyBase<TEntity, TValue> : IProperty<TEntity> where TEntity : class, new()
    {
#if !CF2
        internal delegate TValue Getter(TEntity instance);
        internal delegate void Setter(TEntity instance, TValue value);
#endif
        public virtual object DefaultValue
        {
            get
            {
                DefaultValueAttribute dva = AttributeUtils.GetAttribute<DefaultValueAttribute>(property);
                return dva == null ? null : dva.Value;
            }
        }
#if !CF
        public virtual string Description
        {
            get
            {
                DescriptionAttribute da = AttributeUtils.GetAttribute<DescriptionAttribute>(property);
                return da == null ? null : da.Description;
            }
        }
#endif
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
#if !CF2
        private readonly Getter getter;
        private readonly Setter setter;
#endif
        protected PropertyBase(PropertyInfo property)
        {
            if (property == null) throw new ArgumentNullException("property");
            this.property = property;
            if (!Serializer.TryGetTag(property, out tag, out name, out dataFormat, out isRequired))
            {
                throw new ArgumentOutOfRangeException(
                    string.Format(
                        "Property is valid for proto-serialization: {0}.{1}",
                        property.DeclaringType.Name,
                        property.Name));
            }
#if !CF2
            MethodInfo method;
            if (property.CanRead && (method = property.GetGetMethod(true)) != null)
            {
                getter = (Getter)Delegate.CreateDelegate(typeof(Getter), null, method);
            }
            if (property.CanWrite && (method = property.GetSetMethod(true)) != null)
            {
                setter = (Setter)Delegate.CreateDelegate(typeof(Setter), null, method);
            }
#endif
        }

        protected TValue GetValue(TEntity instance)
        {
#if CF2
            return (TValue)property.GetValue(instance,null);
#else
            return getter(instance);
#endif
        }

        protected void SetValue(TEntity instance, TValue value)
        {
#if CF2
            property.SetValue(instance,value, null);
#else
            setter(instance, value);
#endif
        }

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
            if (len == 0)
            {
                return 0;
            }
            return GetPrefixLength() + len;
        }
        protected int Serialize<TActualValue>(TActualValue value, ISerializer<TActualValue> serializer, SerializationContext context)
        {
            // TODO: add a "ShouldSerialize" instead of this
            int expectedLen = serializer.GetLength(value, context);
            if (expectedLen == 0) return 0;
            int prefixLen = WriteFieldToken(context),
                actualLen = serializer.Serialize(value, context);

            Trace(false, value, context);

            Serializer.VerifyBytesWritten(expectedLen, actualLen);
            return prefixLen + actualLen;
        }


        [Conditional(SerializationContext.VerboseSymbol)]
        protected void Trace(bool read, object value, SerializationContext context)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(read ? '>' : '<', context.Depth).Append(' ')
                .Append(Tag).Append(' ').Append(Name)
                .Append(" = ").Append(value)
                .Append('\t').Append(context.Stream.Position);
            Debug.WriteLine(sb.ToString(), SerializationContext.DebugCategory);

        }
    }
}
