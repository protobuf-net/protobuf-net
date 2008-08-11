using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace ProtoBuf.Property
{
    internal sealed class PropertyEnum<TSource,TEnum> : Property<TSource, TEnum>
        where TEnum : struct
    {
        private static readonly KeyValuePair<TEnum, uint>[] values;
        
        private static readonly EqualityComparer<TEnum> enumComparer = EqualityComparer<TEnum>.Default;
        
        private uint defaultWireValue;
        
        static PropertyEnum() {
            // get the known mappings between TEnum and TValue; if a wire-value is set
            // via ProtoEnumAttribute then use that; otherwise just convert/cast the
            // value
            List<KeyValuePair<TEnum, uint>> list = new List<KeyValuePair<TEnum, uint>>();
            foreach (FieldInfo enumField in typeof(TEnum).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (!enumField.IsLiteral)
                {
                    continue;
                }

                TEnum key = (TEnum) enumField.GetValue(null);
                ProtoEnumAttribute ea = AttributeUtils.GetAttribute<ProtoEnumAttribute>(enumField);

                uint value;
                if (ea == null || !ea.HasValue())
                {
                    value = (uint)Convert.ChangeType(key, typeof(uint), CultureInfo.InvariantCulture);
                }
                else
                {
                    value = (uint)ea.Value;
                }
                foreach(KeyValuePair<TEnum, uint> existing in list) {
                    if ((existing.Value == value) || enumComparer.Equals(existing.Key, key))
                    {
                        errMsg = string.Format("The enum {0} has conflicting values {1} and {2}",
                            typeof(TEnum), existing.Key, enumField.Name);
                        return; // but throw from the *regular* ctor to prevent obscure type init errors
                    }
                }
                list.Add(new KeyValuePair<TEnum, uint>(key, value));
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

        private static TEnum GetKey(uint value)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].Value == value) return values[i].Key;
            }

            throw new ProtoException(string.Format("No key found for {0}", value));
        }

        private static uint GetWireValue(TEnum key) 
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
        
        protected override void OnAfterInit()
        {
            base.OnAfterInit();
            if(IsOptional) //&& !Enum.IsDefined(typeof(TEnum), DefaultValue))
            {
                defaultWireValue = GetWireValue(DefaultValue);
                //throw new ProtoException(string.Format(
                //    "The default enum value ({0}.{1}) is invalid for the optional property {2}", typeof(TEnum).Name, DefaultValue, Name));
            }
        }

        public override WireType WireType { get { return WireType.Variant; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            uint wireValue = GetWireValue(GetValue(source));
            if (IsOptional && wireValue == defaultWireValue) return 0;
            return WritePrefix(context) + context.EncodeUInt32(wireValue);
        }

        public override TEnum DeserializeImpl(TSource source, SerializationContext context)
        {
            uint wireValue = context.DecodeUInt32();
            return GetKey(wireValue);
        }
    }
}
