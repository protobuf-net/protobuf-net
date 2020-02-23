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

        [Fact]
        public void BuildTypeHierarchyForServices()
        {

            var set = new FileDescriptorSet();
            using (var file = File.OpenText("basic_service.proto"))
            {
                Assert.True(set.Add("basic_service.proto", true, file));
            }
            set.Process();
            var err = set.GetErrors();
            Assert.Empty(err);

            Assert.Equal(".HelloWorld.HelloService", set.Files[0].Services[0].FullyQualifiedName);
            Assert.Equal(".HelloWorld.HelloService.SayHello", set.Files[0].Services[0].Methods[0].FullyQualifiedName);
        }
    }
}
