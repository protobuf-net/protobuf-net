using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Collections.Specialized.BitVector32;

namespace ProtoBuf.Serializers
{
    /// <summary>
    /// Provides utility methods for creating serializers for repeated data
    /// </summary>
    public static partial class RepeatedSerializer
    {
        /// <summary>Create a serializer that indicates that a scenario is not supported</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Obsolete("Since this isn't supported, you probably shouldn't be doing it...", false)]
        public static RepeatedSerializer<TCollection, T> CreateNestedDataNotSupported<TCollection, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
        {
            ThrowHelper.ThrowNestedDataNotSupported(typeof(TCollection));
            return default;
        }

        /// <summary>Create a serializer that indicates that a scenario is not supported</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Obsolete("Since this isn't supported, you probably shouldn't be doing it...", false)]
        public static RepeatedSerializer<TCollection, T> CreateNotSupported<TCollection, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
        {
            ThrowHelper.ThrowNotSupportedException($"Repeated data of type {typeof(TCollection)} is not supported");
            return default;
        }

        /// <summary>Create a serializer that operates on lists</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<List<T>, T> CreateList<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => SerializerCache<ListSerializer<T>>.InstanceField;

        /// <summary>Create a serializer that operates on lists</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<TList, T> CreateList<TList, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            where TList : List<T>
            => SerializerCache<ListSerializer<TList, T>>.InstanceField;

        /// <summary>Create a serializer that operates on most common collections</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<TCollection, T> CreateEnumerable<TCollection, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            where TCollection : class, IEnumerable<T>
            => SerializerCache<EnumerableSerializer<TCollection, TCollection, T>>.InstanceField;

        /// <summary>Create a serializer that operates on most common collections</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<TCollection, T> CreateEnumerable<TCollection, TCreate, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            where TCollection : class, IEnumerable<T>
            where TCreate : TCollection
            => SerializerCache<EnumerableSerializer<TCollection, TCreate, T>>.InstanceField;

        /// <summary>Create a serializer that operates on lists</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<T[], T> CreateVector<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => SerializerCache<VectorSerializer<T>>.InstanceField;

        /// <summary>Create a serializer that operates on lists</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<TCollection, T> CreateQueue<TCollection, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            where TCollection : Queue<T>
            => SerializerCache<QueueSerializer<TCollection, T>>.InstanceField;

        /// <summary>Create a serializer that operates on lists</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<TCollection, T> CreateStack<TCollection, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            where TCollection : Stack<T>
            => SerializerCache<StackSerializer<TCollection, T>>.InstanceField;

        /// <summary>Create a serializer that operates on sets</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<TCollection, T> CreateSet<TCollection, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            where TCollection : ISet<T>
            => SerializerCache<SetSerializer<TCollection, T>>.InstanceField;

#if NET6_0_OR_GREATER
        /// <summary>Create a serializer that operates on sets</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<IReadOnlySet<T>, T> CreateReadOnySet<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => SerializerCache<ReadOnlySetSerializer<T>>.InstanceField;
#endif

        /// <summary>Reverses a range of values</summary>
        [MethodImpl(ProtoReader.HotPath)] // note: not "in" because ArraySegment<T> isn't "readonly" on all TFMs
        internal static void ReverseInPlace<T>(this ref ArraySegment<T> values) => Array.Reverse(values.Array, values.Offset, values.Count);
        [MethodImpl(ProtoReader.HotPath)]
        internal static ref T Singleton<T>(this ref ArraySegment<T> values) => ref values.Array[values.Offset];
    }


