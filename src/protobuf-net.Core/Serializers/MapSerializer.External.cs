using ProtoBuf.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ProtoBuf.Serializers
{
    /// <summary>
    /// ExternalMapSerializer provides a base class for concrete types to inherit from, but only provide the methods for collection management
    /// It does not require changes to internal protobuf-net state handling
    /// </summary>
    /// <typeparam name="TCollection">the collection type being provided (e.g. Map for F#) </typeparam>
    /// <typeparam name="TKey">key to the collection</typeparam>
    /// <typeparam name="TValue">type of the value held within the collection</typeparam>
    public abstract class ExternalMapSerializer<[DynamicallyAccessedMembers(DynamicAccess.ContractType)]TCollection, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] TKey, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] TValue> : MapSerializer<TCollection, TKey, TValue> where TCollection : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        internal override void Write(ref ProtoWriter.State state, int fieldNumber, WireType wireType, TCollection values, in KeyValuePairSerializer<TKey, TValue> pairSerializer)
        {
            var iter = values.GetEnumerator();
            Write(ref state, fieldNumber, wireType, ref iter, pairSerializer);
        }
    }
}
