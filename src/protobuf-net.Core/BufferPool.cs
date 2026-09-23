using System;
using System.Buffers;
using System.Diagnostics;

namespace ProtoBuf
{
    internal static class BufferPool
    {
        private static readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

        internal const int BUFFER_LENGTH = 1024;

        internal static byte[] GetBuffer() => GetBuffer(BUFFER_LENGTH);

        internal static byte[] GetBuffer(int minSize)
        {
            byte[] cachedBuff = GetCachedBuffer(minSize);
            return cachedBuff ?? new byte[minSize];
        }

        internal static byte[] GetCachedBuffer(int minSize) => _pool.Rent(minSize);

        // https://docs.microsoft.com/en-us/dotnet/framework/configure-apps/file-schema/runtime/gcallowverylargeobjects-element
        private const int MaxByteArraySize = int.MaxValue - 56;

        internal static void ResizeAndFlushLeft(ref byte[] buffer, int toFitAtLeastBytes, int copyFromIndex, int copyBytes)
        {
            Debug.Assert(buffer is not null);
            Debug.Assert(toFitAtLeastBytes > buffer.Length);
            Debug.Assert(copyFromIndex >= 0);
            Debug.Assert(copyBytes >= 0);

            int newLength = buffer.Length * 2;
            if (newLength < 0)
            {
                newLength = MaxByteArraySize;
            }

            if (newLength < toFitAtLeastBytes) newLength = toFitAtLeastBytes;

            if (copyBytes == 0)
            {
                ReleaseBufferToPool(ref buffer);
            }

            var newBuffer = GetCachedBuffer(newLength) ?? new byte[newLength];

            if (copyBytes > 0)
            {
                Buffer.BlockCopy(buffer, copyFromIndex, newBuffer, 0, copyBytes);
                ReleaseBufferToPool(ref buffer);
            }

            buffer = newBuffer;
        }

        /// <summary>
        /// Returns <paramref name="buffer"/> to the pool and clears the caller's reference.
        /// </summary>
        /// <remarks>
        /// The parameter is deliberately NOT <c>ref byte[]?</c>: callers hold the buffer in a field
        /// that is non-null for the whole life of an active object, and widening it here would push
        /// a null check onto every one of their uses. The null written back is the pooled-object
        /// convention - the field is null only between teardown and the next rent - so it is stated
        /// once, here, with <c>null!</c>.
        /// </remarks>
        internal static void ReleaseBufferToPool(ref byte[] buffer)
        {
            var tmp = buffer;
            buffer = null!;
            if (tmp is not null) _pool.Return(tmp);
        }
    }
}
