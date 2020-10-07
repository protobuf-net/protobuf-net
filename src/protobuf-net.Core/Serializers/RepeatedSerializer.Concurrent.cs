using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Serializers
{
    public static partial class RepeatedSerializer
    {
        /// <summary>Create a serializer that operates on immutable sets</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<TCollection, T> CreateConcurrentBag<TCollection, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            where TCollection : ConcurrentBag<T>
            => SerializerCache<ConcurrentBagSerializer<TCollection, T>>.InstanceField;

        /// <summary>Create a serializer that operates on immutable sets</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<TCollection, T> CreateConcurrentStack<TCollection, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            where TCollection : ConcurrentStack<T>
            => SerializerCache<ConcurrentStackSerializer<TCollection, T>>.InstanceField;

        /// <summary>Create a serializer that operates on immutable sets</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<TCollection, T> CreateConcurrentQueue<TCollection, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            where TCollection : ConcurrentQueue<T>
            => SerializerCache<ConcurrentQueueSerializer<TCollection, T>>.InstanceField;

        /// <summary>Create a serializer that operates on immutable sets</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<TCollection, T> CreateIProducerConsumerCollection<TCollection, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            where TCollection : class, IProducerConsumerCollection<T>
            => SerializerCache<ProducerConsumerSerializer<TCollection, T>>.InstanceField;
    }

    class ProducerConsumerSerializer<TCollection, T> : RepeatedSerializer<TCollection, T>
        where TCollection : class, IProducerConsumerCollection<T>
    {
        protected override TCollection Clear(TCollection values, ISerializationContext context)
        {
            if (values.Count != 0)
            {
                if (values is ICollection<T> collection) collection.Clear();
                else ThrowHelper.ThrowInvalidOperationException("Unable to clear the collection: " + values.GetType().NormalizeName());
            }
            return values;
        }

        protected override TCollection AddRange(TCollection values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            foreach(var item in newValues.AsSpan())
            {
                if (!values.TryAdd(item)) ThrowHelper.ThrowInvalidOperationException("Unable to add to the collection: " + values.GetType().NormalizeName());
            }
            return values;
        }

        protected override TCollection Initialize(TCollection values, ISerializationContext context)
            => values ?? TypeModel.ActivatorCreate<TCollection>(); // we *are* the factory

        protected override int TryGetCount(TCollection values) => TryGetCountDefault(values);

        internal override long Measure(TCollection values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
        {
            var iter = values.GetEnumerator();
            try
            {
                return Measure(ref iter, serializer, context, wireType);
            }
            finally
            {
                iter?.Dispose();
            }
        }
        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, TCollection values, ISerializer<T> serializer)
        {
            var iter = values.GetEnumerator();
            try
            {
                Write(ref state, fieldNumber, category, wireType, ref iter, serializer);
            }
            finally
            {
                iter?.Dispose();
            }
        }
        internal override void WritePacked(ref ProtoWriter.State state, TCollection values, IMeasuringSerializer<T> serializer, WireType wireType)
        {
            var iter = values.GetEnumerator();
            try
            {
                WritePacked(ref state, ref iter, serializer, wireType);
            }
            finally
            {
                iter?.Dispose();
            }
        }
    }
    sealed class ConcurrentBagSerializer<TCollection, T> : ProducerConsumerSerializer<TCollection, T>
        where TCollection : ConcurrentBag<T>
    {
#if PLAT_CONCURRENT_CLEAR
        protected override TCollection Clear(TCollection values, ISerializationContext context)
        {
            values.Clear();
            return values;
        }
#endif

        protected override TCollection AddRange(TCollection values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            foreach (var value in newValues.AsSpan())
                values.Add(value);
            return values;
        }
    }

    sealed class ConcurrentQueueSerializer<TCollection, T> : ProducerConsumerSerializer<TCollection, T>
        where TCollection : ConcurrentQueue<T>
    {
#if PLAT_CONCURRENT_CLEAR
        protected override TCollection Clear(TCollection values, ISerializationContext context)
        {
            values.Clear();
            return values;
        }
#endif

        protected override TCollection AddRange(TCollection values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            foreach (var value in newValues.AsSpan())
                values.Enqueue(value);
            return values;
        }
    }

    sealed class ConcurrentStackSerializer<TCollection, T> : ProducerConsumerSerializer<TCollection, T>
        where TCollection : ConcurrentStack<T>
    {
        protected override TCollection Clear(TCollection values, ISerializationContext context)
        {
            values.Clear();
            return values;
        }

        protected override TCollection AddRange(TCollection values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            newValues.ReverseInPlace();
            values.PushRange(newValues.Array, newValues.Offset, newValues.Count);
            return values;
        }
    }
}
