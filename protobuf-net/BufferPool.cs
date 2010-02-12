
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
        internal static void ReleaseBufferToPool(ref byte[] buffer)
        {
            if (buffer == null) return;
            try
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
            finally
            {
                // if no space, just drop it on the floor
                buffer = null;
            }
        }

    }
}
