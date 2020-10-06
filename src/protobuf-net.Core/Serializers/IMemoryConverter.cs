using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProtoBuf.Serializers
{
    /// <summary>
    /// Provides an abstract way of referring to simple range-based
    /// data types as Memory<typeparamref name="TElement"/>.
    /// </summary>
    public interface IMemoryConverter<TStorage, TElement>
    {
        /// <summary>
        /// Provides a non-null value from the provided storage.
        /// For many value-types, this will simply return the input value. For
        /// reference-types, the input should be null-coalesced against an
        /// empty value such as Array.Empty<typeparamref name="TStorage"/>().
        /// </summary>
        TStorage NonNull(in TStorage value);

        /// <summary>
        /// Get the length (in terms of element count) of the provided storage.
        /// </summary>
        int GetLength(in TStorage value);

        /// <summary>
        /// Access a Memory<typeparamref name="TElement"/> that is the underlying
        /// data held by this storage.
        /// </summary>
        Memory<TElement> GetMemory(in TStorage value);

        /// <summary>
        /// Resizes (typically: allocates and copies) the provided storage by
        /// the requested additional capacity, returning a memory to *just
        /// the additional portion*). The implementor is responsible for
        /// ensuring that the old values are copied if necessary.
        /// The implementor may choose to recycle the old storage, if
        /// appropriate.
        /// </summary>
        Memory<TElement> Expand(ISerializationContext context, ref TStorage value, int additionalCapacity);
    }

    /// <summary>
    /// Provides a memory converter implementation for many common storage kinds.
    /// </summary>
    public sealed class DefaultMemoryConverter<T> :
        IMemoryConverter<T[], T>,
        IMemoryConverter<ArraySegment<T>, T>,
        IMemoryConverter<Memory<T>, T>,
        IMemoryConverter<ReadOnlyMemory<T>, T>
    {
        [MethodImpl(ProtoReader.HotPath)]
        internal static IMemoryConverter<TStorage, T> GetFor<TStorage>(TypeModel model)
            => model?.GetSerializerCore<TStorage>(default) as IMemoryConverter<TStorage, T>
            ?? Instance as IMemoryConverter<TStorage, T> ?? NotSupported<TStorage>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IMemoryConverter<TStorage, T> NotSupported<TStorage>()
        {
            ThrowHelper.ThrowInvalidOperationException(
                $"No memory-converter is available for storage {typeof(TStorage).NormalizeName()} with element-type {typeof(T).NormalizeName()}.");
            return default;
        }


        /// <summary>
        /// Provides the singleton instance for element type <typeparamref name="T"/>.
        /// </summary>
        public static DefaultMemoryConverter<T> Instance { get; }
            = new DefaultMemoryConverter<T>();
        private DefaultMemoryConverter() { }

        T[] IMemoryConverter<T[], T>.NonNull(in T[] value) => value ?? Array.Empty<T>();
        int IMemoryConverter<T[], T>.GetLength(in T[] value) => value is null ? 0 : value.Length;

        Memory<T> IMemoryConverter<T[], T>.GetMemory(in T[] value) => new Memory<T>(value);

        Memory<T> IMemoryConverter<T[], T>.Expand(ISerializationContext context, ref T[] value, int additionalCapacity)
        {
            int oldCapacity = value is null ? 0 : value.Length;
            Array.Resize<T>(ref value, oldCapacity + additionalCapacity);
            return new Memory<T>(value, oldCapacity, additionalCapacity);
        }

        ArraySegment<T> IMemoryConverter<ArraySegment<T>, T>.NonNull(in ArraySegment<T> value) => value;

        int IMemoryConverter<ArraySegment<T>, T>.GetLength(in ArraySegment<T> value) => value.Count;

        Memory<T> IMemoryConverter<ArraySegment<T>, T>.GetMemory(in ArraySegment<T> value)
            => new Memory<T>(value.Array, value.Offset, value.Count);

        Memory<T> IMemoryConverter<ArraySegment<T>, T>.Expand(ISerializationContext context, ref ArraySegment<T> value, int additionalCapacity)
        {
            // we can't expand into the segment, because we don't know what else is using it;
            // so: allocate an entire array
            int oldCount = value.Count;
            var arr = new T[oldCount + additionalCapacity];
            Array.Copy(value.Array, value.Offset, arr, 0, oldCount);
            value = new ArraySegment<T>(arr);
            return new Memory<T>(arr, oldCount, additionalCapacity);
        }

        Memory<T> IMemoryConverter<Memory<T>, T>.NonNull(in Memory<T> value) => value;

        int IMemoryConverter<Memory<T>, T>.GetLength(in Memory<T> value) => value.Length;

        Memory<T> IMemoryConverter<Memory<T>, T>.GetMemory(in Memory<T> value) => value;

        Memory<T> IMemoryConverter<Memory<T>, T>.Expand(ISerializationContext context, ref Memory<T> value, int additionalCapacity)
        {
            var oldValue = value;
            value = new T[oldValue.Length + additionalCapacity];
            oldValue.CopyTo(value);
            return value.Slice(oldValue.Length);
        }

        ReadOnlyMemory<T> IMemoryConverter<ReadOnlyMemory<T>, T>.NonNull(in ReadOnlyMemory<T> value) => value;

        int IMemoryConverter<ReadOnlyMemory<T>, T>.GetLength(in ReadOnlyMemory<T> value) => value.Length;

        Memory<T> IMemoryConverter<ReadOnlyMemory<T>, T>.GetMemory(in ReadOnlyMemory<T> value)
            => MemoryMarshal.AsMemory(value);

        Memory<T> IMemoryConverter<ReadOnlyMemory<T>, T>.Expand(ISerializationContext context, ref ReadOnlyMemory<T> value, int additionalCapacity)
        {
            int oldLength = value.Length;
            Memory<T> newValue = new T[oldLength + additionalCapacity];
            value.CopyTo(newValue);
            value = newValue;
            return newValue.Slice(oldLength);
        }
    }
}
