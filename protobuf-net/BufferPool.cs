
namespace ProtoBuf
{
    internal class BufferPool
    {
        private BufferPool() { }
        const int PoolSize = 20;
        internal const int BufferLength = 1024;
        private static readonly byte[][] pool = new byte[PoolSize][];

        internal static byte[] GetBuffer()
        {
            lock (pool)
            {
                for (int i = 0; i < pool.Length; i++)
                {
                    if (pool[i] != null)
                    {
                        byte[] result = pool[i];
                        pool[i] = null;
                        return result;
                    }
                }
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
            try
            {
                if (buffer.Length == BufferLength)
                {
                    lock (pool)
                    {
                        for (int i = 0; i < pool.Length; i++)
                        {
                            if (pool[i] == null)
                            {
                                pool[i] = buffer;
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                // if no space, just drop it on the floor
                buffer = null;
            }
        }

    }
}
