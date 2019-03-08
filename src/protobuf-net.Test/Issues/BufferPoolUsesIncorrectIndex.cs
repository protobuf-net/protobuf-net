using System;
using Xunit;

namespace ProtoBuf.Issues
{
    public class BufferPoolUsesIncorrectIndex
    {
        [Fact]
        public void ReleaseBufferToPoolUsesCorrectIndexWithFreeSlot()
        {
            byte[][] possiblyFromThePool = new byte[BufferPool.POOL_SIZE][];
            // Since the BufferPool is static lets empty it by requesting BufferPool.POOL_SIZE buffers
            for (int i = 0; i < BufferPool.POOL_SIZE; i++)
            {
                possiblyFromThePool[i] = BufferPool.GetBuffer();
            }

            var firstBuffer = BufferPool.GetBuffer();
            // Make a copy so we keep our weak reference around
            var firstBufferCopy = firstBuffer;
            firstBuffer[0] = 1;
            var secondBuffer = BufferPool.GetBuffer();
            secondBuffer[0] = 2;
            var secondBufferCopy = secondBuffer;
            BufferPool.ReleaseBufferToPool(ref firstBuffer);
            BufferPool.ReleaseBufferToPool(ref secondBuffer);
            // Now the pool should have two weak references.
            // but in our bug case only the first pool item will be filled.
            var firstReusedBuffer = BufferPool.GetBuffer();
            var secondReusedBuffer = BufferPool.GetBuffer();
            // Now the first buffer should be one of reused ones.
            // Reused buffers won't be set to zero by the runtime.
            Assert.NotEqual(0, firstReusedBuffer[0]);
            // As should the second buffer.
            Assert.NotEqual(0, secondReusedBuffer[0]);
        }
    }
}
