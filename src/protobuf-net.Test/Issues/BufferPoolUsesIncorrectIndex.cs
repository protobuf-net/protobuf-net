using Xunit;

namespace ProtoBuf.Issues
{
    public class BufferPoolUsesIncorrectIndex
    {
        [Fact]
        public void ReleaseBufferToPoolUsesCorrectIndexWithFreeSlot()
        {
            // Since the BufferPool is static flush it.
            BufferPool.Flush();
            // Get a buffer
            var firstBuffer = BufferPool.GetBuffer();
            // Make a copy so we keep our weak references around
            var firstBufferCopy = firstBuffer;
            firstBuffer[0] = 1;
            var secondBuffer = BufferPool.GetBuffer();
            secondBuffer[0] = 2;
            var secondBufferCopy = secondBuffer;
            BufferPool.ReleaseBufferToPool(ref firstBuffer);
            BufferPool.ReleaseBufferToPool(ref secondBuffer);
            // Now the pool would have two CachedBuffers.
            // but in our bug case only the first pool item will be filled.
            var firstReusedBuffer = BufferPool.GetBuffer();
            var secondReusedBuffer = BufferPool.GetBuffer();
            // Now the first buffer should be one of reused ones.
            // Also checking this way should keep our copies alive.
            Assert.True(firstReusedBuffer[0] == firstBufferCopy[0] || firstReusedBuffer[0] == secondBufferCopy[0]);
            // As should the second buffer.
            Assert.True(secondReusedBuffer[0] == firstBufferCopy[0] || secondReusedBuffer[0] == secondBufferCopy[0]);
        }
    }
}
