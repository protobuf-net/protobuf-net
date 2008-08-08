using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace ProtoBuf.Property
{
    internal sealed class PropertyEnum<TSource,TEnum, TValue> : Property<TSource, TEnum>
        where TEnum : struct
        where TValue : struct
    {
        private static readonly KeyValuePair<TEnum, TValue>[] values;
        
        private static readonly EqualityComparer<TEnum> enumComparer = EqualityComparer<TEnum>.Default;
        private static readonly EqualityComparer<TValue> valueComparer = EqualityComparer<TValue>.Default;


        static PropertyEnum() {
            // get the known mappings between TEnum and TValue; if a wire-value is set
            // via ProtoEnumAttribute then use that; otherwise just convert/cast the
            // value
            List<KeyValuePair<TEnum, TValue>> list = new List<KeyValuePair<TEnum, TValue>>();
            foreach (FieldInfo enumField in typeof(TEnum).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (!enumField.IsLiteral)
                {
                    continue;
                }

                TEnum key = (TEnum) enumField.GetValue(null);
                ProtoEnumAttribute ea = AttributeUtils.GetAttribute<ProtoEnumAttribute>(enumField);

                TValue value;
                if (ea == null || !ea.HasValue())
                {
                    value = (TValue)Convert.ChangeType(key, typeof(TValue), CultureInfo.InvariantCulture);
                }
                else
                {
                    value = (TValue)Convert.ChangeType(ea.Value, typeof(TValue), CultureInfo.InvariantCulture);
                }
                foreach(KeyValuePair<TEnum, TValue> existing in list) {
                    if (enumComparer.Equals(existing.Key, key) || valueComparer.Equals(existing.Value, value))
                    {
                        errMsg = string.Format("The enum {0} has conflicting values {1} and {2}",
                            typeof(TEnum), existing.Key, enumField.Name);
                        return; // but throw from the *regular* ctor to prevent obscure type init errors
                    }
                }
                list.Add(new KeyValuePair<TEnum, TValue>(key, value));
            }

            values = list.ToArray();
        }
        private static readonly string errMsg;
        public PropertyEnum()
        {
            if (errMsg != null)
            {
                throw new ProtoException(errMsg);
            }
        }

        private static TEnum GetKey(TValue value)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (valueComparer.Equals(values[i].Value, value)) return values[i].Key;
            }

            throw new ProtoException(string.Format("No key found for {0}={1}", typeof(TValue).Name, value));
        }

        private static TValue GetValue(TEnum key) 
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (enumComparer.Equals(values[i].Key, key)) return values[i].Value;
            }

            throw new ProtoException(string.Format("No value found for {0}={1}", typeof(TEnum).Name, key));
        }
        public override string DefinedType
        {
            get { return Serializer.GetDefinedTypeName<TEnum>(); }
        }
        private Property<TValue, TValue> innerProperty;
        protected override void OnBeforeInit(MemberInfo member, bool overrideIsGroup)
        {
            innerProperty = PropertyFactory.CreatePassThru<TValue>(member, overrideIsGroup);
            base.OnBeforeInit(member, overrideIsGroup);
        }
        protected override void OnAfterInit()
        {
            base.OnAfterInit();
            if(IsOptional && !Enum.IsDefined(typeof(TEnum), DefaultValue))
            {
                throw new ProtoException(string.Format(
                    "The default enum value ({0}.{1}) is invalid for the optional property {2}", typeof(TEnum).Name, DefaultValue, Name));
            }
        }

        public override WireType WireType { get { return WireType.Variant; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            TEnum value = GetValue(source);
            if (IsOptional && enumComparer.Equals(value, DefaultValue)) return 0;
            return innerProperty.Serialize(GetValue(value), context);
        }

        public override TEnum DeserializeImpl(TSource source, SerializationContext context)
        {
            return GetKey(innerProperty.DeserializeImpl(default(TValue), context));
        }
    }
}
