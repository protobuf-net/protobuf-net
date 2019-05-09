using Xunit;

namespace ProtoBuf.Perf
{
    public class Primitives
    {
        [Fact]
        public void DecimalIsOptimized()
        {
            Assert.True(BclHelpers.DecimalOptimized);
        }
    }
}
