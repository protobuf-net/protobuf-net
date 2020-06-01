using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ProtoBuf.Internal
{
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct KeyValuePairSerializer<TKey, TValue> : ISerializer<KeyValuePair<TKey, TValue>>
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
            if (TypeHelper<TKey>.IsReferenceType && TypeHelper<TKey>.ValueChecker.IsNull(key))
                key = TypeModel.CreateInstance<TKey>(state.Context, _keySerializer);
            if (TypeHelper<TValue>.IsReferenceType && TypeHelper<TValue>.ValueChecker.IsNull(value))
                value = TypeModel.CreateInstance<TValue>(state.Context, _valueSerializer);

            return new KeyValuePair<TKey, TValue>(key, value);
        }

        public void Write(ref ProtoWriter.State state, KeyValuePair<TKey, TValue> value)
        {
            // this deals with nulls and implicit zeros
            if (TypeHelper<TKey>.ValueChecker.HasNonTrivialValue(value.Key))
                state.WriteAny(1, _keyFeatures, value.Key, _keySerializer);

            // this deals with nulls and implicit zeros
            if (TypeHelper<TValue>.ValueChecker.HasNonTrivialValue(value.Value))
                state.WriteAny(2, _valueFeatures, value.Value, _valueSerializer);
        }
    }
}
