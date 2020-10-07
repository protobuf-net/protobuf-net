using ProtoBuf.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProtoBuf.Serializers
{
    public static partial class RepeatedSerializer
    {
        /// <summary>Create a serializer that operates on immutable arrays</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<ImmutableArray<T>, T> CreateImmutableArray<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => SerializerCache<ImmutableArraySerializer<T>>.InstanceField;

        /// <summary>Create a serializer that operates on immutable lists</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<ImmutableList<T>, T> CreateImmutableList<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => SerializerCache<ImmutableListSerializer<T>>.InstanceField;

        /// <summary>Create a serializer that operates on immutable lists</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<IImmutableList<T>, T> CreateImmutableIList<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => SerializerCache<ImmutableIListSerializer<T>>.InstanceField;

        /// <summary>Create a serializer that operates on immutable queues</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<ImmutableQueue<T>, T> CreateImmutableQueue<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => SerializerCache<ImmutableQueueSerializer<T>>.InstanceField;

        /// <summary>Create a serializer that operates on immutable queues</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<IImmutableQueue<T>, T> CreateImmutableIQueue<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => SerializerCache<ImmutableIQueueSerializer<T>>.InstanceField;

        /// <summary>Create a serializer that operates on immutable stacks</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<ImmutableStack<T>, T> CreateImmutableStack<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => SerializerCache<ImmutableStackSerializer<T>>.InstanceField;

        /// <summary>Create a serializer that operates on immutable stacks</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<IImmutableStack<T>, T> CreateImmutableIStack<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => SerializerCache<ImmutableIStackSerializer<T>>.InstanceField;

        /// <summary>Create a serializer that operates on immutable sets</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<ImmutableHashSet<T>, T> CreateImmutableHashSet<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => SerializerCache<ImmutableHashSetSerializer<T>>.InstanceField;

        /// <summary>Create a serializer that operates on immutable sets</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<ImmutableSortedSet<T>, T> CreateImmutableSortedSet<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => SerializerCache<ImmutableSortedSetSerializer<T>>.InstanceField;

        /// <summary>Create a serializer that operates on immutable sets</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<IImmutableSet<T>, T> CreateImmutableISet<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => SerializerCache<ImmutableISetSerializer<T>>.InstanceField;



    }

    sealed class ImmutableArraySerializer<T> : RepeatedSerializer<ImmutableArray<T>, T>
    {
        protected override ImmutableArray<T> Initialize(ImmutableArray<T> values, ISerializationContext context)
            => values.IsDefault ? ImmutableArray<T>.Empty : values;
        protected override ImmutableArray<T> Clear(ImmutableArray<T> values, ISerializationContext context)
            => values.Clear();
        protected override ImmutableArray<T> AddRange(ImmutableArray<T> values, ref ArraySegment<T> newValues, ISerializationContext context)
            => newValues.Count == 1 ? values.Add(newValues.Singleton()) : values.AddRange(newValues);

        protected override int TryGetCount(ImmutableArray<T> values) => values.IsEmpty ? 0 : values.Length;

        internal override long Measure(ImmutableArray<T> values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
        {
            var iter = new Enumerator(values);
            return Measure(ref iter, serializer, context, wireType);
        }

        internal override void WritePacked(ref ProtoWriter.State state, ImmutableArray<T> values, IMeasuringSerializer<T> serializer, WireType wireType)
        {
            var iter = new Enumerator(values);
            WritePacked(ref state, ref iter, serializer, wireType);
        }

        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, ImmutableArray<T> values, ISerializer<T> serializer)
        {
            var iter = new Enumerator(values);
            Write(ref state, fieldNumber, category, wireType, ref iter, serializer);
        }

        [StructLayout(LayoutKind.Auto)]
        struct Enumerator : IEnumerator<T>
        {
            public void Reset() => ThrowHelper.ThrowNotSupportedException();
            public Enumerator(ImmutableArray<T> array) => _iter = array.GetEnumerator();
            private ImmutableArray<T>.Enumerator _iter;
            public T Current => _iter.Current;
            object IEnumerator.Current => _iter.Current;
            public bool MoveNext() => _iter.MoveNext();
            public void Dispose() { }
        }
    }

    sealed class ImmutableListSerializer<T> : RepeatedSerializer<ImmutableList<T>, T>
    {
        protected override ImmutableList<T> Initialize(ImmutableList<T> values, ISerializationContext context)
            => values ?? ImmutableList<T>.Empty;
        protected override ImmutableList<T> AddRange(ImmutableList<T> values, ref ArraySegment<T> newValues, ISerializationContext context)
            => newValues.Count == 1 ? values.Add(newValues.Singleton()) : values.AddRange(newValues);
        protected override ImmutableList<T> Clear(ImmutableList<T> values, ISerializationContext context)
            => values.Clear();
        protected override int TryGetCount(ImmutableList<T> values) => values is null ? 0 : values.Count;

        internal override long Measure(ImmutableList<T> values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
        {
            var iter = values.GetEnumerator();
            return Measure(ref iter, serializer, context, wireType);
        }
        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, ImmutableList<T> values, ISerializer<T> serializer)
        {
            var iter = values.GetEnumerator();
            Write(ref state, fieldNumber, category, wireType, ref iter, serializer);
        }
        internal override void WritePacked(ref ProtoWriter.State state, ImmutableList<T> values, IMeasuringSerializer<T> serializer, WireType wireType)
        {
            var iter = values.GetEnumerator();
            WritePacked(ref state, ref iter, serializer, wireType);
        }
    }
    sealed class ImmutableIListSerializer<T> : RepeatedSerializer<IImmutableList<T>, T>
    {
        protected override IImmutableList<T> Initialize(IImmutableList<T> values, ISerializationContext context)
            => values ?? ImmutableList<T>.Empty;
        protected override IImmutableList<T> AddRange(IImmutableList<T> values, ref ArraySegment<T> newValues, ISerializationContext context)
            => newValues.Count == 1 ? values.Add(newValues.Singleton()) : values.AddRange(newValues);
        protected override IImmutableList<T> Clear(IImmutableList<T> values, ISerializationContext context)
            => values.Clear();
        protected override int TryGetCount(IImmutableList<T> values) => TryGetCountDefault(values);

        internal override long Measure(IImmutableList<T> values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
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
        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, IImmutableList<T> values, ISerializer<T> serializer)
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
        internal override void WritePacked(ref ProtoWriter.State state, IImmutableList<T> values, IMeasuringSerializer<T> serializer, WireType wireType)
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

    sealed class ImmutableHashSetSerializer<T> : RepeatedSerializer<ImmutableHashSet<T>, T>
    {
        protected override ImmutableHashSet<T> Initialize(ImmutableHashSet<T> values, ISerializationContext context)
            => values ?? ImmutableHashSet<T>.Empty;
        protected override ImmutableHashSet<T> AddRange(ImmutableHashSet<T> values, ref ArraySegment<T> newValues, ISerializationContext context)
            => newValues.Count == 1 ? values.Add(newValues.Singleton()) : values.Union(newValues);
        protected override ImmutableHashSet<T> Clear(ImmutableHashSet<T> values, ISerializationContext context)
            => values.Clear();
        protected override int TryGetCount(ImmutableHashSet<T> values) => values is null ? 0 : values.Count;

        internal override long Measure(ImmutableHashSet<T> values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
        {
            var iter = values.GetEnumerator();
            return Measure(ref iter, serializer, context, wireType);
        }
        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, ImmutableHashSet<T> values, ISerializer<T> serializer)
        {
            var iter = values.GetEnumerator();
            Write(ref state, fieldNumber, category, wireType, ref iter, serializer);
        }
        internal override void WritePacked(ref ProtoWriter.State state, ImmutableHashSet<T> values, IMeasuringSerializer<T> serializer, WireType wireType)
        {
            var iter = values.GetEnumerator();
            WritePacked(ref state, ref iter, serializer, wireType);
        }
    }
    sealed class ImmutableSortedSetSerializer<T> : RepeatedSerializer<ImmutableSortedSet<T>, T>
    {
        protected override ImmutableSortedSet<T> Initialize(ImmutableSortedSet<T> values, ISerializationContext context)
            => values ?? ImmutableSortedSet<T>.Empty;
        protected override ImmutableSortedSet<T> AddRange(ImmutableSortedSet<T> values, ref ArraySegment<T> newValues, ISerializationContext context)
            => newValues.Count == 1 ? values.Add(newValues.Singleton()) : values.Union(newValues);
        protected override ImmutableSortedSet<T> Clear(ImmutableSortedSet<T> values, ISerializationContext context)
            => values.Clear();
        protected override int TryGetCount(ImmutableSortedSet<T> values) => values is null ? 0 : values.Count;

        internal override long Measure(ImmutableSortedSet<T> values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
        {
            var iter = values.GetEnumerator();
            return Measure(ref iter, serializer, context, wireType);
        }
        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, ImmutableSortedSet<T> values, ISerializer<T> serializer)
        {
            var iter = values.GetEnumerator();
            Write(ref state, fieldNumber, category, wireType, ref iter, serializer);
        }
        internal override void WritePacked(ref ProtoWriter.State state, ImmutableSortedSet<T> values, IMeasuringSerializer<T> serializer, WireType wireType)
        {
            var iter = values.GetEnumerator();
            WritePacked(ref state, ref iter, serializer, wireType);
        }
    }
    sealed class ImmutableISetSerializer<T> : RepeatedSerializer<IImmutableSet<T>, T>
    {
        protected override IImmutableSet<T> Initialize(IImmutableSet<T> values, ISerializationContext context)
            => values ?? ImmutableHashSet<T>.Empty;
        protected override IImmutableSet<T> AddRange(IImmutableSet<T> values, ref ArraySegment<T> newValues, ISerializationContext context)
            => newValues.Count == 1 ? values.Add(newValues.Singleton()) : values.Union(newValues);
        protected override IImmutableSet<T> Clear(IImmutableSet<T> values, ISerializationContext context)
            => values.Clear();
        protected override int TryGetCount(IImmutableSet<T> values) => TryGetCountDefault(values);

        internal override long Measure(IImmutableSet<T> values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
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
        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, IImmutableSet<T> values, ISerializer<T> serializer)
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
        internal override void WritePacked(ref ProtoWriter.State state, IImmutableSet<T> values, IMeasuringSerializer<T> serializer, WireType wireType)
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


    sealed class ImmutableStackSerializer<T> : RepeatedSerializer<ImmutableStack<T>, T>
    {
        protected override ImmutableStack<T> Initialize(ImmutableStack<T> values, ISerializationContext context)
            => values ?? ImmutableStack<T>.Empty;
        protected override ImmutableStack<T> AddRange(ImmutableStack<T> values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            if (newValues.Count == 1) return values.Push(newValues.Singleton());
            newValues.ReverseInPlace();
            foreach (var value in newValues.AsSpan())
                values = values.Push(value);
            return values;
        }

        protected override ImmutableStack<T> Clear(ImmutableStack<T> values, ISerializationContext context)
            => values.Clear();
        protected override int TryGetCount(ImmutableStack<T> values) => (values is null || values.IsEmpty) ? 0 : -1;

        internal override long Measure(ImmutableStack<T> values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
        {
            var iter = new Enumerator(values);
            return Measure(ref iter, serializer, context, wireType);
        }
        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, ImmutableStack<T> values, ISerializer<T> serializer)
        {
            var iter = new Enumerator(values);
            Write(ref state, fieldNumber, category, wireType, ref iter, serializer);
        }
        internal override void WritePacked(ref ProtoWriter.State state, ImmutableStack<T> values, IMeasuringSerializer<T> serializer, WireType wireType)
        {
            var iter = new Enumerator(values);
            WritePacked(ref state, ref iter, serializer, wireType);
        }

        [StructLayout(LayoutKind.Auto)]
        struct Enumerator : IEnumerator<T>
        {
            private ImmutableStack<T>.Enumerator _iter;
            public Enumerator(ImmutableStack<T> stack) => _iter = stack.GetEnumerator();

            T IEnumerator<T>.Current => _iter.Current;
            object IEnumerator.Current => _iter.Current;

            void IDisposable.Dispose() { }

            bool IEnumerator.MoveNext() => _iter.MoveNext();

            void IEnumerator.Reset() => ThrowHelper.ThrowNotImplementedException();
        }
    }
    sealed class ImmutableIStackSerializer<T> : RepeatedSerializer<IImmutableStack<T>, T>
    {
        protected override IImmutableStack<T> Initialize(IImmutableStack<T> values, ISerializationContext context)
            => values ?? ImmutableStack<T>.Empty;
        protected override IImmutableStack<T> AddRange(IImmutableStack<T> values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            if (newValues.Count == 1) return values.Push(newValues.Singleton());
            newValues.ReverseInPlace();
            foreach (var value in newValues.AsSpan())
                values = values.Push(value);
            return values;
        }

        protected override IImmutableStack<T> Clear(IImmutableStack<T> values, ISerializationContext context)
            => values.Clear();
        protected override int TryGetCount(IImmutableStack<T> values)
        {
            try
            {
                return values is null || values.IsEmpty ? 0 : -1;
            }
            catch
            {
                return -1;
            }
        }

        internal override long Measure(IImmutableStack<T> values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
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
        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, IImmutableStack<T> values, ISerializer<T> serializer)
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
        internal override void WritePacked(ref ProtoWriter.State state, IImmutableStack<T> values, IMeasuringSerializer<T> serializer, WireType wireType)
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

    sealed class ImmutableQueueSerializer<T> : RepeatedSerializer<ImmutableQueue<T>, T>
    {
        protected override ImmutableQueue<T> Initialize(ImmutableQueue<T> values, ISerializationContext context)
            => values ?? ImmutableQueue<T>.Empty;
        protected override ImmutableQueue<T> AddRange(ImmutableQueue<T> values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            if (newValues.Count == 1) return values.Enqueue(newValues.Singleton());
            foreach (var value in newValues.AsSpan())
                values = values.Enqueue(value);
            return values;
        }

        protected override ImmutableQueue<T> Clear(ImmutableQueue<T> values, ISerializationContext context)
            => values.Clear();
        protected override int TryGetCount(ImmutableQueue<T> values) => values is null || values.IsEmpty ? 0 : -1;

        internal override long Measure(ImmutableQueue<T> values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
        {
            var iter = new Enumerator(values);
            return Measure(ref iter, serializer, context, wireType);
        }
        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, ImmutableQueue<T> values, ISerializer<T> serializer)
        {
            var iter = new Enumerator(values);
            Write(ref state, fieldNumber, category, wireType, ref iter, serializer);
        }
        internal override void WritePacked(ref ProtoWriter.State state, ImmutableQueue<T> values, IMeasuringSerializer<T> serializer, WireType wireType)
        {
            var iter = new Enumerator(values);
            WritePacked(ref state, ref iter, serializer, wireType);
        }

        [StructLayout(LayoutKind.Auto)]
        struct Enumerator : IEnumerator<T>
        {
            private ImmutableQueue<T>.Enumerator _iter;
            public Enumerator(ImmutableQueue<T> queue) => _iter = queue.GetEnumerator();

            T IEnumerator<T>.Current => _iter.Current;
            object IEnumerator.Current => _iter.Current;

            void IDisposable.Dispose() { }

            bool IEnumerator.MoveNext() => _iter.MoveNext();

            void IEnumerator.Reset() => ThrowHelper.ThrowNotImplementedException();
        }
    }
    sealed class ImmutableIQueueSerializer<T> : RepeatedSerializer<IImmutableQueue<T>, T>
    {
        protected override IImmutableQueue<T> Initialize(IImmutableQueue<T> values, ISerializationContext context)
            => values ?? ImmutableQueue<T>.Empty;
        protected override IImmutableQueue<T> AddRange(IImmutableQueue<T> values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            if (newValues.Count == 1) return values.Enqueue(newValues.Singleton());
            foreach (var value in newValues.AsSpan())
                values = values.Enqueue(value);
            return values;
        }

        protected override IImmutableQueue<T> Clear(IImmutableQueue<T> values, ISerializationContext context)
            => values.Clear();
        protected override int TryGetCount(IImmutableQueue<T> values)
        {
            try
            {
                return values is null || values.IsEmpty ? 0 : -1;
            }
            catch
            {
                return -1;
            }
        }

        internal override long Measure(IImmutableQueue<T> values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
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
        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, IImmutableQueue<T> values, ISerializer<T> serializer)
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
        internal override void WritePacked(ref ProtoWriter.State state, IImmutableQueue<T> values, IMeasuringSerializer<T> serializer, WireType wireType)
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
}
