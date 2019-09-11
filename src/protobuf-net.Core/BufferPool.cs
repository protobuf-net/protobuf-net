using System;
using System.Buffers;

namespace ProtoBuf
{
    internal sealed class BufferPool
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
            Helpers.DebugAssert(buffer != null);
            Helpers.DebugAssert(toFitAtLeastBytes > buffer.Length);
            Helpers.DebugAssert(copyFromIndex >= 0);
            Helpers.DebugAssert(copyBytes >= 0);

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

            var newBuffer = GetCachedBuffer(toFitAtLeastBytes) ?? new byte[newLength];

            if (copyBytes > 0)
            {
                Buffer.BlockCopy(buffer, copyFromIndex, newBuffer, 0, copyBytes);
                ReleaseBufferToPool(ref buffer);
            }

            buffer = newBuffer;
        }

        internal static void ReleaseBufferToPool(ref byte[] buffer)
        {
            var tmp = buffer;
            buffer = null;
            if (tmp != null) _pool.Return(tmp);
        }
    }
}