    /// <summary>
    /// Base class for simple collection serializers
    /// </summary>
    public abstract class RepeatedSerializer<TCollection, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] TItem> : IRepeatedSerializer<TCollection>, IFactory<TCollection>
    {
        TCollection IFactory<TCollection>.Create(ISerializationContext context) => Initialize(default, context);

        SerializerFeatures ISerializer<TCollection>.Features => SerializerFeatures.CategoryRepeated;

        void IRepeatedSerializer<TCollection>.WriteRepeated(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures features, TCollection values) => WriteRepeated(ref state, fieldNumber, features, values, default);

        TCollection IRepeatedSerializer<TCollection>.ReadRepeated(ref ProtoReader.State state, SerializerFeatures features, TCollection values) => ReadRepeated(ref state, features, values, default);

        TCollection ISerializer<TCollection>.Read(ref ProtoReader.State state, TCollection value)
        {
            ThrowHelper.ThrowInvalidOperationException("Should have used " + nameof(IRepeatedSerializer<TCollection>.ReadRepeated));
            return default;
        }

        void ISerializer<TCollection>.Write(ref ProtoWriter.State state, TCollection value)
            => ThrowHelper.ThrowInvalidOperationException("Should have used " + nameof(IRepeatedSerializer<TCollection>.WriteRepeated));

        private void WriteNullWrapped(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures features, TCollection values, ISerializer<TItem> serializer)
        {
            Debug.Assert(features.IsRepeated(), "repeated feature expected");
            Debug.Assert(features.HasAny(SerializerFeatures.OptionWrappedCollection), "wrapped collection handling expected");

            if (!(TypeHelper<TCollection>.CanBeNull && TypeHelper<TCollection>.ValueChecker.IsNull(values)))
            {
                state.WriteFieldHeader(fieldNumber, ProtoWriter.State.AssertWrappedAndGetWireType(ref features, out _));
                Debug.Assert(!features.HasAny(SerializerFeatures.OptionWrappedCollection), "wrapped collection handling should have been removed");

                state.GetWriter().WriteWrappedCollection(ref state, features, values, this, serializer);
            }
        }

        /// <summary>
        /// Serialize a sequence of values to the supplied writer
        /// </summary>
        public void WriteRepeated(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures features, TCollection values, ISerializer<TItem> serializer = null)
        {
            if (features.HasAny(SerializerFeatures.OptionWrappedCollection))
            {
                WriteNullWrapped(ref state, fieldNumber, features, values, serializer);
                return;
            }

            serializer ??= TypeModel.GetSerializer<TItem>(state.Model);
            var serializerFeatures = serializer.Features;
            if (serializerFeatures.IsRepeated()) TypeModel.ThrowNestedListsNotSupported(typeof(TItem));
            features.InheritFrom(serializerFeatures);

            int count = TryGetCount(values);

            var category = serializerFeatures.GetCategory();
            var wireType = features.GetWireType();
            if (TypeHelper<TItem>.CanBePacked && !features.IsPackedDisabled() && (count == 0 || count > 1) && serializer is IMeasuringSerializer<TItem> measurer)
            {
                if (category != SerializerFeatures.CategoryScalar) serializerFeatures.ThrowInvalidCategory();
                if (count == 0)
                {
                    WriteZeroLengthPackedHeader(ref state, fieldNumber);
                }
                else
                {
                    WritePacked(ref state, fieldNumber, wireType, values, count, measurer);
                }
            }
            else
            {
                if (count != 0) Write(ref state, fieldNumber, category, wireType, values, serializer, features);
            }
        }

        private static void WriteZeroLengthPackedHeader(ref ProtoWriter.State state, int fieldNumber)
        {
            if (state.Model.OmitsOption(TypeModel.TypeModelOptions.SkipZeroLengthPackedArrays))
            {   // we only need to write these for exact v2 compatibility
                state.WriteFieldHeader(fieldNumber, WireType.String);
                var writer = state.GetWriter();
                writer.AdvanceAndReset(writer.ImplWriteVarint64(ref state, 0UL));
            }
        }

        internal abstract void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, TCollection values, ISerializer<TItem> serializer, SerializerFeatures features);

        // this does *not* dispose the enumerator; if the caller cares: caller does
        [MethodImpl(ProtoReader.HotPath)]
        internal static void Write<TEnumerator>(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, ref TEnumerator values, ISerializer<TItem> serializer, SerializerFeatures features)
            where TEnumerator : IEnumerator<TItem>
        {
            var writer = state.GetWriter();
            bool wrapped = features.HasAny(SerializerFeatures.OptionWrappedValue);
            // when wrapping inside a collection, we always need to write the message header, so:
            // we must use field-presence rules
            if (wrapped) features |= SerializerFeatures.OptionWrappedValueFieldPresence;
            while (values.MoveNext())
            {
                var value = values.Current;

                if (wrapped)
                {
                    state.WriteWrapped<TItem>(fieldNumber, features, value, serializer);
                }
                else
                {
                    if (TypeHelper<TItem>.CanBeNull && TypeHelper<TItem>.ValueChecker.IsNull(value))
                        ThrowHelper.ThrowNullRepeatedContents<TItem>();

                    state.WriteFieldHeader(fieldNumber, wireType);
                    switch (category)
                    {
                        case SerializerFeatures.CategoryMessageWrappedAtRoot:
                        case SerializerFeatures.CategoryMessage:
                            writer.WriteMessage<TItem>(ref state, value, serializer, PrefixStyle.Base128, true);
                            break;
                        case SerializerFeatures.CategoryScalar:
                            serializer.Write(ref state, value);
                            break;
                        default:
                            category.ThrowInvalidCategory();
                            break;
                    }
                }
            }
        }

        internal abstract long Measure(TCollection values, IMeasuringSerializer<TItem> serializer, ISerializationContext context, WireType wireType);

        // this does *not* dispose the enumerator; if the caller cares: caller does
        [MethodImpl(ProtoReader.HotPath)]
        internal static long Measure<TEnumerator>(ref TEnumerator values, IMeasuringSerializer<TItem> serializer, ISerializationContext context, WireType wireType)
            where TEnumerator : IEnumerator<TItem>
        {
            long length = 0;
            while (values.MoveNext())
            {
                length += serializer.Measure(context, wireType, values.Current);
            }
            return length;
        }

        internal abstract void WritePacked(ref ProtoWriter.State state, TCollection values, IMeasuringSerializer<TItem> serializer, WireType wireType);

        // this does *not* dispose the enumerator; if the caller cares: caller does
        [MethodImpl(ProtoReader.HotPath)]
        internal static void WritePacked<TEnumerator>(ref ProtoWriter.State state, ref TEnumerator values, IMeasuringSerializer<TItem> serializer, WireType wireType)
            where TEnumerator : IEnumerator<TItem>
        {
            while (values.MoveNext())
            {
                var value = values.Current;
                state.WireType = wireType; // tell the serializer what we want to do
                serializer.Write(ref state, value);
            }
        }

        private void WritePacked(ref ProtoWriter.State state, int fieldNumber, WireType wireType, TCollection values, int count, IMeasuringSerializer<TItem> serializer)
        {
            long expectedLength;
            switch (wireType)
            {
                case WireType.Fixed32:
                    expectedLength = count * 4;
                    break;
                case WireType.Fixed64:
                    expectedLength = count * 8;
                    break;
                case WireType.Varint:
                case WireType.SignedVarint:
                    expectedLength = Measure(values, serializer, state.Context, wireType);
                    break;
                default:
                    ThrowHelper.ThrowInvalidPackedOperationException(wireType, typeof(TItem));
                    expectedLength = default;
                    break;
            }

            state.WriteFieldHeader(fieldNumber, WireType.String);
            var writer = state.GetWriter();
            writer.AdvanceAndReset(writer.ImplWriteVarint64(ref state, (ulong)expectedLength));
            long before = state.GetPosition();
            WritePacked(ref state, values, serializer, wireType);
            long actualLength = state.GetPosition() - before;
            if (actualLength != expectedLength) ThrowHelper.ThrowInvalidOperationException(
                $"packed encoding length miscalculation for {typeof(TItem).NormalizeName()}, {wireType}; expected {expectedLength}, got {actualLength}");
        }

        /// <summary>If possible to do so *cheaply*, return the count of the items in the collection</summary>
        /// <remarks>TryGetCountDefault can be used as a reasonable fallback</remarks>
        protected abstract int TryGetCount(TCollection values);

        /// <summary>Applies a range of common strategies for cheaply counting collections</summary>
        /// <remarks>This involves multiple tests and exception handling; if your collection is known to be reliable, you should prefer an exposed .Count or similar</remarks>
        protected int TryGetCountDefault(TCollection values)
        {
            try
            {
                return values switch
                {
                    IReadOnlyCollection<TItem> roc => roc.Count, // test this first - most common things implement it
                    ICollection<TItem> collection => collection.Count,
                    ICollection untyped => untyped.Count,
                    null => 0,
                    _ => -1,
                };
            }
            catch
            {   // some types pretend to be countable, but they *lie*
                return -1;
            }
        }

        private TCollection ReadNullWrapped(ref ProtoReader.State state, SerializerFeatures features, TCollection values, ISerializer<TItem> serializer)
        {
            features &= ~(SerializerFeatures.OptionWrappedCollection | SerializerFeatures.OptionWrappedCollectionGroup);
            int fieldNumber;
            var tok = state.StartSubItem();
            bool needInit = true;
            while ((fieldNumber = state.ReadFieldHeader()) > 0)
            {
                if (fieldNumber == TypeModel.ListItemTag)
                {
                    values = ReadRepeated(ref state, features, values, serializer);
                    needInit = false;
                }
                else
                {
                    state.SkipField();
                }
            }
            state.EndSubItem(tok);
            if (needInit)
            {
                values = Initialize(values, state.Context);
            }
            return values;
        }

        /// <summary>
        /// Deserializes a sequence of values from the supplied reader
        /// </summary>
        public TCollection ReadRepeated(ref ProtoReader.State state, SerializerFeatures features, TCollection values, ISerializer<TItem> serializer = null)
        {
            if (features.HasAny(SerializerFeatures.OptionWrappedCollection))
            {
                return ReadNullWrapped(ref state, features, values, serializer);
            }

            serializer ??= TypeModel.GetSerializer<TItem>(state.Model);
            var serializerFeatures = serializer.Features;
            if (serializerFeatures.IsRepeated()) TypeModel.ThrowNestedListsNotSupported(typeof(TItem));
            features.InheritFrom(serializerFeatures);

            if (features.HasAny(SerializerFeatures.OptionWrappedValue))
                features |= SerializerFeatures.OptionWrappedValueFieldPresence;

            var ctx = state.Context;
            values = Initialize(values, ctx);
            using var buffer = state.FillBuffer<ISerializer<TItem>, TItem>(features, serializer, features.DefaultFor<TItem>());
            if ((features & SerializerFeatures.OptionClearCollection) != 0) values = Clear(values, ctx);
            if (buffer.IsEmpty) return values;
            var segment = buffer.Segment;
            return AddRange(values, ref segment, ctx);
        }


        /// <summary>Ensure that the collection is not nil, if required</summary>
        protected virtual TCollection Initialize(TCollection values, ISerializationContext context) => values;

        /// <summary>Remove any existing contents from the collection</summary>
        protected abstract TCollection Clear(TCollection values, ISerializationContext context);

        /// <summary>Add new contents to the collection</summary>
        protected abstract TCollection AddRange(TCollection values, ref ArraySegment<TItem> newValues, ISerializationContext context);
        // note: not "in" because ArraySegment<T> is not "readonly" on all targeted TFMs
    }

    sealed class StackSerializer<TCollection, T> : RepeatedSerializer<TCollection, T>
        where TCollection : Stack<T>
    {
        protected override TCollection Initialize(TCollection values, ISerializationContext context)
            => values ?? TypeModel.ActivatorCreate<TCollection>();
        protected override TCollection Clear(TCollection values, ISerializationContext context)
        {
            values.Clear();
            return values;
        }

        protected override int TryGetCount(TCollection values) => values is null ? 0 : values.Count;

        protected override TCollection AddRange(TCollection values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            newValues.ReverseInPlace();
            foreach (var value in newValues.AsSpan())
                values.Push(value);
            return values;
        }
        internal override long Measure(TCollection values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
        {
            var iter = values.GetEnumerator();
            return Measure(ref iter, serializer, context, wireType);
        }

        internal override void WritePacked(ref ProtoWriter.State state, TCollection values, IMeasuringSerializer<T> serializer, WireType wireType)
        {
            var iter = values.GetEnumerator();
            WritePacked(ref state, ref iter, serializer, wireType);
        }

        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, TCollection values, ISerializer<T> serializer, SerializerFeatures features)
        {
            var iter = values.GetEnumerator();
            Write(ref state, fieldNumber, category, wireType, ref iter, serializer, features);
        }
    }

    sealed class ListSerializer<T> : ListSerializer<List<T>, T>
    {
        protected override List<T> Initialize(List<T> values, ISerializationContext context)
            => values ?? new List<T>();
    }
    class ListSerializer<TList, T> : RepeatedSerializer<TList, T>
        where TList : List<T>
    {
        protected override TList Initialize(TList values, ISerializationContext context)
            // note: don't call TypeModel.CreateInstance: *we are the factory*
            => values ?? TypeModel.ActivatorCreate<TList>();

        protected override TList Clear(TList values, ISerializationContext context)
        {
            values.Clear();
            return values;
        }
        protected override TList AddRange(TList values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            values.AddRange(newValues);
            return values;
        }

        protected override int TryGetCount(TList values) => values is null ? 0 : values.Count;

        internal override long Measure(TList values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
        {
            var iter = values.GetEnumerator();
            return Measure(ref iter, serializer, context, wireType);
        }
        internal override void WritePacked(ref ProtoWriter.State state, TList values, IMeasuringSerializer<T> serializer, WireType wireType)
        {
            var iter = values.GetEnumerator();
            WritePacked(ref state, ref iter, serializer, wireType);
        }
        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, TList values, ISerializer<T> serializer, SerializerFeatures features)
        {
            var iter = values.GetEnumerator();
            Write(ref state, fieldNumber, category, wireType, ref iter, serializer, features);
        }
    }

    class EnumerableSerializer<TCollection, TCreate, T> : RepeatedSerializer<TCollection, T>
        where TCollection : class, IEnumerable<T>
        where TCreate : TCollection
    {
        protected override TCollection Initialize(TCollection values, ISerializationContext context)
            // note: don't call TypeModel.CreateInstance: *we are the factory*
            => values ?? (typeof(TCreate).IsInterface ? (TCollection)(object)new List<T>() : TypeModel.ActivatorCreate<TCreate>());

        protected override int TryGetCount(TCollection values) => TryGetCountDefault(values); // don't trust them much

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
        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, TCollection values, ISerializer<T> serializer, SerializerFeatures features)
        {
            var iter = values.GetEnumerator();
            try
            {
                Write(ref state, fieldNumber, category, wireType, ref iter, serializer, features);
            }
            finally
            {
                iter?.Dispose();
            }
        }

        private static void ThrowInvalidCollectionType(object collection)
            => ThrowHelper.ThrowInvalidOperationException($"For repeated data declared as {typeof(TCollection).NormalizeName()}, the *underlying* collection ({collection?.GetType().NormalizeName()}) must implement ICollection<T> and must not declare itself read-only; alternative (more exotic) collections can be used, but must be declared using their well-known form (for example, a member could be declared as ImmutableHashSet<T>)");

        protected override TCollection Clear(TCollection values, ISerializationContext context)
        {
            if (values is ICollection<T> collection && !collection.IsReadOnly)
            {
                collection.Clear();
            }
            else
            {
                if (typeof(TCollection) == typeof(IEnumerable<T>))
                {
                    // this is a recognised case; we'll use the same default type as Initialize
                    values = Initialize(default, context);
                }
                else
                {
                    ThrowInvalidCollectionType(values);
                }
            }
            return values;
        }

        protected override TCollection AddRange(TCollection values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            switch (values)
            {
                case List<T> list:
                    list.AddRange(newValues);
                    break;
                case ICollection<T> collection when !collection.IsReadOnly:
                    foreach (var item in newValues.AsSpan())
                        collection.Add(item);
                    break;
                default:
                    ThrowInvalidCollectionType(values);
                    break;
            }
            return values;
        }
    }

    sealed class VectorSerializer<T> : RepeatedSerializer<T[], T>
    {
        protected override T[] Initialize(T[] values, ISerializationContext context)
            => values ?? Array.Empty<T>();
        protected override T[] Clear(T[] values, ISerializationContext context)
            => Array.Empty<T>();
        protected override T[] AddRange(T[] values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            var arr = new T[values.Length + newValues.Count];
            Array.Copy(values, 0, arr, 0, values.Length);
            Array.Copy(newValues.Array, newValues.Offset, arr, values.Length, newValues.Count);
            return arr;
        }
        protected override int TryGetCount(T[] values) => values is null ? 0 : values.Length;

        internal override long Measure(T[] values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
        {
            var iter = new Enumerator(values);
            return Measure(ref iter, serializer, context, wireType);
        }

        internal override void WritePacked(ref ProtoWriter.State state, T[] values, IMeasuringSerializer<T> serializer, WireType wireType)
        {
            var iter = new Enumerator(values);
            WritePacked(ref state, ref iter, serializer, wireType);
        }

        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, T[] values, ISerializer<T> serializer, SerializerFeatures features)
        {
            var iter = new Enumerator(values);
            Write(ref state, fieldNumber, category, wireType, ref iter, serializer, features);
        }

        [StructLayout(LayoutKind.Auto)]
        struct Enumerator : IEnumerator<T>
        {
            public readonly void Reset() => ThrowHelper.ThrowNotSupportedException();
            private readonly T[] _array;
            private int _index;
            public Enumerator(T[] array)
            {
                _array = array;
                _index = -1;
            }
            public readonly T Current => _array[_index];

            readonly object IEnumerator.Current => _array[_index];
            public bool MoveNext() => ++_index < _array.Length;
            public readonly void Dispose() { }
        }
    }

    sealed class QueueSerializer<TCollection, T> : RepeatedSerializer<TCollection, T>
        where TCollection : Queue<T>
    {
        protected override TCollection Initialize(TCollection values, ISerializationContext context)
            => values ?? TypeModel.ActivatorCreate<TCollection>();
        protected override TCollection Clear(TCollection values, ISerializationContext context)
        {
            values.Clear();
            return values;
        }

        protected override int TryGetCount(TCollection values) => values is null ? 0 : values.Count;

        protected override TCollection AddRange(TCollection values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            foreach (var value in newValues.AsSpan())
                values.Enqueue(value);
            return values;
        }

        internal override long Measure(TCollection values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
        {
            var iter = values.GetEnumerator();
            return Measure(ref iter, serializer, context, wireType);
        }
        internal override void WritePacked(ref ProtoWriter.State state, TCollection values, IMeasuringSerializer<T> serializer, WireType wireType)
        {
            var iter = values.GetEnumerator();
            WritePacked(ref state, ref iter, serializer, wireType);
        }
        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, TCollection values, ISerializer<T> serializer, SerializerFeatures features)
        {
            var iter = values.GetEnumerator();
            Write(ref state, fieldNumber, category, wireType, ref iter, serializer, features);
        }
    }

    sealed class SetSerializer<TCollection, T> : RepeatedSerializer<TCollection, T>
        where TCollection : ISet<T>
    {
        protected override TCollection Initialize(TCollection values, ISerializationContext context) =>
            values ?? (typeof(TCollection).IsInterface ? (TCollection)(object)new HashSet<T>() : TypeModel.ActivatorCreate<TCollection>());

        protected override TCollection Clear(TCollection values, ISerializationContext context)
        {
            values.Clear();
            return values;
        }

        protected override int TryGetCount(TCollection values) => values is null ? 0 : values.Count;

        protected override TCollection AddRange(TCollection values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            values.UnionWith(newValues);
            return values;
        }

        internal override long Measure(TCollection values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
        {
            var iter = values.GetEnumerator();
            return Measure(ref iter, serializer, context, wireType);
        }

        internal override void WritePacked(ref ProtoWriter.State state, TCollection values, IMeasuringSerializer<T> serializer, WireType wireType)
        {
            var iter = values.GetEnumerator();
            WritePacked(ref state, ref iter, serializer, wireType);
        }

        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, TCollection values, ISerializer<T> serializer, SerializerFeatures features)
        {
            var iter = values.GetEnumerator();
            Write(ref state, fieldNumber, category, wireType, ref iter, serializer, features);
        }
    }

