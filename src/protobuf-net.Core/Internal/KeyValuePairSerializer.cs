using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System.Collections.Generic;

namespace ProtoBuf.Internal
{
    internal struct KeyValuePairSerializer<TKey, TValue> : ISerializer<KeyValuePair<TKey, TValue>>
    {
        public SerializerFeatures Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;

        internal KeyValuePairSerializer(
            ISerializer<TKey> keySerializer, SerializerFeatures keyFeatures,
            ISerializer<TValue> valueSerializer, SerializerFeatures valueFeatures)
        {
            _keySerializer = keySerializer;
            _valueSerializer = valueSerializer;
            _keyFeatures = keyFeatures;
            _valueFeatures = valueFeatures;
        }

        private readonly ISerializer<TKey> _keySerializer;
        private readonly ISerializer<TValue> _valueSerializer;
        private readonly SerializerFeatures _keyFeatures, _valueFeatures;

        public KeyValuePair<TKey, TValue> Read(ref ProtoReader.State state, KeyValuePair<TKey, TValue> pair)
        {
            TKey key = pair.Key;
            TValue value = pair.Value;
            int field;
            bool haveKey = false, haveValue = false;
            while ((field = state.ReadFieldHeader()) > 0)
            {
                switch (field)
                {
                    case 1:
                        key = state.ReadAny(_keyFeatures, key, _keySerializer);
                        break;
                    case 2:
                        value = state.ReadAny(_valueFeatures, value, _valueSerializer);
                        break;
                    default:
                        state.SkipField();
                        break;
                }
            }
            if (TypeHelper<TKey>.IsReferenceType && !haveKey && key is null)
                key = TypeModel.CreateInstance<TKey>(state.Context, _keySerializer);
            if (TypeHelper<TValue>.IsReferenceType && !haveValue && value is null)
                value = TypeModel.CreateInstance<TValue>(state.Context, _valueSerializer);

            return new KeyValuePair<TKey, TValue>(key, value);
        }

        public void Write(ref ProtoWriter.State state, KeyValuePair<TKey, TValue> value)
        {
            try
            {
                // this deals with nulls and implicit zeros
                if (!EqualityComparer<TKey>.Default.Equals(value.Key, default))
                {
                    if (typeof(TKey) == typeof(string) && (string)(object)value.Key == "")
                    { } // don't write empty strings; ugly, but it works
                    else
                    {
                        state.WriteAny(1, _keyFeatures, value.Key, _keySerializer);
                    }
                }

                // this deals with nulls and implicit zeros
                if (!EqualityComparer<TValue>.Default.Equals(value.Value, default))
                {
                    if (typeof(TValue) == typeof(string) && (string)(object)value.Value == "")
                    { } // don't write empty strings; ugly, but it works
                    else
                    {
                        state.WriteAny(2, _valueFeatures, value.Value, _valueSerializer);
                    }
                }
            }
            catch (ProtoPackedException ppe)
            {   // something very odd is happning, but *very* rarely (always "Issue54"); investigating
                ThrowHelper.ThrowInvalidOperationException(ppe.Message + $" with features {_keyFeatures}/{_valueFeatures}", ppe); 
            }
        }
    }
}
