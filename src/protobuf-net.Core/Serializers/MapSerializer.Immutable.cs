using ProtoBuf.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Serializers
{
    public static partial class MapSerializer
    {

        /// <summary>Create a map serializer that operates on immutable dictionaries</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static MapSerializer<ImmutableDictionary<TKey, TValue>, TKey, TValue> CreateImmutableDictionary<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] TKey, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] TValue>()
            => SerializerCache<ImmutableDictionarySerializer<TKey, TValue>>.InstanceField;

        /// <summary>Create a map serializer that operates on immutable dictionaries</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static MapSerializer<ImmutableSortedDictionary<TKey, TValue>, TKey, TValue> CreateImmutableSortedDictionary<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] TKey, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] TValue>()
            => SerializerCache<ImmutableSortedDictionarySerializer<TKey, TValue>>.InstanceField;

        /// <summary>Create a map serializer that operates on immutable dictionaries</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static MapSerializer<IImmutableDictionary<TKey, TValue>, TKey, TValue> CreateIImmutableDictionary<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] TKey, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] TValue>()
            => SerializerCache<ImmutableIDictionarySerializer<TKey, TValue>>.InstanceField;
    }

    sealed class ImmutableDictionarySerializer<TKey, TValue> : MapSerializer<ImmutableDictionary<TKey, TValue>, TKey, TValue>
    {
        protected override ImmutableDictionary<TKey, TValue> Clear(ImmutableDictionary<TKey, TValue> values, ISerializationContext context)
            => values.Clear();
        protected override ImmutableDictionary<TKey, TValue> Initialize(ImmutableDictionary<TKey, TValue> values, ISerializationContext context)
            => values ?? ImmutableDictionary<TKey, TValue>.Empty;
        protected override ImmutableDictionary<TKey, TValue> AddRange(ImmutableDictionary<TKey, TValue> values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context)
        {
            if (newValues.Count == 1)
            {
                var pair = newValues.Singleton();
                return values.Add(pair.Key, pair.Value);
            }
            return values.AddRange(newValues);
        }
        protected override ImmutableDictionary<TKey, TValue> SetValues(ImmutableDictionary<TKey, TValue> values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context)
        {
            if (newValues.Count == 1)
            {
                var pair = newValues.Singleton();
                return values.SetItem(pair.Key, pair.Value);
            }
            return values.SetItems(newValues);
        }

        internal override void Write(ref ProtoWriter.State state, int fieldNumber, WireType wireType, ImmutableDictionary<TKey, TValue> values, in KeyValuePairSerializer<TKey, TValue> pairSerializer)
        {
            var iter = values.GetEnumerator();
            Write(ref state, fieldNumber, wireType, ref iter, pairSerializer);
        }
    }

    sealed class ImmutableSortedDictionarySerializer<TKey, TValue> : MapSerializer<ImmutableSortedDictionary<TKey, TValue>, TKey, TValue>
    {
        protected override ImmutableSortedDictionary<TKey, TValue> Clear(ImmutableSortedDictionary<TKey, TValue> values, ISerializationContext context)
            => values.Clear();
        protected override ImmutableSortedDictionary<TKey, TValue> Initialize(ImmutableSortedDictionary<TKey, TValue> values, ISerializationContext context)
            => values ?? ImmutableSortedDictionary<TKey, TValue>.Empty;
        protected override ImmutableSortedDictionary<TKey, TValue> AddRange(ImmutableSortedDictionary<TKey, TValue> values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context)
        {
            if (newValues.Count == 1)
            {
                var pair = newValues.Singleton();
                return values.Add(pair.Key, pair.Value);
            }
            return values.AddRange(newValues);
        }
        protected override ImmutableSortedDictionary<TKey, TValue> SetValues(ImmutableSortedDictionary<TKey, TValue> values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context)
        {
            if (newValues.Count == 1)
            {
                var pair = newValues.Singleton();
                return values.SetItem(pair.Key, pair.Value);
            }
            return values.SetItems(newValues);
        }

        internal override void Write(ref ProtoWriter.State state, int fieldNumber, WireType wireType, ImmutableSortedDictionary<TKey, TValue> values, in KeyValuePairSerializer<TKey, TValue> pairSerializer)
        {
            var iter = values.GetEnumerator();
            Write(ref state, fieldNumber, wireType, ref iter, pairSerializer);
        }
    }

    sealed class ImmutableIDictionarySerializer<TKey, TValue> : MapSerializer<IImmutableDictionary<TKey, TValue>, TKey, TValue>
    {
        protected override IImmutableDictionary<TKey, TValue> Clear(IImmutableDictionary<TKey, TValue> values, ISerializationContext context)
            => values.Clear();
        protected override IImmutableDictionary<TKey, TValue> Initialize(IImmutableDictionary<TKey, TValue> values, ISerializationContext context)
            => values ?? ImmutableDictionary<TKey, TValue>.Empty;
        protected override IImmutableDictionary<TKey, TValue> AddRange(IImmutableDictionary<TKey, TValue> values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context)
        {
            if (newValues.Count == 1)
            {
                var pair = newValues.Singleton();
                return values.Add(pair.Key, pair.Value);
            }
            return values.AddRange(newValues);
        }
        protected override IImmutableDictionary<TKey, TValue> SetValues(IImmutableDictionary<TKey, TValue> values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context)
        {
            if (newValues.Count == 1)
            {
                var pair = newValues.Singleton();
                return values.SetItem(pair.Key, pair.Value);
            }
            return values.SetItems(newValues);
        }

        internal override void Write(ref ProtoWriter.State state, int fieldNumber, WireType wireType, IImmutableDictionary<TKey, TValue> values, in KeyValuePairSerializer<TKey, TValue> pairSerializer)
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
