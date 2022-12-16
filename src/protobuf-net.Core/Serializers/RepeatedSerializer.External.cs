using ProtoBuf.Internal;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ProtoBuf.Serializers
{

    /// <summary>
    /// ExternalSerializer provides a base class for concrete types to inherit from, but only provide the methods for collection management
    /// It does not require changes to internal protobuf-net state handling
    /// </summary>
    /// <typeparam name="TCollection">the collection type being provided (e.g. Map for F#) </typeparam>
    /// <typeparam name="T">type of the value held within the collection</typeparam>

    public abstract class ExternalSerializer<TCollection, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] T> : RepeatedSerializer<TCollection, T> where TCollection : IEnumerable<T>
    {
        internal override long Measure(TCollection values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
        {
            IEnumerator<T> iter = null;
            try
            {
                iter = values.GetEnumerator();
                return Measure(ref iter, serializer, context, wireType);
            }
            finally
            {
                if (iter != null)
                    iter.Dispose();
            }
        }
        internal override void WritePacked(ref ProtoWriter.State state, TCollection values, IMeasuringSerializer<T> serializer, WireType wireType)
        {
            IEnumerator<T> iter = null;
            try
            {
                iter = values.GetEnumerator();
                WritePacked(ref state, ref iter, serializer, wireType);
            }
            finally
            {
                if (iter != null)
                    iter.Dispose();
            }
        }

        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, TCollection values, ISerializer<T> serializer, SerializerFeatures features)
        {
            IEnumerator<T> iter = null;
            try
            {
                iter = values.GetEnumerator();
                Write(ref state, fieldNumber, category, wireType, ref iter, serializer, features);
            }
            finally
            {
                if (iter != null)
                    iter.Dispose();
            }
        }
    }
}
