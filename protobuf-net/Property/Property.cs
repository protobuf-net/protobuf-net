using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
#if !CF
using System.Reflection.Emit;
#endif

namespace ProtoBuf.Property
{
    internal interface ILengthProperty<TValue>
    {
        int Serialize(TValue value, SerializationContext context);
    }
    internal abstract class Property<TSource>
    {
        public abstract Type PropertyType { get; }

        public override string ToString()
        {
            return prefix == 0 ? "(not initialised)" : string.Format("{0}: {1} ({2})", Name, Tag, WireType);
        }

        public bool IsGroup { get { return WireType == WireType.StartGroup; } }
        public void Init(int tag, DataFormat format, Delegate getValue, Delegate setValue, bool isOptional, object defaultValue)
        {
            InitPrivate(tag, format, isOptional, null, getValue, setValue, defaultValue);
        }
        public void Init(MemberInfo member)
        {
            if (member == null) throw new ArgumentNullException("member");
            InitPrivate(0, DataFormat.Default, false, member, null, null, null);
        }


        public virtual IEnumerable<Property<TSource>> GetCompatibleReaders()
        {
            yield break;
        }

        private void InitPrivate(int tag, DataFormat dataFormat, bool isOptional, MemberInfo member, Delegate getValue, Delegate setValue, object defaultValue)
        {
            if (this.prefix != 0) throw new InvalidOperationException("Can only initialise a property once");

            if(member != null)
            {
                bool isRequired;
                if (!Serializer.TryGetTag(member, out tag, out name, out dataFormat, out isRequired))
                {
                    throw new ArgumentException(member.Name + " cannot be used for serialization", "member");
                }
                isOptional = !isRequired;
                {
                    DefaultValueAttribute dva = AttributeUtils.GetAttribute<DefaultValueAttribute>(member);
                    defaultValue = dva == null ? null : dva.Value;
                }
#if CF
                this.description = null; // not used in CF; assigned to get rid of warning
#else
                {
                    DescriptionAttribute da = AttributeUtils.GetAttribute<DescriptionAttribute>(member);
                    this.description = da == null ? null : da.Description;
                }
#endif
            }
            this.isOptional = isOptional;
            this.defaultValue = defaultValue;
            this.dataFormat = dataFormat; // set initial format, and use the *field* for the "ref" so either usage is valid
            OnBeforeInit(member, getValue, setValue, tag, ref this.dataFormat);
            this.prefix = Serializer.GetFieldToken(tag, WireType);
            OnAfterInit();
        }

        private DataFormat dataFormat;
        public DataFormat DataFormat { get { return dataFormat; } }

        protected virtual void OnBeforeInit(MemberInfo member, Delegate getValue, Delegate setValue, int tag, ref DataFormat format)
        {}
        protected virtual void OnAfterInit()
        { }

        private string name, description;
        public string Name { get { return name; } }
        public string Description { get { return description; } }

        private bool isOptional;
        public bool IsOptional { get { return isOptional; } }

        private uint prefix;
        public uint FieldPrefix { get { return prefix; } }
        private bool suppressPrefix;
        public bool SuppressPrefix
        {
            get { return suppressPrefix; }
            set { suppressPrefix = value; }
        }
        public int Tag { get { return (int)(prefix >> 3); } }
        public abstract WireType WireType { get; }

        protected int WritePrefix(SerializationContext context)
        {
            return suppressPrefix ? 0 : context.EncodeUInt32(FieldPrefix);
        }

        public abstract int Serialize(TSource source, SerializationContext context);
        public abstract void Deserialize(TSource source, SerializationContext context);

        private object defaultValue;
        public object DefaultValue { get { return defaultValue; } }

        // only called for .proto generation, so no need to optimise
        public virtual bool IsRepeated { get { return false; } }
        public abstract string DefinedType { get; }
    }

    internal abstract class Property<TSource, TValue> : Property<TSource>
    {
        public override Type PropertyType
        {
            get { return typeof(TValue); }
        }
        private Getter<TSource, TValue> getValue;
        private Setter<TSource, TValue> setValue;

        protected Property<TSource, TValue> CreateSlave(Property<TValue, TValue> inner)
        {
            Property<TSource, TValue> prop = new SlaveProperty<TSource, TValue>(inner);
            prop.Init(Tag, DataFormat, getValue, setValue, IsOptional, defaultValue);
            return prop;
        }
        protected T CreateAlternative<T>(DataFormat format) where T : new()
        {
            T alt = new T();
            Property<TSource> forInit = alt as Property<TSource>;
            if (forInit == null) throw new InvalidOperationException("Internal protobuf-net error; the type T in CreateAlternative must be a Property<TSource>; this cannot be enforced in a generic constraint due to a mono compiler issue. T was " + typeof(T).FullName);
            forInit.Init(Tag, format, getValue, setValue, IsOptional, DefaultValue);
            return alt;
        }
        
