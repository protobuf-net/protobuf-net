using Xunit;

namespace ProtoBuf
{
    public class DiscriminatedUnions
    {
        [Fact]
        public void BasicUsage()
        {
            DiscriminatedUnion32 union;
            
            union = new DiscriminatedUnion32(4, 42);
            Assert.True(union.Is(4));
            Assert.Equal(4, union.Discriminator);
            Assert.Equal(42, union.Int32);

            DiscriminatedUnion32.Reset(ref union, 3); // should do nothing
            Assert.True(union.Is(4));
            Assert.Equal(4, union.Discriminator);
            Assert.Equal(42, union.Int32);

            DiscriminatedUnion32.Reset(ref union, 4); // should reset
            Assert.False(union.Is(4));
            Assert.True(union.Is(0));
            //Assert.Equal(0, union.Discriminator);

            //union = new DiscriminatedUnion32(4, 42);

            //union = default;
            //Assert.True(union.Is(0));
            //Assert.Equal(0, union.Discriminator);

        }
    }
}
