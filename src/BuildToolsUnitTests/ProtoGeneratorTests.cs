using ProtoBuf.BuildTools.Generators;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BuildToolsUnitTests
{
    public class ProtoGeneratorTests : GeneratorTestBase<ProtoFileGenerator>
    {
        public ProtoGeneratorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public async Task BasicGenerateWorks()
        {
            (var result, var diagnostics) = await GenerateAsync(Text("test.proto", @"syntax = ""proto3""; message Foo {}"));

            Assert.Empty(diagnostics);
            Assert.Single(result.GeneratedTrees);
        }

        [Fact]
        public async Task EmbeddedImportWorks()
        {
            (var result, var diagnostics) = await GenerateAsync(Text("test.proto", @"
syntax = ""proto3"";
import ""google/protobuf/timestamp.proto"";
message Foo {
    .google.protobuf.Timestamp when = 1;
}"));

            Assert.Empty(diagnostics);
            Assert.Single(result.GeneratedTrees);
        }
    }
}
