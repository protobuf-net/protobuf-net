using System;
using System.IO;
using System.Threading;

namespace ProtoBuf
{
    internal sealed class BufferPool
    {
        internal static void Flush()
        {
            lock (pool)
            {
                for (int i = 0; i < pool.Length; i++) pool[i] = null;
            }
        }

        private BufferPool() { }
        const int PoolSize = 20;
        internal const int BufferLength = 1024;
        private static readonly CachedBuffer[] pool = new CachedBuffer[PoolSize];

        internal static byte[] GetBuffer()
        {
            return GetBuffer(BufferLength);
        }

        /// <summary>
        /// Gets a buffer with a minimum size of <paramref name="minSize"/>
        /// </summary>
        /// <remarks>
        /// The method returns the smallest cached buffer with a size greater than <paramref name="minSize"/> or a new buffer if none was found.
        /// </remarks>
        internal static byte[] GetBuffer(int minSize)
        {
            byte[] cachedBuff = GetCachedBuffer(minSize);
            return cachedBuff ?? new byte[minSize];
        }

        /// <summary>
        /// Gets a cached buffer with a minimum size of <paramref name="minSize"/>.
        /// </summary>
        /// <remarks>
        /// The method returns the smallest cached buffer with a size greater than <paramref name="minSize"/> or <c>null</c> if none was found.
        /// </remarks>
        internal static byte[] GetCachedBuffer(int minSize)
        {
            if (minSize <= 0)
            {
                throw new ArgumentOutOfRangeException("minSize");
            }

            lock (pool)
            {
                int bestIndex = -1;
                byte[] bestMatch = null;
                for (int i = 0; i < pool.Length; i++)
                {
                    CachedBuffer buffer = pool[i];
                    if (buffer == null || buffer.Size < minSize)
                    {
                        continue;   // This buffer is useless: either null or too small
                    }
                    else if (bestMatch != null && bestMatch.Length < buffer.Size)
                    {
                        continue;   // We already have a smaller fitting buffer.
                    }

                    byte[] tmp = buffer.Buffer; // Get a reference to the cached byte array, check if it has already been collected.
                    if (tmp == null)
                    {
                        pool[i] = null; // Already collected, we can forget it
                    }
                    else
                    {
                        bestMatch = tmp;
                        bestIndex = i;
                    }
                }

                if (bestIndex >= 0)
                {
                    pool[bestIndex] = null;
                }

                return bestMatch;
            }
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

            try
            {
                if (copyBytes == 0)
                {
                    BufferPool.ReleaseBufferToPool(ref buffer); // No need to copy, we can release immediately
                }

                byte[] newBuffer = GetCachedBuffer(toFitAtLeastBytes);
                if (newBuffer == null)
                {
                    newBuffer = new byte[newLength];
                }

                if (copyBytes > 0)
                {
                    Helpers.BlockCopy(buffer, copyFromIndex, newBuffer, 0, copyBytes);
                    BufferPool.ReleaseBufferToPool(ref buffer);
                }

                buffer = newBuffer;
            }
            catch (OutOfMemoryException)
            {
                // Low memory situation: flush existing buffers, save current to disk, allocate new one and read data back
                string tempPath = Path.GetTempFileName();
                try
                {
                    Flush();
                    File.WriteAllBytes(tempPath, buffer);   // Write the current buffer to disk to be able to release the current buffer
                    buffer = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    buffer = new byte[(newLength + toFitAtLeastBytes) / 2];  // A bit more conservative resizing
                    using (FileStream stream = File.OpenRead(tempPath))
                    {
                        stream.Read(buffer, 0, copyBytes);  // Read data back from disk
                    }
                }
                finally
                {
                    try { File.Delete(tempPath); }  // Remove temporary file
                    catch { }
                }
            }
        }

        internal static void ReleaseBufferToPool(ref byte[] buffer)
        {
            if (buffer == null) return;

            lock (pool)
            {
                // Replace the smallest buffer: we want to cache big buffers
                int minIndex = 0;
                int minSize = Int32.MaxValue;
                for (int i = 0; i < pool.Length; i++)
                {
                    CachedBuffer tmp = pool[i];
                    if (tmp == null || !tmp.IsAlive)
                    {
                        minIndex = 0;
                        break;
                    }
                    else if (tmp.Size < minSize)
                    {
                        minIndex = i;
                        minSize = tmp.Size;
                    }
                }

                pool[minIndex] = new CachedBuffer(buffer);
            }

            // if no space, just drop it on the floor
            buffer = null;
        }

        private class CachedBuffer
        {
            private readonly WeakReference _reference;
            private readonly int _size;

            public CachedBuffer(int size)
            {
                if (size <= 0)
                {
                    throw new ArgumentOutOfRangeException("size");
                }

                _size = size;
                _reference = new WeakReference(new byte[size]);
            }

            public CachedBuffer(byte[] buffer)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException("buffer");
                }

                _size = buffer.Length;
                _reference = new WeakReference(buffer);
            }

            public bool IsAlive
            {
                get { return _reference.IsAlive; }
            }

            public int Size
            {
                get { return _size; }
            }

            public byte[] Buffer
            {
                get { return (byte[])_reference.Target; }
            }
        }
    }
}