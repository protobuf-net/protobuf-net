using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Generators;
using System;
using System.Linq;
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
        public async Task GenerateWithImport()
        {
            (var result, var diagnostics) = await GenerateAsync(
                Texts(
                    ("/code/x/y/foo.proto", @"
syntax = ""proto3"";
import ""import/bar.proto"";

message Foo {
    Bar bar = 1;
}
"),
                    ("/code/x/y/import/bar.proto", @"
syntax = ""proto3"";

message Bar {
    int32 i = 1;
}")
                ));
            Assert.Empty(diagnostics);
            Assert.Equal(2, result.GeneratedTrees.Length);
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

        [Fact]
        public async Task DeepImportWorksWithExtraImport()
        {
            (var result, var diagnostics) = await GenerateAsync(Texts(
                ("/foo/google/protobuf/a.proto", @"
syntax = ""proto3"";
import ""google/protobuf/b.proto"";
message Foo {
    Bar bar = 1;
}", new[] { ("ImportPaths", "../../") }),
("/foo/google/protobuf/b.proto", @"
syntax = ""proto3"";
message Bar {}", null)
));
            Assert.Empty(diagnostics);
            Assert.Equal(2, result.GeneratedTrees.Length);
        }

        [Fact]
        public async Task DeepImportFailsWithoutExtraImport()
        {
            (var result, var diagnostics) = await GenerateAsync(Texts(
                ("/foo/google/protobuf/a.proto", @"
syntax = ""proto3"";
import ""google/protobuf/b.proto"";
message Foo {
    Bar bar = 1;
}", Array.Empty<(string,string)>()),
("/foo/google/protobuf/b.proto", @"
syntax = ""proto3"";
message Bar {}", null)
));
            Assert.Equal(3, diagnostics.Length);
            Assert.Single(diagnostics.Where(x => x.Id == "PBN1004" && x.GetMessage() == "unable to find: 'google/protobuf/b.proto'" && x.Severity == DiagnosticSeverity.Error));
            Assert.Single(diagnostics.Where(x => x.Id == "PBN1002" && x.GetMessage() == "type not found: 'Bar'" && x.Severity == DiagnosticSeverity.Error));
            Assert.Single(diagnostics.Where(x => x.Id == "PBN1020" && x.GetMessage() == "import not used: 'google/protobuf/b.proto'" && x.Severity == DiagnosticSeverity.Warning));
        }
    }
}
