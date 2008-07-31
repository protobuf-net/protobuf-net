using System;
using System.ComponentModel;
using System.Reflection;
using System.Diagnostics;
using System.Text;

namespace ProtoBuf
{

    internal delegate TValue Getter<TEntity, TValue>(TEntity instance) where TEntity : class;
    internal delegate void Setter<TEntity, TValue>(TEntity instance, TValue value) where TEntity : class;

    internal abstract class PropertyBase<TEntity, TProperty, TValue> : IProperty<TEntity>
        where TEntity : class, new()
    {
        public object DefaultValue
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

        protected abstract bool HasValue(TProperty value);

        private readonly int tag, prefixLength;
        public int Tag { get { return tag; } }
        private readonly string name;
        public string Name { get { return name; } }
        private readonly bool isRequired;
        public bool IsRequired { get { return isRequired; } }
        private readonly DataFormat dataFormat;
        public DataFormat DataFormat { get { return dataFormat; } }

        private readonly PropertyInfo property;
        public PropertyInfo Property { get { return property; } }

        protected readonly Getter<TEntity, TProperty> GetValue;
        protected readonly Setter<TEntity, TProperty> SetValue;

        private readonly bool isGroup;
        public bool IsGroup { get { return isGroup; } }
        protected PropertyBase(PropertyInfo property)
        {
            if (property == null) throw new ArgumentNullException("property");
            this.property = property;
            if (!Serializer.TryGetTag(property, out tag, out name, out dataFormat, out isRequired, out isGroup))
            {
                throw new ArgumentOutOfRangeException(
                    string.Format(
                        "Property is valid for proto-serialization: {0}.{1}",
                        property.DeclaringType.Name,
                        property.Name));
            }
            prefixLength = Serializer.GetPrefixLength(tag);

            this.ValueSerializer = GetSerializer<TValue>(property);
            this.groupSerializer = ValueSerializer as IGroupSerializer<TValue>;

            MethodInfo method;
            if (property.CanRead && (method = property.GetGetMethod(true)) != null)
            {
#if CF2
                GetValue = delegate(TEntity instance) { return (TProperty)property.GetValue(instance, null); };
#else
                GetValue = (Getter<TEntity, TProperty>)Delegate.CreateDelegate(typeof(Getter<TEntity, TProperty>), null, method); 
#endif

            }
            if (property.CanWrite && (method = property.GetSetMethod(true)) != null)
            {
#if CF2
                SetValue = delegate(TEntity instance, TProperty value) { property.SetValue(instance, value, null); };
#else
                SetValue = (Setter<TEntity, TProperty>)Delegate.CreateDelegate(typeof(Setter<TEntity, TProperty>), null, method);
#endif
            }

        }

        public abstract void Deserialize(TEntity instance, SerializationContext context);
        public abstract void DeserializeGroup(TEntity instance, SerializationContext context);

        public int Serialize(TEntity instance, SerializationContext context)
        {
            TProperty value = GetValue(instance);
            return HasValue(value) ? Serialize(value, context) : 0;
        }

        public abstract int Serialize(TProperty value, SerializationContext context);

        public int GetLength(TEntity instance, SerializationContext context)
        {
            TProperty value = GetValue(instance);
            return HasValue(value) ? GetLengthImpl(value, context) : 0;
        }

        protected readonly ISerializer<TValue> ValueSerializer;

        private readonly IGroupSerializer<TValue> groupSerializer;
        protected IGroupSerializer<TValue> GroupSerializer
        {
            get
            {
                if (groupSerializer == null) throw new ProtoException("Cannot treat property as a group: " + Name);
                return groupSerializer;
            }
        }
                

        protected abstract int GetLengthImpl(TProperty instance, SerializationContext context);

        public WireType WireType { get { return ValueSerializer.WireType; } }
        public string DefinedType { get { return ValueSerializer.DefinedType; } }

        public Type PropertyType { get { return typeof(TProperty); } }
        public virtual bool IsRepeated { get { return false; } }

        protected int GetValueLength(TValue value, SerializationContext context)
        {
            if (isGroup)
            {
                return prefixLength + prefixLength + GroupSerializer.GetLengthGroup(value, context);
            }
            else
            {
                int len = ValueSerializer.GetLength(value, context);
                return len == 0 ? 0 : prefixLength + len;
            }
        }
        protected int SerializeValue(TValue value, SerializationContext context)
        {
            
            Trace(false, value, context);

            if (isGroup)
            {
                return Serializer.WriteFieldToken(Tag, WireType.StartGroup, context)
                    + GroupSerializer.SerializeGroup(value, context)
                    + Serializer.WriteFieldToken(Tag, WireType.EndGroup, context);
            }
            else
            {
                return Serializer.WriteFieldToken(Tag, WireType, context)
                    + ValueSerializer.Serialize(value, context);
            }
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
