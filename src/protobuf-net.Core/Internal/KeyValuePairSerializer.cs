using ProtoBuf.Meta;
using System.Collections.Generic;

namespace ProtoBuf.Internal
{
    internal sealed class KeyValuePairSerializer<TKey, TValue> : ISerializer<KeyValuePair<TKey, TValue>>
    {
        // this is used to prevent problems deserializing maps that omit the bytes for an empty string key/value
        internal static KeyValuePair<TKey, TValue> Default => new KeyValuePair<TKey, TValue>(
            TypeHelper<TKey>.Default, TypeHelper<TValue>.Default
        );

        public SerializerFeatures Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;

        private KeyValuePairSerializer(
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

        private static readonly KeyValuePairSerializer<TKey, TValue> s_default = CreateDefault();

        private static KeyValuePairSerializer<TKey, TValue> CreateDefault()
        {
            try
            {
                var keySerializer = TypeModel.TryGetSerializer<TKey>(null);
                var valueSerializer = TypeModel.TryGetSerializer<TValue>(null);
                if (keySerializer != null && valueSerializer != null)
                {
                    return new KeyValuePairSerializer<TKey, TValue>(keySerializer, keySerializer.Features,
                        valueSerializer, valueSerializer.Features);
                }
            }
            catch {}
            return null;
        }

        public static KeyValuePairSerializer<TKey, TValue> Create(
            TypeModel model,
            ISerializer<TKey> keySerializer, SerializerFeatures keyFeatures,
            ISerializer<TValue> valueSerializer, SerializerFeatures valueFeatures)
        {
            keySerializer ??= TypeModel.GetSerializer<TKey>(model);
            valueSerializer ??= TypeModel.GetSerializer<TValue>(model);

            var shared = s_default;
            if (shared != null && keySerializer == shared._keySerializer && valueSerializer == shared._valueSerializer
                && keyFeatures == shared._keyFeatures && valueFeatures == shared._valueFeatures)
            {
                return shared;
            }
            return new KeyValuePairSerializer<TKey, TValue>(keySerializer, keyFeatures, valueSerializer, valueFeatures);
        }

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
            return new KeyValuePair<TKey, TValue>(key, value);
        }

        public void Write(ref ProtoWriter.State state, KeyValuePair<TKey, TValue> value)
        {
            if (!EqualityComparer<TKey>.Default.Equals(value.Key, default))
                state.WriteAny(1, _keyFeatures, value.Key, _keySerializer);

            if (!EqualityComparer<TValue>.Default.Equals(value.Value, default))
                state.WriteAny(2, _valueFeatures, value.Value, _valueSerializer);
        }
    }
}
