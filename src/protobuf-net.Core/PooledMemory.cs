using ProtoBuf.Serializers;
using System;
using System.Buffers;
using System.Runtime.InteropServices;
#nullable enable
namespace ProtoBuf
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("", "CA2231", Justification = "mirror Memory<T>")] // implement ==/!= and match Equals
    public readonly struct PooledMemory<T> : IDisposable, IEquatable<PooledMemory<T>>
    {
        private readonly Action<Memory<T>>? _dispose;
        public Memory<T> Memory { get; }
        public int Length => Memory.Length;
        public bool IsEmpty => Memory.IsEmpty;

        // empty and no dispose required
        internal bool IsTrivial => Memory.IsEmpty && _dispose is null;

        public void Dispose() => _dispose?.Invoke(Memory);

        public PooledMemory(Memory<T> value)
        {
            Memory = value;
            _dispose = null;
        }
        public PooledMemory(Memory<T> value, Action<Memory<T>>? dispose)
        {
            Memory = value;
            _dispose = dispose;
        }

        public static implicit operator PooledMemory<T>(T[] value) => new PooledMemory<T>(value);
        public static implicit operator PooledMemory<T>(Memory<T> value) => new PooledMemory<T>(value);
        public static implicit operator PooledMemory<T>(ArraySegment<T> value) => new PooledMemory<T>(value);
        public static implicit operator Memory<T>(PooledMemory<T> value) => value.Memory;
        public static implicit operator ReadOnlyMemory<T>(PooledMemory<T> value) => value.Memory;

        // convenience API for consumers to obtain buffers (right-size) that use the default array-pool
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

        public override int GetHashCode() => Memory.GetHashCode();
        public override string ToString() => Memory.ToString();
        public override bool Equals(object? obj)
            => obj is PooledMemory<T> other && Equals(other);

        public bool Equals(PooledMemory<T> other) => Memory.Equals(other.Memory);
    }
}
