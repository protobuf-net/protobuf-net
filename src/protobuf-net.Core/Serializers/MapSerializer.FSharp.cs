using ProtoBuf.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.FSharp.Collections;
using System.Linq;

namespace ProtoBuf.Serializers
{
    public static partial class MapSerializer
    {

        /// <summary>Create a map serializer that operates on FSharp Maps</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static MapSerializer<FSharpMap<TKey, TValue>, TKey, TValue> CreateFSharpMap<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] TKey, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] TValue>()
            => SerializerCache<FSharpMapSerializer<TKey, TValue>>.InstanceField;
    }

    public sealed class FSharpMapSerializer<TKey, TValue> : MapSerializer<FSharpMap<TKey, TValue>, TKey, TValue>
    {
        protected override FSharpMap<TKey, TValue> Clear(FSharpMap<TKey, TValue> values, ISerializationContext context)
            => MapModule.Empty<TKey, TValue>();
        protected override FSharpMap<TKey, TValue> Initialize(FSharpMap<TKey, TValue> values, ISerializationContext context)
            => values ?? MapModule.Empty<TKey, TValue>();
        protected override FSharpMap<TKey, TValue> AddRange(FSharpMap<TKey, TValue> values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context)
        {
            if (values.IsEmpty)
            {
                return MapModule.OfSeq<TKey,TValue>(newValues.Select(r=> new Tuple<TKey, TValue> (r.Key,r.Value)));
            }
            var map = values;
            foreach (var pair in newValues)
            {
                map = MapModule.Add(pair.Key, pair.Value, map);
            }
            return map;
        }
        protected override FSharpMap<TKey, TValue> SetValues(FSharpMap<TKey, TValue> values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context)
        {
            if (values.IsEmpty)
            {
                return MapModule.OfSeq<TKey, TValue>(newValues.Select(r => new Tuple<TKey, TValue>(r.Key, r.Value)));
            }
            var dictionary = new Dictionary<TKey, TValue>(values.Count);
            foreach(var cur in values)
            {
                dictionary.Add(cur.Key, cur.Value);
            }
            foreach (var pair in newValues)
            {
                dictionary[pair.Key] = pair.Value;
            }
            return MapModule.OfSeq<TKey, TValue>(dictionary.Select(r => new Tuple<TKey, TValue>(r.Key, r.Value)));
        }

        internal override void Write(ref ProtoWriter.State state, int fieldNumber, WireType wireType, FSharpMap<TKey, TValue> values, in KeyValuePairSerializer<TKey, TValue> pairSerializer)
        {
            var iter = ((IEnumerable<KeyValuePair<TKey, TValue>>)values).GetEnumerator();
            Write(ref state, fieldNumber, wireType, ref iter, pairSerializer);
        }
    }
}
