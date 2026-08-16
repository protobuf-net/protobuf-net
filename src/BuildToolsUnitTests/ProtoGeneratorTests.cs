using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Generators;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

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

        /// <summary>
        /// PBN1900: a typo in item metadata is reported rather than silently ignored.
        /// </summary>
        /// <remarks>
        /// Every one of these option parsers falls back rather than throwing - which is right for
        /// a build, but means <c>SubTypes="seald"</c> does nothing and says nothing. The negative
        /// cases matter as much as the positive: a VALID value must stay silent, or the warning
        /// becomes noise and gets suppressed wholesale.
        /// </remarks>
        [Theory]
        [InlineData("SubTypes", "sealed", false)]
        [InlineData("SubTypes", "Sealed", false)]     // case-insensitive, as MSBuild metadata is
        [InlineData("SubTypes", "seald", true)]
        [InlineData("OneOf", "enum", false)]
        [InlineData("OneOf", "enums", true)]
        [InlineData("Names", "noplural", false)]
        [InlineData("Names", "no-plural", true)]
        [InlineData("ListSet", "off", false)]         // IsEnabledValue lumps this with "anything
        [InlineData("ListSet", "of", true)]           // else"; only PBN1900 can tell them apart
        [InlineData("LangVersion", "whatever", false)] // free-form: must NOT be second-guessed
        public async Task UnrecognisedOptionValueIsReported(string key, string value, bool expectWarning)
        {
            var (_, diagnostics) = await GenerateAsync(Texts(
                ("test.proto", @"syntax = ""proto3""; message Foo { int32 id = 1; }",
                 // the harness adds the build_metadata.AdditionalFiles. prefix itself
                 new[] { (key, value) })));

            var reported = diagnostics.Where(d => d.Id == "PBN1900").ToArray();
            Assert.Equal(expectWarning ? 1 : 0, reported.Length);
            if (expectWarning)
            {
                Assert.Equal(DiagnosticSeverity.Warning, reported[0].Severity);
                // the message has to be actionable: it names the option AND the accepted spellings
                var text = reported[0].GetMessage();
                Assert.Contains(key, text);
                Assert.Contains(value, text);
            }
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
        public async Task DefinitionIsNotIncludedInOutput()
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
message Bar {}", new[] { ("IncludeInOutput", "False") }
)));
            Assert.Empty(diagnostics);
            Assert.Single(result.GeneratedTrees);
        }

        [Fact]
        public async Task IncludeInOutputGarbageInput()
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
message Bar {}", new[] { ("IncludeInOutput", "Garbage") }
                )));
            Assert.Empty(diagnostics);
            Assert.Equal(2, result.GeneratedTrees.Length);
        }

        [Fact]
        public async Task IncludeInOutputNoInput()
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
message Bar {}", new[] { ("IncludeInOutput", "") }
                )));
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
