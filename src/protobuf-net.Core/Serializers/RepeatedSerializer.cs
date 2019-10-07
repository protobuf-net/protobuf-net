using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Serializers
{
    /// <summary>
    /// Provides utility methods for creating serializers for repeated data
    /// </summary>
    public static class RepeatedSerializer
    {
        /// <summary>Create a serializer that operates on lists</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<List<T>, T> CreateList<T>()
            => SerializerCache<ListSerializer<T>>.InstanceField;

        /// <summary>Create a serializer that operates on lists</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<TList, T> CreateList<TList, T>()
            where TList : List<T>
            => SerializerCache<ListSerializer<TList, T>>.InstanceField;

        /// <summary>Create a map serializer that operates on dictionaries</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static MapSerializer<Dictionary<TKey, TValue>, TKey, TValue> CreateDictionary<TKey, TValue>()
            => SerializerCache<DictionarySerializer<TKey, TValue>>.InstanceField;

        /// <summary>Create a map serializer that operates on dictionaries</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static MapSerializer<TCollection, TKey, TValue> CreateDictionary<TCollection, TKey, TValue>()
            where TCollection : IDictionary<TKey, TValue>
            => SerializerCache<DictionarySerializer<TCollection, TKey, TValue>>.InstanceField;

        /// <summary>Create a serializer that operates on lists</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<TCollection, T> CreateCollection<TCollection, T>()
            where TCollection : ICollection<T>
            => SerializerCache<CollectionSerializer<TCollection, TCollection, T>>.InstanceField;

        /// <summary>Create a serializer that operates on lists</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<TCollection, T> CreateCollection<TCollection, TCreate, T>()
            where TCollection : ICollection<T>
            where TCreate : TCollection
            => SerializerCache<CollectionSerializer<TCollection, TCreate, T>>.InstanceField;

        /// <summary>Create a serializer that operates on lists</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<T[], T> CreateVector<T>()
            => SerializerCache<VectorSerializer<T>>.InstanceField;

        /// <summary>Create a serializer that operates on lists</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<Queue<T>, T> CreateQueue<T>()
            => SerializerCache<QueueSerializer<T>>.InstanceField;

        /// <summary>Create a serializer that operates on lists</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<Stack<T>, T> CreateStack<T>()
            => SerializerCache<StackSerializer<T>>.InstanceField;

        /// <summary>Create a serializer that operates on lists</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<ImmutableArray<T>, T> CreateImmutableArray<T>()
            => SerializerCache<ImmutableArraySerializer<T>>.InstanceField;

        /// <summary>Create a map serializer that operates on immutable dictionaries</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static MapSerializer<ImmutableDictionary<TKey, TValue>, TKey, TValue> CreateImmutableDictionary<TKey, TValue>()
            => SerializerCache<ImmutableDictionarySerializer<TKey, TValue>>.InstanceField;

        /// <summary>Create a map serializer that operates on immutable dictionaries</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static MapSerializer<ImmutableSortedDictionary<TKey, TValue>, TKey, TValue> CreateImmutableSortedDictionary<TKey, TValue>()
            => SerializerCache<ImmutableSortedDictionarySerializer<TKey, TValue>>.InstanceField;

        /// <summary>Create a map serializer that operates on immutable dictionaries</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static MapSerializer<IImmutableDictionary<TKey, TValue>, TKey, TValue> CreateIImmutableDictionary<TKey, TValue>()
            => SerializerCache<ImmutableIDictionarySerializer<TKey, TValue>>.InstanceField;

        internal static readonly MethodInfo[] s_methods = typeof(RepeatedSerializer).GetMethods(BindingFlags.Static | BindingFlags.Public);

        internal static MemberInfo GetProvider(Type type, out bool isMap)
        {
            isMap = false;
            if (!TypeHelper.ResolveUniqueEnumerableT(type, out var t)) return null;

            if (type.IsArray) return Resolve(nameof(CreateVector), t);

            var provider = GetProvider(type, type, ref isMap);
            if (provider != null) return provider;
            if (type.IsClass)
            {   // try for inheritance, i.e. Foo : List<int>
                Type current = type;
                while (current != null && current != typeof(object))
                {
                    provider = GetProvider(current, type, ref isMap);
                    if (provider != null) return provider;
                    current = current.BaseType;
                }
            }

            if (type.IsGenericType)
            {   // things that don't withstand inheritance
                var genDef = type.GetGenericTypeDefinition();
                if (genDef == typeof(ImmutableArray<>))
                    return Resolve(nameof(CreateImmutableArray), t);

                if (genDef == typeof(ImmutableDictionary<,>))
                {
                    isMap = true;
                    return Resolve(nameof(CreateImmutableDictionary), type.GetGenericArguments());
                }
                if (genDef == typeof(ImmutableSortedDictionary<,>))
                {
                    isMap = true;
                    return Resolve(nameof(CreateImmutableSortedDictionary), type.GetGenericArguments());
                }
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                var kArgs = t.GetGenericArguments();

                var iDict = typeof(IDictionary<,>).MakeGenericType(kArgs);
                if (iDict.IsAssignableFrom(type))
                {
                    isMap = true;
                    return Resolve(nameof(CreateDictionary), type, kArgs[0], kArgs[1]);
                }

                iDict = typeof(IImmutableDictionary<,>).MakeGenericType(kArgs);
                if (iDict.IsAssignableFrom(type))
                {
                    isMap = true;
                    return Resolve(nameof(CreateIImmutableDictionary), type, kArgs[0], kArgs[1]);
                }
            }

            var iColT = typeof(ICollection<>).MakeGenericType(t);
            if (iColT.IsAssignableFrom(type)) return Resolve(nameof(CreateCollection), type, t);

            return null;
        }
        private static MemberInfo GetProvider(Type root, Type current, ref bool isMap)
        {
            if (current.IsGenericType)
            {
                var genDef = current.GetGenericTypeDefinition();
                if (genDef == typeof(List<>))
                {
                    var genArgs = current.GetGenericArguments();
                    return Resolve(nameof(CreateList),
                        current == root ? new Type[] { genArgs[0] } : new Type[] { root, genArgs[0] });
                }

                if (genDef == typeof(Dictionary<,>))
                {
                    var genArgs = current.GetGenericArguments();
                    isMap = true;
                    return Resolve(nameof(CreateDictionary),
                        current == root ? new Type[] { genArgs[0], genArgs[1] } : new Type[] { root, genArgs[0], genArgs[1] });
                }
            }
            return null;
        }


        static MemberInfo Resolve(string methodName, params Type[] genericArgs)
        {
            foreach (var method in s_methods)
            {
                if (method.Name == methodName)
                {
                    if (!method.IsGenericMethod)
                    {
                        return genericArgs.Length == 0 ? method : null;
                    }
                    if (method.GetGenericArguments().Length == genericArgs.Length)
                        return method.MakeGenericMethod(genericArgs);
                }
            }
            return null;
        }

        /// <summary>Reverses a range of values</summary>
        [MethodImpl(ProtoReader.HotPath)]
        internal static void Reverse<T>(ArraySegment<T> values) => Array.Reverse(values.Array, values.Offset, values.Count);

        /// <summary>Obtains a range of values as a span</summary>
        [MethodImpl(ProtoReader.HotPath)]
        internal static Span<T> AsSpan<T>(ArraySegment<T> values) => new Span<T>(values.Array, values.Offset, values.Count);

        /// <summary>Obtains a range of values as an enumerable sequence</summary>
        [MethodImpl(ProtoReader.HotPath)]
        internal static IEnumerable<T> AsEnumerable<T>(ArraySegment<T> values) => values;

        [MethodImpl(ProtoReader.HotPath)]
        internal static IEnumerable<TItem> AsEnumerable<TCollection, TItem>(TCollection values)
        {
            var sequence = values as IEnumerable<TItem>;
            if (sequence is null) ThrowHelper.ThrowInvalidOperationException(
                $"Unusual collection requires custom implementation: {typeof(TCollection).NormalizeName()}");
            return sequence;
        }
    }


    /// <summary>
    /// Base class for simple collection serializers
    /// </summary>
    public abstract class RepeatedSerializer<TCollection, TItem> : IRepeatedSerializer<TCollection>
    {
        SerializerFeatures ISerializer<TCollection>.Features => SerializerFeatures.CategoryRepeated;

        TCollection ISerializer<TCollection>.Read(ref ProtoReader.State state, TCollection value)
        {
            ThrowHelper.ThrowInvalidOperationException("Should have used " + nameof(IRepeatedSerializer<TCollection>.ReadRepeated));
            return default;
        }

        void ISerializer<TCollection>.Write(ref ProtoWriter.State state, TCollection value)
            => ThrowHelper.ThrowInvalidOperationException("Should have used " + nameof(IRepeatedSerializer<TCollection>.WriteRepeated));

        void IRepeatedSerializer<TCollection>.WriteRepeated(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures features, TCollection values)
            => WriteRepeated(ref state, fieldNumber, features, values, default);

        /// <summary>
        /// Serialize a sequence of values to the supplied writer
        /// </summary>
        public void WriteRepeated(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures features, TCollection values, ISerializer<TItem> serializer = null)
        {
            serializer ??= TypeModel.GetSerializer<TItem>(state.Model);
            var serializerFeatures = serializer.Features;
            if (serializerFeatures.IsRepeated()) TypeModel.ThrowNestedListsNotSupported(typeof(TItem));

            int count = TryGetCount(values);
            if (count == 0) return;

            var category = serializerFeatures.GetCategory();
            var wireType = features.GetWireType();
            if (TypeHelper<TItem>.CanBePacked && !features.IsPackedDisabled() && count > 1 && serializer is IMeasuringSerializer<TItem> measurer)
            {
                if (category != SerializerFeatures.CategoryScalar) serializerFeatures.ThrowInvalidCategory();
                WritePacked(ref state, fieldNumber, wireType, values, count, measurer);
            }
            else
            {
                Write(ref state, fieldNumber, category, wireType, values, serializer);
            }
        }

        internal virtual void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, TCollection values, ISerializer<TItem> serializer)
        {
            var writer = state.GetWriter();
            foreach (var value in RepeatedSerializer.AsEnumerable<TCollection, TItem>(values))
            {
                if (TypeHelper<TItem>.CanBeNull && value is null) ThrowHelper.ThrowNullReferenceException<TItem>();
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

        internal virtual long Measure(TCollection values, IMeasuringSerializer<TItem> serializer, ISerializationContext context, WireType wireType)
        {
            long length = 0;
            foreach (var item in RepeatedSerializer.AsEnumerable<TCollection, TItem>(values))
            {
                length += serializer.Measure(context, wireType, item);
            }
            return length;
        }

        internal virtual void WritePacked(ref ProtoWriter.State state, TCollection values, IMeasuringSerializer<TItem> serializer, WireType wireType)
        {
            foreach (var value in RepeatedSerializer.AsEnumerable<TCollection, TItem>(values))
            {
                if (TypeHelper<TItem>.CanBeNull && value is null) ThrowHelper.ThrowNullReferenceException<TItem>();
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
                    ThrowHelper.ThrowInvalidOperationException($"Invalid wire-type for packed encoding: {wireType}");
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
        protected virtual int TryGetCount(TCollection value) => value switch
        {
            IReadOnlyCollection<TItem> collection => collection.Count,
            null => 0,
            _ => -1,
        };

        TCollection IRepeatedSerializer<TCollection>.ReadRepeated(ref ProtoReader.State state, SerializerFeatures features, TCollection values)
            => ReadRepeated(ref state, features, values, default);

        /// <summary>
        /// Deserializes a sequence of values from the supplied reader
        /// </summary>
        public TCollection ReadRepeated(ref ProtoReader.State state, SerializerFeatures features, TCollection values, ISerializer<TItem> serializer = null)
        {
            serializer ??= TypeModel.GetSerializer<TItem>(state.Model);
            var serializerFeatures = serializer.Features;
            if (serializerFeatures.IsRepeated()) TypeModel.ThrowNestedListsNotSupported(typeof(TItem));

            var ctx = state.Context;
            values = Initialize(values, ctx);
            using var buffer = state.FillBuffer<TItem>(features, serializer, TypeHelper<TItem>.Default);
            if ((features & SerializerFeatures.OptionClearCollection) != 0) values = Clear(values, ctx);
            return buffer.IsEmpty ? values : AddRange(values, buffer.Segment, ctx);
        }


        /// <summary>Ensure that the collection is not nil, if required</summary>
        protected virtual TCollection Initialize(TCollection values, ISerializationContext context) => values;

        /// <summary>Remove any existing contents from the collection</summary>
        protected abstract TCollection Clear(TCollection values, ISerializationContext context);

        /// <summary>Add new contents to the collection</summary>
        protected abstract TCollection AddRange(TCollection values, ArraySegment<TItem> newValues, ISerializationContext context);
    }

    sealed class StackSerializer<T> : RepeatedSerializer<Stack<T>, T>
    {
        protected override Stack<T> Initialize(Stack<T> values, ISerializationContext context)
            => values ?? new Stack<T>();
        protected override Stack<T> Clear(Stack<T> values, ISerializationContext context)
        {
            values.Clear();
            return values;
        }
        protected override Stack<T> AddRange(Stack<T> values, ArraySegment<T> newValues, ISerializationContext context)
        {
            RepeatedSerializer.Reverse(newValues);
            foreach (var value in RepeatedSerializer.AsSpan(newValues))
                values.Push(value);
            return values;
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
            => values ?? TypeModel.CreateInstance<TList>(context, this);

        protected override TList Clear(TList values, ISerializationContext context)
        {
            values.Clear();
            return values;
        }
        protected override TList AddRange(TList values, ArraySegment<T> newValues, ISerializationContext context)
        {
            values.AddRange(RepeatedSerializer.AsEnumerable(newValues));
            return values;
        }
    }

    sealed class CollectionSerializer<TCollection, TCreate, T> : RepeatedSerializer<TCollection, T>
        where TCollection : ICollection<T>
        where TCreate : TCollection
    {
        protected override TCollection Initialize(TCollection values, ISerializationContext context)
            => values ?? (typeof(TCreate).IsInterface ? (TCollection)(object)new List<T>() : TypeModel.CreateInstance<TCreate>(context));
        protected override TCollection Clear(TCollection values, ISerializationContext context)
        {
            values.Clear();
            return values;
        }
        protected override TCollection AddRange(TCollection values, ArraySegment<T> newValues, ISerializationContext context)
        {
            switch (values)
            {
                case List<T> list:
                    list.AddRange(RepeatedSerializer.AsEnumerable(newValues));
                    break;
                default:
                    foreach (var item in RepeatedSerializer.AsSpan(newValues))
                        values.Add(item);
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
        protected override T[] AddRange(T[] values, ArraySegment<T> newValues, ISerializationContext context)
        {
            var arr = new T[values.Length + newValues.Count];
            Array.Copy(values, 0, arr, 0, values.Length);
            Array.Copy(newValues.Array, newValues.Offset, arr, values.Length, newValues.Count);
            return arr;
        }
        protected override int TryGetCount(T[] value) => value is null ? 0 : value.Length;
    }

    sealed class QueueSerializer<T> : RepeatedSerializer<Queue<T>, T>
    {
        protected override Queue<T> Initialize(Queue<T> values, ISerializationContext context)
            => values ?? new Queue<T>();
        protected override Queue<T> Clear(Queue<T> values, ISerializationContext context)
        {
            values.Clear();
            return values;
        }
        protected override Queue<T> AddRange(Queue<T> values, ArraySegment<T> newValues, ISerializationContext context)
        {
            foreach (var value in RepeatedSerializer.AsSpan(newValues))
                values.Enqueue(value);
            return values;
        }
    }

    sealed class ImmutableArraySerializer<T> : RepeatedSerializer<ImmutableArray<T>, T>
    {
        protected override ImmutableArray<T> Initialize(ImmutableArray<T> values, ISerializationContext context)
            => values.IsDefault ? ImmutableArray<T>.Empty : values;
        protected override ImmutableArray<T> Clear(ImmutableArray<T> values, ISerializationContext context)
            => values.Clear();
        protected override ImmutableArray<T> AddRange(ImmutableArray<T> values, ArraySegment<T> newValues, ISerializationContext context)
            => values.AddRange(RepeatedSerializer.AsEnumerable(newValues));

        protected override int TryGetCount(ImmutableArray<T> value) => value.IsEmpty ? 0 : value.Length;
    }
}
