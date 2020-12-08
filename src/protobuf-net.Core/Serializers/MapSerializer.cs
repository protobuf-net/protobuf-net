using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Serializers
{
    /// <summary>
    /// Provides utility methods for creating serializers for repeated data
    /// </summary>
    public static partial class MapSerializer
    {
        /// <summary>Create a map serializer that operates on dictionaries</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static MapSerializer<Dictionary<TKey, TValue>, TKey, TValue> CreateDictionary<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] TKey, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] TValue>()
            => SerializerCache<DictionarySerializer<TKey, TValue>>.InstanceField;

        /// <summary>Create a map serializer that operates on dictionaries</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static MapSerializer<TCollection, TKey, TValue> CreateDictionary<TCollection, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] TKey, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] TValue>()
            where TCollection : IDictionary<TKey, TValue>
            => SerializerCache<DictionarySerializer<TCollection, TKey, TValue>>.InstanceField;
    }

    /// <summary>
    /// Base class for dictionary-like collection serializers
    /// </summary>
    public abstract class MapSerializer<TCollection, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] TKey, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] TValue> : IRepeatedSerializer<TCollection>, IFactory<TCollection>
    {
        SerializerFeatures ISerializer<TCollection>.Features => SerializerFeatures.CategoryRepeated;

        TCollection IFactory<TCollection>.Create(ISerializationContext context) => Initialize(default, context);

        TCollection ISerializer<TCollection>.Read(ref ProtoReader.State state, TCollection value)
        {
            ThrowHelper.ThrowInvalidOperationException("Should have used " + nameof(IRepeatedSerializer<TCollection>.ReadRepeated));
            return default;
        }

        void ISerializer<TCollection>.Write(ref ProtoWriter.State state, TCollection value)
            => ThrowHelper.ThrowInvalidOperationException("Should have used " + nameof(IRepeatedSerializer<TCollection>.WriteRepeated));

        void IRepeatedSerializer<TCollection>.WriteRepeated(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures features, TCollection values)
            => WriteMap(ref state, fieldNumber, features, values, default, default, default, default);

        TCollection IRepeatedSerializer<TCollection>.ReadRepeated(ref ProtoReader.State state, SerializerFeatures features, TCollection values)
            => ReadMap(ref state, features, values, default, default, default, default);

        static KeyValuePairSerializer<TKey, TValue> GetSerializer(
            TypeModel model, SerializerFeatures keyFeatures, SerializerFeatures valueFeatures, ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer)
        {
            keySerializer ??= TypeModel.GetSerializer<TKey>(model);
            valueSerializer ??= TypeModel.GetSerializer<TValue>(model);

            keyFeatures.InheritFrom(keySerializer.Features);
            valueFeatures.InheritFrom(valueSerializer.Features);

            return new KeyValuePairSerializer<TKey, TValue>(keySerializer, keyFeatures, valueSerializer, valueFeatures);
        }

        /// <summary>
        /// Deserializes a sequence of values from the supplied reader
        /// </summary>
        public void WriteMap(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures features, TCollection values,
            SerializerFeatures keyFeatures, SerializerFeatures valueFeatures, ISerializer<TKey> keySerializer = null, ISerializer<TValue> valueSerializer = null)
        {
            var pairSerializer = GetSerializer(state.Model, keyFeatures, valueFeatures, keySerializer, valueSerializer);
            features.InheritFrom(pairSerializer.Features);
            var wireType = features.GetWireType();

            Write(ref state, fieldNumber, wireType, values, pairSerializer);
        }

        internal abstract void Write(ref ProtoWriter.State state, int fieldNumber, WireType wireType, TCollection values, in KeyValuePairSerializer<TKey, TValue> pairSerializer);

        [MethodImpl(ProtoReader.HotPath)]
        internal static void Write<TEnumerator>(ref ProtoWriter.State state, int fieldNumber, WireType wireType, ref TEnumerator enumerator, in KeyValuePairSerializer<TKey, TValue> pairSerializer)
            where TEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            if (enumerator.MoveNext())
            {
                // TODO: avoid boxing on the write API (already done for read)
                ISerializer<KeyValuePair<TKey, TValue>> boxed = pairSerializer;
                do
                {
                    state.WriteFieldHeader(fieldNumber, wireType);
                    state.GetWriter().WriteMessage(ref state, enumerator.Current, boxed, PrefixStyle.Base128, false);
                } while (enumerator.MoveNext());
            }
        }

        /// <summary>Ensure that the collection is not nil, if required</summary>
        protected virtual TCollection Initialize(TCollection values, ISerializationContext context) => values;

        /// <summary>Remove any existing contents from the collection</summary>
        protected abstract TCollection Clear(TCollection values, ISerializationContext context);

        /// <summary>Add new contents to the collection</summary>
        protected abstract TCollection AddRange(TCollection values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context);
        // note: not "in" because ArraySegment<T> is not "readonly" on all targeted TFMs

        /// <summary>Update the new contents intoto the collection, overwriting existing values</summary>
        protected abstract TCollection SetValues(TCollection values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context);
        // note: not "in" because ArraySegment<T> is not "readonly" on all targeted TFMs

        /// <summary>
        /// Deserializes a sequence of values from the supplied reader
        /// </summary>
        public TCollection ReadMap(ref ProtoReader.State state, SerializerFeatures features, TCollection values,
            SerializerFeatures keyFeatures, SerializerFeatures valueFeatures, ISerializer<TKey> keySerializer = null, ISerializer<TValue> valueSerializer = null)
        {
            var ctx = state.Context;
            var pairSerializer = GetSerializer(state.Model, keyFeatures, valueFeatures, keySerializer, valueSerializer);
            features.InheritFrom(pairSerializer.Features);
            values = Initialize(values, ctx);

            using var buffer = state.FillBuffer(features, pairSerializer,
                new KeyValuePair<TKey, TValue>(TypeHelper<TKey>.Default, TypeHelper<TValue>.Default));
            if ((features & SerializerFeatures.OptionClearCollection) != 0) values = Clear(values, ctx);
            if (!buffer.IsEmpty)
            {
                var segment = buffer.Segment;
                values = (features & SerializerFeatures.OptionFailOnDuplicateKey) == 0
                    ? SetValues(values, ref segment, ctx) : AddRange(values, ref segment, ctx);
            }
            return values;

        }
    }

    sealed class DictionarySerializer<TKey, TValue> : MapSerializer<Dictionary<TKey, TValue>, TKey, TValue>
    {
        protected override Dictionary<TKey, TValue> Initialize(Dictionary<TKey, TValue> values, ISerializationContext context)
            => values ?? new Dictionary<TKey, TValue>();

        protected override Dictionary<TKey, TValue> Clear(Dictionary<TKey, TValue> values, ISerializationContext context)
        {
            values.Clear();
            return values;
        }

        protected override Dictionary<TKey, TValue> AddRange(Dictionary<TKey, TValue> values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context)
        {
            foreach (var pair in newValues.AsSpan())
                values.Add(pair.Key, pair.Value);
            return values;
        }

        protected override Dictionary<TKey, TValue> SetValues(Dictionary<TKey, TValue> values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context)
        {
            foreach (var pair in newValues.AsSpan())
                values[pair.Key] = pair.Value;
            return values;
        }

        internal override void Write(ref ProtoWriter.State state, int fieldNumber, WireType wireType, Dictionary<TKey, TValue> values, in KeyValuePairSerializer<TKey, TValue> pairSerializer)
        {
            var iter = values.GetEnumerator();
            Write(ref state, fieldNumber, wireType, ref iter, pairSerializer);
        }
    }
    class DictionarySerializer<TCollection, TKey, TValue> : MapSerializer<TCollection, TKey, TValue>
        where TCollection : IDictionary<TKey, TValue>
    {
        protected override TCollection Initialize(TCollection values, ISerializationContext context)
            // note: don't call TypeModel.CreateInstance: *we are the factory*
            => values ?? (typeof(TCollection).IsInterface ? (TCollection)(object)new Dictionary<TKey, TValue>() : TypeModel.ActivatorCreate<TCollection>());

        protected override TCollection Clear(TCollection values, ISerializationContext context)
        {
            values.Clear();
            return values;
        }

        protected override TCollection AddRange(TCollection values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context)
        {
            foreach (var pair in newValues.AsSpan())
                values.Add(pair.Key, pair.Value);
            return values;
        }

        protected override TCollection SetValues(TCollection values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context)
        {
            foreach (var pair in newValues.AsSpan())
                values[pair.Key] = pair.Value;
            return values;
        }

        internal override void Write(ref ProtoWriter.State state, int fieldNumber, WireType wireType, TCollection values, in KeyValuePairSerializer<TKey, TValue> pairSerializer)
        {
            var iter = values.GetEnumerator();
            try
            {
                Write(ref state, fieldNumber, wireType, ref iter, pairSerializer);
            }
            finally
            {
                iter?.Dispose();
            }
        }
    }
}
