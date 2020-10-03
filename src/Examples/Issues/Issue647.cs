using Google.Protobuf.Reflection;
using ProtoBuf.Meta;
using ProtoBuf.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Issues
{
    public class Issue647
    {
        private readonly ITestOutputHelper _log;
        private void Log(string message) => _log?.WriteLine(message);
        public Issue647(ITestOutputHelper log) => _log = log;

        const string ExpectedProto = @"syntax = ""proto3"";
import ""protobuf-net/protogen.proto""; // custom protobuf-net options

message BarClass {
   option (.protobuf_net.msgopt).namespace = ""ProtoBuf.Issues.Issue647Types.Bar"";
}
message BazClass {
   option (.protobuf_net.msgopt).namespace = ""ProtoBuf.Issues.Issue647Types.Baz"";
}
message FooClass {
   option (.protobuf_net.msgopt).namespace = ""ProtoBuf.Issues.Issue647Types.Foo"";
   BarClass BarMember = 1;
   BazClass BazMember = 2;
}
";
        [Fact]
        public void ProtoIncludesNamespaces()
        {
            var proto = Serializer.GetProto(new SchemaGenerationOptions { Types = { typeof(Issue647Types.Foo.FooClass) }, Syntax = ProtoSyntax.Proto3, Flags = SchemaGenerationFlags.MultipleNamespaceSupport });
            Log(proto);
            Assert.Equal(ExpectedProto, proto, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void GeneratedCodeIncludesNamespaces()
        {
            var set = new FileDescriptorSet();
            set.Add("my.proto", true, new StringReader(ExpectedProto));
            set.Process();
            Assert.Empty(set.GetErrors());
            var actualCsharp = CSharpCodeGenerator.Default.Generate(set, options: new Dictionary<string, string>
            {
                { "langver", "8" },
            }).Single().Text;
            Log(actualCsharp);

            var expectedCsharp = File.ReadAllText(@"Issues/Issue647.Generated.cs");
            Assert.Equal(expectedCsharp, actualCsharp, ignoreLineEndingDifferences: true);
        }
    }
}
