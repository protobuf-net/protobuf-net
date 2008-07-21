using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf
{
    sealed class EnumSerializer<TEnum, TValue> : ISerializer<TEnum>
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
            List<KeyValuePair<TEnum, TValue>> list = new List<KeyValuePair<TEnum,TValue>>();
            foreach(TEnum key in Enum.GetValues(typeof(TEnum)))
            {
                string name = Enum.GetName(typeof(TEnum), key);
                ProtoEnumAttribute ea = AttributeUtils.GetAttribute<ProtoEnumAttribute>
                    (typeof(TEnum).GetField(name, BindingFlags.Static | BindingFlags.Public));
                TValue value;
                if (ea == null || !ea.HasValue())
                {
                    value = (TValue)Convert.ChangeType(key, typeof(TValue));
                }
                else
                {
                    value = (TValue)Convert.ChangeType(ea.Value, typeof(TValue));
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
            throw new KeyNotFoundException(string.Format("No key found for {0}={1}", typeof(TValue).Name, value));
        }
        private TValue GetValue(TEnum key) 
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (enumComparer.Equals(values[i].Key, key)) return values[i].Value;
            }
            throw new KeyNotFoundException(string.Format("No value found for {0}={1}", typeof(TEnum).Name, key));
        }
        string ISerializer<TEnum>.DefinedType { get { return valueSerializer.DefinedType; } }
        WireType ISerializer<TEnum>.WireType { get { return valueSerializer.WireType; } }
        int ISerializer<TEnum>.GetLength(TEnum key, SerializationContext context)
        {
            return valueSerializer.GetLength(GetValue(key), context);
        }
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
