using ProtoBuf.Serializers;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Runtime.InteropServices;
#nullable enable
namespace ProtoBuf
{
    /// <summary>
    /// Functionally identical to <see cref="Memory{T}"/>, but disposable - with dispose intended to represent pool reuse.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("", "CA2231", Justification = "mirror Memory<T>")] // implement ==/!= and match Equals
    public readonly struct PooledMemory<T> : IDisposable, IEquatable<PooledMemory<T>>
    {
        private readonly Action<Memory<T>>? _dispose;
        
        /// <summary>
        /// Gets the underlying memory represented by this instance.
        /// </summary>
        public Memory<T> Memory { get; }

        /// <summary>
        /// Gets the underlying span represented by this instance.
        /// </summary>
        public Span<T> Span => Memory.Span;

        /// <inheritdoc cref="Memory{T}.Length"/>
        public int Length => Memory.Length;
        /// <inheritdoc cref="Memory{T}.IsEmpty"/>
        public bool IsEmpty => Memory.IsEmpty;

        /// <summary>
        /// Indicates whether the value is empty and no dispose required
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsTrivial => Memory.IsEmpty && _dispose is null;

        /// <summary>
        /// Release all resources associated with this instance.
        /// </summary>
        public void Dispose() => _dispose?.Invoke(Memory);

        /// <summary>
        /// Create a new instance without any dispose/release behaviour.
        /// </summary>
        public PooledMemory(Memory<T> value)
        {
            Memory = value;
            _dispose = null;
        }

        /// <summary>
        /// Create a new instance, optionally with a dispose/release behaviour.
        /// </summary>
        public PooledMemory(Memory<T> value, Action<Memory<T>>? dispose)
        {
            Memory = value;
            _dispose = dispose;
        }

        /// <summary>
        /// Create a new instance without any dispose/release behaviour.
        /// </summary>
        public static implicit operator PooledMemory<T>(T[] value) => new PooledMemory<T>(value);
        /// <summary>
        /// Create a new instance without any dispose/release behaviour.
        /// </summary>
        public static implicit operator PooledMemory<T>(Memory<T> value) => new PooledMemory<T>(value);
        /// <summary>
        /// Create a new instance without any dispose/release behaviour.
        /// </summary>
        public static implicit operator PooledMemory<T>(ArraySegment<T> value) => new PooledMemory<T>(value);
        /// <summary>
        /// Gets the memory associated with this instance.
        /// </summary>
        public static implicit operator Memory<T>(PooledMemory<T> value) => value.Memory;
        /// <summary>
        /// Gets the memory associated with this instance.
        /// </summary>
        public static implicit operator ReadOnlyMemory<T>(PooledMemory<T> value) => value.Memory;

        /// <summary>
        /// Rent a memory chunk from the default array pool, that gets returned when disposed.
        /// </summary>
        public static PooledMemory<T> Rent(int size)
        {
            if (size == 0) return default;
            if (size < 0) Throw();
            var oversized = ArrayPool<T>.Shared.Rent(size);
            return new PooledMemory<T>(new Memory<T>(oversized, 0, size), s_DefaultPoolDispose);

            static void Throw() => throw new ArgumentOutOfRangeException(nameof(size));
        }

        private static readonly Action<Memory<T>> s_DefaultPoolDispose = value =>
        {
            if (MemoryMarshal.TryGetArray<T>(value, out var segment) && segment.Array is not null)
            {
                ArrayPool<T>.Shared.Return(segment.Array);
            }
        };

        /// <inheritdoc cref="object.GetHashCode"/>
        public override int GetHashCode() => Memory.GetHashCode();
        /// <inheritdoc cref="object.ToString"/>
        public override string ToString() => Memory.ToString();
        /// <inheritdoc cref="object.Equals(object)"/>
        public override bool Equals(object? obj)
            => obj is PooledMemory<T> other && Equals(other);
        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(PooledMemory<T> other) => Memory.Equals(other.Memory);
    }
}
