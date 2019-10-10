using Google.Protobuf.Reflection;
using System.IO;
using Xunit;

namespace ProtoBuf
{
    public class Parsers
    {
        [Fact]
        public void EmbeddedImportsWork()
        {
            
            var set = new FileDescriptorSet();
            using (var file = File.OpenText("basic.proto"))
            {
                Assert.True(set.Add("basic.proto", true, file));
            }
            set.Process();
            var err = set.GetErrors();
            Assert.Empty(err);
        }
    }
}
