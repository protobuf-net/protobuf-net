
using System.Threading;
namespace ProtoBuf
{
    internal class BufferPool
    {
        private BufferPool() { }
        const int PoolSize = 20;
        internal const int BufferLength = 1024;
        private static readonly object[] pool = new object[PoolSize];

        internal static byte[] GetBuffer()
        {
            for (int i = 0; i < pool.Length; i++)
            {
                object tmp;
                if ((tmp = Interlocked.Exchange(ref pool[i], null)) != null) return (byte[])tmp;
            }
            return new byte[BufferLength];
        }
        internal static void ResizeAndFlushLeft(ref byte[] buffer, int toFitAtLeastBytes, int copyFromIndex, int copyBytes)
        {
            Helpers.DebugAssert(buffer != null);
            Helpers.DebugAssert(toFitAtLeastBytes > buffer.Length);
            Helpers.DebugAssert(copyFromIndex >= 0);
            Helpers.DebugAssert(copyBytes >= 0);

            // try doubling, else match
            int newLength = buffer.Length * 2;
            if (newLength < toFitAtLeastBytes) newLength = toFitAtLeastBytes;

            byte[] newBuffer = new byte[newLength];
            if (copyBytes > 0)
            {
                Helpers.BlockCopy(buffer, copyFromIndex, newBuffer, 0, copyBytes);
            }
            if (buffer.Length == BufferPool.BufferLength)
            {
                BufferPool.ReleaseBufferToPool(ref buffer);
            }
            buffer = newBuffer;
        }
        internal static void ReleaseBufferToPool(ref byte[] buffer)
        {
            if (buffer == null) return;
            if (buffer.Length == BufferLength)
            {
                for (int i = 0; i < pool.Length; i++)
                {
                    if (Interlocked.CompareExchange(ref pool[i], buffer, null) == null)
                    {
                        break; // found a null; swapped it in
                    }
                }
            }
            // if no space, just drop it on the floor
            buffer = null;
        }

    }
}