#if NET6_0_OR_GREATER
    sealed class ReadOnlySetSerializer<T> : RepeatedSerializer<IReadOnlySet<T>, T>
    {
        protected override IReadOnlySet<T> Initialize(IReadOnlySet<T> values, ISerializationContext context) => values ?? new HashSet<T>();

        protected override IReadOnlySet<T> Clear(IReadOnlySet<T> values, ISerializationContext context)
        {
            if (values is ISet<T> target && !target.IsReadOnly)
            {
                target.Clear();
                return values;
            }
            return new HashSet<T>();
        }

        protected override int TryGetCount(IReadOnlySet<T> values) => values is null ? 0 : values.Count;

        protected override IReadOnlySet<T> AddRange(IReadOnlySet<T> values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            if (values is ISet<T> target && !target.IsReadOnly)
            {
                target.UnionWith(newValues);
                return values;
            }

            var resultSet = new HashSet<T>(values.Count + newValues.Count);
            resultSet.UnionWith(values);
            resultSet.UnionWith(newValues);
            return resultSet;
        }
        internal override long Measure(IReadOnlySet<T> values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
        {
            var iter = values.GetEnumerator();
            return Measure(ref iter, serializer, context, wireType);
        }

        internal override void WritePacked(ref ProtoWriter.State state, IReadOnlySet<T> values, IMeasuringSerializer<T> serializer, WireType wireType)
        {
            var iter = values.GetEnumerator();
            WritePacked(ref state, ref iter, serializer, wireType);
        }

        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, IReadOnlySet<T> values, ISerializer<T> serializer, SerializerFeatures features)
        {
            var iter = values.GetEnumerator();
            Write(ref state, fieldNumber, category, wireType, ref iter, serializer, features);
        }
    }
#endif
}
