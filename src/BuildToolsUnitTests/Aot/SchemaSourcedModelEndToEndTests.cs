using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Generators;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.Aot
{
    /// <summary>
    /// The thing this whole design is for: a <c>.proto</c>, its generated DTOs, and a
    /// <c>[ProtoModel]</c> that serializes them, all in ONE project (docs/aot-schema-model.md).
    /// </summary>
    /// <remarks>
    /// Both real generators are run over one compilation, exactly as a consumer's build would, and
    /// the result is compiled. Nothing here stands in for anything: <c>ProtoFileGenerator</c>
    /// produces the DTOs, <c>ProtoModelGenerator</c> produces the serializers from the same schema,
    /// and neither can see the other's output.
    /// </remarks>
    public class SchemaSourcedModelEndToEndTests : GeneratorTestBase<ProtoFileGenerator>
    {
        public SchemaSourcedModelEndToEndTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
            => _output = testOutputHelper;

        private readonly ITestOutputHelper _output;

        private const string Schema = @"syntax = ""proto3"";
package shop;
enum Status { UNKNOWN = 0; ACTIVE = 1; }
message Address { string city = 1; int32 postcode = 2; }
message Customer {
    int32 id = 1;
    string name = 2;
    bool active = 3;
    double balance = 4;
    Status status = 5;
    Address address = 6;
}";

        private const string ConsumerSource = @"
using ProtoBuf;
using ProtoBuf.Meta;

#pragma warning disable PBN9001 // the compile-time model APIs are [Experimental]
namespace Shop.Models
{
    [ProtoModel, ProtoSchema(""shop.proto"")]
    public partial class ShopModel : TypeModel { }
}
#pragma warning restore PBN9001
";

        /// <summary>
        /// A second schema with the same LEAF name but its own package and messages - which is what
        /// makes a bare <c>"shop.proto"</c> genuinely ambiguous without also making the two DTO sets
        /// collide, since that would be a conflict with or without any of this.
        /// </summary>
        private const string OtherSchema = @"syntax = ""proto3"";
package other;
message Widget { int32 size = 1; }";

        // additional files carry FULL paths in a real build, which is exactly why a bare leaf can
        // be ambiguous; using relative paths here would let the leaf match one of them exactly and
        // quietly test the wrong thing
        private const string ShopPath = @"C:\proj\shop.proto";

        /// <summary>Same LEAF as <see cref="ShopPath"/>, for the ambiguity case.</summary>
        private const string OtherPath = @"C:\proj\other\shop.proto";

        /// <summary>
        /// A DISTINCT leaf, for the cases that have to compile.
        /// </summary>
        /// <remarks>
        /// Observed while writing these, and worth knowing: <c>ProtoFileGenerator</c> keys schemas
        /// by leaf name (<c>Path.GetFileName</c>, then <c>set.Add(name, ...)</c>), so two
        /// same-named <c>.proto</c> files in different directories do not both produce DTOs. That
        /// is a pre-existing limit of the DTO generator, not of the model path - but it does mean
        /// the ambiguity a <c>[ProtoSchema]</c> can report is, today, only reachable in a project
        /// whose DTO generation is already incomplete. The diagnostic is still worth having: it
        /// names the problem where the alternative is a silent pick.
        /// </remarks>
        private const string WidgetPath = @"C:\proj\other\widget.proto";

        private async Task<(Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics)> RunAsync(
            string consumerSource, params (string path, string content)[] extraSchemas)
        {
            var texts = new[] { (ShopPath, Schema) }.Concat(extraSchemas).ToArray();
            var additional = Texts(texts);

            var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp12,
                documentationMode: DocumentationMode.Parse);
            var optionsProvider = TestAnalyzeConfigOptionsProvider.Empty.WithGlobalOptions(
                new TestAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                new ISourceGenerator[]
                {
                    new ProtoFileGenerator(),
                    new ProtoModelGenerator().AsSourceGenerator(),
                },
                additional, parseOptions: parseOptions, optionsProvider: optionsProvider);

            var (_, baseline) = await ObtainProjectAndCompilationAsync();

            // the real runtime's reference set: the emitted model reaches Span/Memory and friends
            var platform = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                ?.Split(System.IO.Path.PathSeparator)
                .Where(p => p.EndsWith(".dll"))
                .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
                .ToArray() ?? Array.Empty<MetadataReference>();

            var compilation = baseline
                .AddReferences(platform)
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(consumerSource, parseOptions));

            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out var produced);
            return (updated, produced);
        }

        [Fact]
        public async Task OneProjectSchemaDtosAndModelAllCompile()
        {
            var (compilation, diagnostics) = await RunAsync(ConsumerSource);

            foreach (var d in diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning))
            {
                _output.WriteLine(d.ToString());
            }
            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToArray();
            if (errors.Length != 0)
            {
                // dump everything on failure: the interesting cases here are all "the two
                // generators disagreed about a name", which is unreadable from the error alone
                foreach (var error in errors) _output.WriteLine(error.ToString());
                foreach (var tree in compilation.SyntaxTrees)
                {
                    _output.WriteLine("--- tree: " + tree.FilePath);
                    _output.WriteLine(tree.ToString());
                }
            }
            Assert.Empty(errors);

            // the DTOs exist...
            Assert.NotNull(compilation.GetTypeByMetadataName("Shop.Customer"));
            Assert.NotNull(compilation.GetTypeByMetadataName("Shop.Address"));

            // ...and the model really serializes them, rather than being an empty shell
            var model = compilation.GetTypeByMetadataName("Shop.Models.ShopModel");
            Assert.NotNull(model);
            var generated = compilation.SyntaxTrees
                .Select(t => t.ToString())
                .Where(t => t.Contains("ShopModel"))
                .ToArray();
            Assert.Contains(generated, t => t.Contains("ISerializer<global::Shop.Customer>"));
            Assert.Contains(generated, t => t.Contains("ISerializer<global::Shop.Address>"));
        }

        [Fact]
        public async Task AMissingSchemaSaysSoAndListsWhatThereIs()
        {
            var (_, diagnostics) = await RunAsync(ConsumerSource.Replace(@"""shop.proto""", @"""nope.proto"""));

            var reported = Assert.Single(diagnostics.Where(d => d.Id == "PBN2020"));
            Assert.Contains("nope.proto", reported.GetMessage());
            Assert.Contains("shop.proto", reported.GetMessage()); // says what IS available
        }

        [Fact]
        public async Task AnAmbiguousSchemaNamesTheCandidates()
        {
            // a second shop.proto in a different directory: the bare leaf now identifies neither
            var (_, diagnostics) = await RunAsync(ConsumerSource, (OtherPath, OtherSchema));

            var reported = Assert.Single(diagnostics.Where(d => d.Id == "PBN2021"));
            var message = reported.GetMessage().Replace('\\', '/');
            Assert.Contains("C:/proj/shop.proto", message);
            Assert.Contains("C:/proj/other/shop.proto", message);
        }

        /// <summary>
        /// ...and the fix for that is a path, which is the whole reason the matcher works on
        /// segments rather than on names.
        /// </summary>
        [Theory]
        [InlineData("widget.proto")]              // a bare leaf, unambiguous here
        [InlineData("other/widget.proto")]        // a path
        [InlineData(@"other\widget.proto")]       // ...and the other separator, identically
        [InlineData(@"C:\proj\other\widget.proto")] // the whole thing
        public async Task APathSelectsTheIntendedSchema(string requested)
        {
            var consumer = ConsumerSource.Replace(@"""shop.proto""", "\"" + requested.Replace(@"\", @"\\") + "\"");
            var (compilation, diagnostics) = await RunAsync(consumer, (WidgetPath, OtherSchema));

            Assert.DoesNotContain(diagnostics, d => d.Id is "PBN2020" or "PBN2021");
            var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            foreach (var error in errors) _output.WriteLine(error.ToString());
            Assert.Empty(errors);

            // the model followed the PATH, so it serializes the other schema's message and not
            // the same-named file's - which a leaf-only match could not have expressed
            var generated = compilation.SyntaxTrees.Select(t => t.ToString())
                .Where(t => t.Contains("ShopModel")).ToArray();
            Assert.Contains(generated, t => t.Contains("ISerializer<global::Other.Widget>"));
            Assert.DoesNotContain(generated, t => t.Contains("ISerializer<global::Shop.Customer>"));
        }
    }
}