        protected TValue GetValue(TSource source) { return getValue(source); }
        protected void SetValue(TSource source, TValue value) { setValue(source, value); }
        
        private TValue defaultValue;
        new protected TValue DefaultValue { get { return defaultValue; } }

        public override void Deserialize(TSource source, SerializationContext context)
        {
            SetValue(source, DeserializeImpl(source, context));
        }
     
        public abstract TValue DeserializeImpl(TSource source, SerializationContext context);

        protected virtual void OnBeforeInit(int tag, ref DataFormat format) { }
        protected override sealed void OnBeforeInit(MemberInfo member, Delegate getValue, Delegate setValue, int tag, ref DataFormat format)
        {
            base.OnBeforeInit(member, getValue, setValue, tag, ref format);
            this.defaultValue = ConvertValue(base.DefaultValue);
            if (member == null)
            {
                this.getValue = (Getter<TSource, TValue>)getValue;
                this.setValue = (Setter<TSource, TValue>)setValue;
            }
            else
            {
                switch (member.MemberType)
                {
                    case MemberTypes.Property:
                        PropertyInfo prop = (PropertyInfo)member;
                        if (prop.CanRead)
                        {
#if CF2
                            this.getValue = delegate(TSource source) { return (TValue)prop.GetValue(source, null); };
#else
                            this.getValue = (Getter<TSource, TValue>)Delegate.CreateDelegate(typeof(Getter<TSource, TValue>), null, prop.GetGetMethod(true))
                                ?? delegate { throw new ProtoException("Property cannot be read: " + Name); };
#endif
                        }
                        if (prop.CanWrite)
                        {
#if CF2
                            this.setValue = delegate(TSource source, TValue value) { prop.SetValue(source, value, null); };
#else
                            this.setValue = (Setter<TSource, TValue>)Delegate.CreateDelegate(typeof(Setter<TSource, TValue>), null, prop.GetSetMethod(true))
                                ?? delegate { throw new ProtoException("Property cannot be written: " + Name); };
#endif
                        }
                        break;
                    case MemberTypes.Field:
                        FieldInfo field = (FieldInfo)member;
#if CF
                        // basic boxing/reflection
                        this.getValue = delegate(TSource source) { return (TValue)field.GetValue(source); };
                        this.setValue = delegate(TSource source, TValue value) { field.SetValue(source, value); };
#else
                        // custom IL
                        DynamicMethod method = new DynamicMethod("stfld_" + field.Name, null, new Type[] { typeof(TSource), typeof(TValue)}, field.DeclaringType);
                        ILGenerator il = method.GetILGenerator();
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Stfld, field);
                        il.Emit(OpCodes.Ret);
                        this.setValue = (Setter<TSource, TValue>)method.CreateDelegate(typeof(Setter<TSource, TValue>));

                        method = new DynamicMethod("ldfild_" + field.Name, typeof(TValue), new Type[] { typeof(TSource) }, field.DeclaringType);
                        il = method.GetILGenerator();
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, field);
                        il.Emit(OpCodes.Ret);
                        this.getValue = (Getter<TSource, TValue>)method.CreateDelegate(typeof(Getter<TSource, TValue>));
#endif
     
                        break;
                    default:
                        throw new ArgumentException(member.MemberType.ToString() + " not supported for serialization: ", "member");
                }
            }
            OnBeforeInit(tag, ref format);
        }

        private static TValue ConvertValue(object value)
        {
            if (value == null)
            {
                return default(TValue);
            }
            else
            {
                try
                {
                    return (TValue)Convert.ChangeType(
                        value, typeof(TValue), CultureInfo.InvariantCulture);
                }
                catch
                {
#if !SILVERLIGHT && !CF
                    TypeConverter tc = TypeDescriptor.GetConverter(typeof(TValue));
                    if (tc.CanConvertFrom(value.GetType()))
                    {
                        return (TValue)tc.ConvertFrom(null, CultureInfo.InvariantCulture, value);
                    }
#endif
                    throw;
                }
            }
        }
    }

    class SlaveProperty<TSource, TValue> : Property<TSource, TValue>
    {
        private readonly Property<TValue, TValue> inner;
        public SlaveProperty(Property<TValue, TValue> inner)
        {
            if (inner == null) throw new ArgumentNullException("inner");
            this.inner = inner;
        }
        public override string DefinedType
        {
            get { return inner.DefinedType; }
        }
        public override WireType WireType
        {
            get { return inner.WireType; }
        }
        public override int Serialize(TSource source, SerializationContext context)
        {
            return inner.Serialize(GetValue(source), context);
        }
        public override TValue DeserializeImpl(TSource source, SerializationContext context)
        {
            return inner.DeserializeImpl(GetValue(source), context);
        }


    }
}
