using System;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;

namespace ProtoBuf
{
    internal sealed class EnumSerializer<TEnum, TValue> : ISerializer<TEnum>
        where TEnum : struct
        where TValue : struct
    {
        private readonly KeyValuePair<TEnum, TValue>[] values;
        private readonly ISerializer<TValue> valueSerializer;

        private static readonly EqualityComparer<TEnum> enumComparer = EqualityComparer<TEnum>.Default;
        private static readonly EqualityComparer<TValue> valueComparer = EqualityComparer<TValue>.Default;

        public EnumSerializer(ISerializer<TValue> valueSerializer)
        {
            if (valueSerializer == null) throw new ArgumentNullException("valueSerializer");
            this.valueSerializer = valueSerializer;

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

                list.Add(new KeyValuePair<TEnum, TValue>(key, value));
            }

            values = list.ToArray();
        }

        private TEnum GetKey(TValue value)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (valueComparer.Equals(values[i].Value, value)) return values[i].Key;
            }

            throw new ProtoException(string.Format("No key found for {0}={1}", typeof(TValue).Name, value));
        }

        private TValue GetValue(TEnum key) 
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (enumComparer.Equals(values[i].Key, key)) return values[i].Value;
            }

            throw new ProtoException(string.Format("No value found for {0}={1}", typeof(TEnum).Name, key));
        }

        string ISerializer<TEnum>.DefinedType { get { return Serializer.GetDefinedTypeName<TEnum>(); } }
        WireType ISerializer<TEnum>.WireType { get { return valueSerializer.WireType; } }

        int ISerializer<TEnum>.Serialize(TEnum key, SerializationContext context)
        {
            return valueSerializer.Serialize(GetValue(key), context);
        }

        TEnum ISerializer<TEnum>.Deserialize(TEnum key, SerializationContext context)
        {
            return GetKey(valueSerializer.Deserialize(default(TValue), context));
        }
    }
}
