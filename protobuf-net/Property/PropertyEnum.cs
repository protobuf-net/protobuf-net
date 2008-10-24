using System.Collections.Generic;

namespace ProtoBuf.Property
{
    /// <summary>
    /// Property implemenation that handles enum values.
    /// </summary>
    /// <remarks>All enum wire-values must be in the Int32 range.</remarks>
    internal sealed class PropertyEnum<TSource,TEnum> : Property<TSource, TEnum>
        where TEnum : struct
    {
        private static readonly KeyValuePair<TEnum, int>[] values;
        
        private static readonly EqualityComparer<TEnum> enumComparer = EqualityComparer<TEnum>.Default;
        
        private int defaultWireValue;
        
        static PropertyEnum() {
            // get the known mappings between TEnum and TValue; if a wire-value is set
            // via ProtoEnumAttribute then use that; otherwise just convert/cast the
            // value
            List<KeyValuePair<TEnum, int>> list = new List<KeyValuePair<TEnum, int>>();
            foreach (Serializer.ProtoEnumValue<TEnum> value in Serializer.GetEnumValues<TEnum>())
            {
                foreach (KeyValuePair<TEnum, int> existing in list)
                {
                    if ((existing.Value == value.WireValue) || enumComparer.Equals(existing.Key, value.EnumValue))
                    {
                        errMsg = string.Format("The enum {0} has conflicting values {1} and {2}",
                            typeof(TEnum), existing.Key, value.Name);
                        return; // but throw from the *regular* ctor to prevent obscure type init errors
                    }
                }

                list.Add(new KeyValuePair<TEnum, int>(value.EnumValue, value.WireValue));
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

        private static TEnum GetKey(int value)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].Value == value) return values[i].Key;
            }

            throw new ProtoException(string.Format("No key found for {0}", value));
        }

        private static bool TryGetWireValue(TEnum key, out int value) 
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (enumComparer.Equals(values[i].Key, key))
                {
                    value = values[i].Value;
                    return true;
                }
            }
            value = 0;
            return false;
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
                if(!TryGetWireValue(DefaultValue, out defaultWireValue))
                {
                    throw new ProtoException(string.Format(
                        "The default enum value ({0}.{1}) is not defined for the optional property {2}",
                        typeof(TEnum).Name, DefaultValue, Name));    
                }
                
            }
        }

        public override WireType WireType { get { return WireType.Variant; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            int wireValue;
            if(!TryGetWireValue(GetValue(source), out wireValue))
            {
                throw new ProtoException(string.Format(
                    "The value ({0}.{1}) has no wire-representation for property {2}",
                    typeof(TEnum).Name, DefaultValue, Name));
            }
            if (IsOptional && wireValue == defaultWireValue) return 0;
            return WritePrefix(context) + context.EncodeInt32(wireValue);
        }

        public override TEnum DeserializeImpl(TSource source, SerializationContext context)
        {
            int wireValue = context.DecodeInt32();
            return GetKey(wireValue);
        }
    }
}
