using Microsoft.FSharp.Collections;
using ProtoBuf;
using ProtoBuf.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProtoBuf.FSharp
{
    /// <summary>
    /// Serialization provider for F# Maps
    /// </summary>
    /// <typeparam name="TKey">type of map key</typeparam>
    /// <typeparam name="TValue">type of value</typeparam>
    public sealed class FSharpMapSerializer<TKey, TValue> : ExternalMapSerializer<FSharpMap<TKey, TValue>, TKey, TValue>
    {
        /// <inheritdoc/>
        protected override FSharpMap<TKey, TValue> Clear(FSharpMap<TKey, TValue> values, ISerializationContext context)
            => MapModule.Empty<TKey, TValue>();

        /// <inheritdoc/>
        protected override FSharpMap<TKey, TValue> Initialize(FSharpMap<TKey, TValue> values, ISerializationContext context)
            => values ?? MapModule.Empty<TKey, TValue>();

        /// <inheritdoc/>
        protected override FSharpMap<TKey, TValue> AddRange(FSharpMap<TKey, TValue> values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context)
        {
            if (values == null || values.IsEmpty)
            {
                return MapModule.OfSeq<TKey, TValue>(newValues.Select(r => new Tuple<TKey, TValue>(r.Key, r.Value)));
            }
            var map = values;
            foreach (var pair in newValues)
            {
                map = MapModule.Add(pair.Key, pair.Value, map);
            }
            return map;
        }

        /// <inheritdoc/>
        protected override FSharpMap<TKey, TValue> SetValues(FSharpMap<TKey, TValue> values, ref ArraySegment<KeyValuePair<TKey, TValue>> newValues, ISerializationContext context)
        {
            if (values.IsEmpty)
            {
                return MapModule.OfSeq<TKey, TValue>(newValues.Select(r => new Tuple<TKey, TValue>(r.Key, r.Value)));
            }
            var dictionary = new Dictionary<TKey, TValue>(values.Count);
            foreach (var cur in values)
            {
                dictionary.Add(cur.Key, cur.Value);
            }
            foreach (var pair in newValues)
            {
                dictionary[pair.Key] = pair.Value;
            }
            return MapModule.OfSeq<TKey, TValue>(dictionary.Select(r => new Tuple<TKey, TValue>(r.Key, r.Value)));
        }
    }

    /// <summary>
    /// Factory class to provide consistent idiom with in-build protobuf collections.
    /// This class is the reason for implementation in C# rather than F#: 
    ///     static classes in F# are module, but module does not allow typeof-module
    /// </summary>
    public static class FSharpMapFactory 
    {
        /// <summary>Create a map serializer that operates on FSharp Maps</summary>
        public static MapSerializer<FSharpMap<TKey, TValue>, TKey, TValue> Create<TKey, TValue>()
            => new FSharpMapSerializer<TKey, TValue>();
    }
}