using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Issues
{
    public class Issue381
    {
        [Fact]
        public void CheckCompilerAvailable()
        {
            Assert.True(RuntimeTypeModel.EnableAutoCompile());
        }
    }
}
