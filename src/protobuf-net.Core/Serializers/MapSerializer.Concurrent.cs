using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Serializers
{
    public static partial class MapSerializer
    {
        /// <summary>Create a map serializer that operates on concurrent dictionaries</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static MapSerializer<TCollection, TKey, TValue> CreateConcurrentDictionary<TCollection, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] TKey, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] TValue>()
            where TCollection : ConcurrentDictionary<TKey, TValue>
            => SerializerCache<ConcurrentDictionarySerializer<TCollection, TKey, TValue>>.InstanceField;
    }

    sealed class ConcurrentDictionarySerializer<TCollection, TKey, TValue> : MapSerializer<TCollection, TKey, TValue>
        where TCollection : ConcurrentDictionary<TKey, TValue>
    {
        protected override TCollection Clear(TCollection values, ISerializationContext context)
        {
            values.Clear();
            return values;
        }
        protected override TCollection Initialize(TCollection values, ISerializationContext context)
            => values ?? TypeModel.ActivatorCreate<TCollection>(); // we *are* the factory

        protected override TCollection AddRange(TCollection values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context)
        {
            foreach (var pair in newValues.AsSpan())
                if (!values.TryAdd(pair.Key, pair.Value)) ThrowHelper.ThrowArgumentException("duplicate key");
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
