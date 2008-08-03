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
        private readonly bool isRequired, isGroup, canBeGroup;
        public bool IsRequired { get { return isRequired; } }
        public bool IsGroup { get { return isGroup; } }
        public bool CanBeGroup { get { return canBeGroup; } }

        private readonly DataFormat dataFormat;
        public DataFormat DataFormat { get { return dataFormat; } }

        private readonly PropertyInfo property;
        public PropertyInfo Property { get { return property; } }

        protected readonly Getter<TEntity, TProperty> GetValue;
        protected readonly Setter<TEntity, TProperty> SetValue;

        
        
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

            this.valueSerializer = GetSerializer<TValue>(property);
            this.lengthSerializer = valueSerializer as ILengthSerializer<TValue>;
            this.canBeGroup = lengthSerializer == null ? false : lengthSerializer.CanBeGroup;

            this.wireType = valueSerializer.WireType;
            switch (WireType)
            {
                case WireType.Fixed32:
                case WireType.Fixed64:
                case WireType.Variant:
                    // fine, but can't be a group
                    canBeGroup = false;
                    break;
                case WireType.String:
                    if(lengthSerializer == null)
                    {   
                        throw new ProtoException("Cannot be treated as lengh-prefixed: " + Name);
                    }
                    break;
                case WireType.StartGroup:
                case WireType.EndGroup:
                default:
                    throw new ProtoException("Invalid wire type: " + Name);             
            }
            if (IsGroup && !CanBeGroup)
            {
                throw new ProtoException("Cannot be treated as a group: " + Name);
            }
            
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
        
        int IProperty<TEntity>.Serialize(TEntity instance, SerializationContext context)
        {
            TProperty value = GetValue(instance);
            return HasValue(value) ? Serialize(value, context) : 0;
        }

        public abstract int Serialize(TProperty value, SerializationContext context);

        private readonly ISerializer<TValue> valueSerializer;
        private readonly ILengthSerializer<TValue> lengthSerializer;
        private readonly WireType wireType;

        public WireType WireType { get { return wireType; } }
        public string DefinedType { get { return valueSerializer.DefinedType; } }

        public Type PropertyType { get { return typeof(TProperty); } }
        public virtual bool IsRepeated { get { return false; } }

        protected TValue DeserializeValue(TValue value, SerializationContext context)
        {
            value = valueSerializer.Deserialize(value, context);
            Trace(true, value, context);
            return value;
        }
        protected int SerializeValue(TValue value, SerializationContext context)
        {
            
            Trace(false, value, context);

            if (isGroup)
            {
                return Serializer.WriteFieldToken(Tag, WireType.StartGroup, context)
                    + valueSerializer.Serialize(value, context)
                    + Serializer.WriteFieldToken(Tag, WireType.EndGroup, context);
            }
            else if (WireType == WireType.String)
            {
                return Serializer.WriteFieldToken(Tag, WireType, context)
                    + context.WriteLengthPrefixed(value, lengthSerializer);
            }
            else
            {
                return Serializer.WriteFieldToken(Tag, WireType, context)
                    + valueSerializer.Serialize(value, context);
            }
        }

        [Conditional(SerializationContext.VerboseSymbol)]
        protected void Trace(bool read, object value, SerializationContext context)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(read ? '>' : '<', context.Depth).Append(' ')
                .Append(Tag).Append(' ').Append(Name)
                .Append(" = ").Append(value)
                .Append('\t').Append(context.Position);
            Debug.WriteLine(sb.ToString(), SerializationContext.DebugCategory);

        }
    }
}
