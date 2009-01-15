
using System.Collections.Generic;
namespace ProtoBuf.Property
{
    internal sealed class PropertyPairString<TSource, TKey, TValue> : Property<TSource, KeyValuePair<TKey,TValue>>,
        ILengthProperty<KeyValuePair<TKey,TValue>>
    {
        private Property<TKey, TKey> keyProp;
        private Property<TValue, TValue> valueProp;

        private const int TAG_KEY = 1, TAG_VALUE = 2;
        protected override void OnBeforeInit(int tag, ref DataFormat format)
        {
            // use the default format for the keys...
            DataFormat defaultFormat = DataFormat.Default;
            keyProp = PropertyFactory.CreatePassThru<TKey>(TAG_KEY, ref defaultFormat);
            // ...and the specified format for the values
            valueProp = PropertyFactory.CreatePassThru<TValue>(TAG_VALUE, ref format);
            
            format = ProtoBuf.DataFormat.Default; // but are serializing as string...
            base.OnBeforeInit(tag, ref format);
        }

        public override string DefinedType
        {
            get { return "Pair_" + Serializer.GetDefinedTypeName<TKey>()
                + "_" + Serializer.GetDefinedTypeName<TValue>(); }
        }
        public override WireType WireType { get { return WireType.String; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            KeyValuePair<TKey,TValue> value = GetValue(source);

            return WritePrefix(context) + context.WriteLengthPrefixed(value, 0, this);
        }

        public override KeyValuePair<TKey, TValue> DeserializeImpl(TSource source, SerializationContext context)
        {
            TKey key = default(TKey);
            TValue value = default(TValue);

            long restore = context.LimitByLengthPrefix();
            uint field;
            while(context.TryReadFieldPrefix(out field))
            {
                if(field == keyProp.FieldPrefix)
                {
                    key = keyProp.DeserializeImpl(key, context);
                }
                else if(field == valueProp.FieldPrefix)
                {
                    value = valueProp.DeserializeImpl(value, context);
                }
                else
                {
                    WireType wireType;
                    int tag;
                    Serializer.ParseFieldToken(field, out wireType, out tag);
                    switch(tag)
                    {
                        case TAG_KEY:
                        case TAG_VALUE:
                            throw new ProtoException("Incorrect wire-type reading key/value pair");
                    }
                    Serializer.SkipData(context, tag, wireType);
                }
            }
            // restore the max-pos
            context.MaxReadPosition = restore;

            return new KeyValuePair<TKey, TValue>(key,value);
        }

        int ILengthProperty<KeyValuePair<TKey, TValue>>.Serialize(KeyValuePair<TKey, TValue> pair, SerializationContext context)
        {
            return keyProp.Serialize(pair.Key, context) +
                   valueProp.Serialize(pair.Value, context);
        }
    }
}
