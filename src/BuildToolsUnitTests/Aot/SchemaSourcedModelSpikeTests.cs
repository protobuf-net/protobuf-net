using Google.Protobuf.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Generators;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Reflection;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.Aot
{
    /// <summary>
    /// The spike behind <c>docs/aot-schema-model.md</c>: build AOT plans straight from a
    /// <c>.proto</c> schema, emit the model, and compile it against the DTOs the OTHER generator
    /// emits from the same schema — in one compilation, with neither generator seeing the other.
    /// </summary>
    /// <remarks>
    /// <see cref="SchemaSourcedModelProbeTests"/> proved the mechanism (emitted code may name what
    /// it cannot see). This proves the harder half: that the plan can be derived from the schema
    /// accurately enough for the emitted serializer to bind to the emitted DTOs. It is the
    /// drift-catcher — every CONVENTION comment in <c>SchemaPlanBuilder</c> is a place protogen
    /// could move, and a move shows up here as a build error rather than in a consumer's project.
    /// </remarks>
    public class SchemaSourcedModelSpikeTests : GeneratorTestBase<ProtoFileGenerator>
    {
        public SchemaSourcedModelSpikeTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
            => _output = testOutputHelper;

        private readonly ITestOutputHelper _output;

        private const string Schema = @"syntax = ""proto3"";
package spike;
enum Colour { UNKNOWN = 0; RED = 1; GREEN = 2; }
message Inner { int32 depth = 1; }
message Thing {
    int32 id = 1;
    string name = 2;
    bool active = 3;
    double score = 4;
    Colour colour = 5;
    Inner inner = 6;
    int64 ticks = 7;
    bytes blob = 8;
}";

        private static FileDescriptorSet ParseSchema(string name, string content)
        {
            var set = new FileDescriptorSet();
            set.Add(name, true, new System.IO.StringReader(content));
            set.Process();
            var errors = set.GetErrors();
            Assert.DoesNotContain(errors, e => e.IsError);
            return set;
        }

        /// <summary>
        /// The whole loop: schema -> DTOs (generator A), schema -> plan -> model (this test
        /// standing in for generator B), compiled together.
        /// </summary>
        [Fact]
        public async Task SchemaSourcedModelCompilesAgainstGeneratedDtos()
        {
            // generator A: the DTOs, produced exactly as a consumer's build would
            var (result, diagnostics) = await GenerateAsync(Text("spike.proto", Schema));
            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            var dtoTree = Assert.Single(result.GeneratedTrees);

            // generator B: the same schema, projected into plans and emitted as a model
            var set = ParseSchema("spike.proto", Schema);
            var plan = SchemaPlanBuilder.TryBuild(set, NameNormalizer.Default,
                "SpikeModels", "SpikeModel", out var unsupported);
            Assert.Null(unsupported);
            Assert.NotNull(plan);

            var modelSource = ProtoModelGenerator.Emit(plan!);
            _output.WriteLine(modelSource);

            // the consumer's own half: the partial declaration the attribute would sit on
            const string ModelDeclaration = @"namespace SpikeModels
{
    public partial class SpikeModel : global::ProtoBuf.Meta.TypeModel { }
}";

            var (_, baseline) = await ObtainProjectAndCompilationAsync();

            // the generated model reaches Span/Memory/ReadOnlySequence through the writer surface,
            // so the reference set has to be the real runtime's rather than the harness's minimum
            var platform = ((string?)System.AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                ?.Split(System.IO.Path.PathSeparator)
                .Where(p => p.EndsWith(".dll"))
                .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
                .ToArray() ?? System.Array.Empty<MetadataReference>();
            baseline = baseline.AddReferences(platform);

            var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp12);
            var compilation = baseline.AddSyntaxTrees(
                dtoTree.WithRootAndOptions(dtoTree.GetRoot(), parseOptions),
                CSharpSyntaxTree.ParseText(ModelDeclaration, parseOptions),
                CSharpSyntaxTree.ParseText(modelSource, parseOptions));

            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToArray();
            foreach (var error in errors) _output.WriteLine(error.ToString());
            Assert.Empty(errors);

            // and it really did produce a serializer per message, rather than an empty shell
            Assert.Contains("ISerializer<global::Spike.Thing>", modelSource);
            Assert.Contains("ISerializer<global::Spike.Inner>", modelSource);
        }

        /// <summary>
        /// The scope guard: anything the spike does not model must be REFUSED rather than emitted
        /// wrongly, since a plan that silently omits a member is a wire bug.
        /// </summary>
        [Theory]
        // a repeated ENUM is still refused, and for a reason of its own: it resolves its element
        // serializer from the model, so the services type needs an ISerializerProxy<TEnum>
        [InlineData("enum E { A = 0; }\nmessage M { repeated E xs = 1; }", "serializer proxy")]
        // ...and an enum on either side of a MAP, for exactly the same reason
        [InlineData("enum E { A = 0; }\nmessage M { map<string, E> m = 1; }", "serializer proxy")]
        [InlineData("message M { oneof choice { int32 a = 1; string b = 2; } }", "oneof")]
        public void OutOfScopeShapesAreRefused(string body, string because)
        {
            var set = ParseSchema("scope.proto", "syntax = \"proto3\";\npackage scope;\n" + body);
            var plan = SchemaPlanBuilder.TryBuild(set, NameNormalizer.Default,
                "SpikeModels", "ScopeModel", out var unsupported);
            Assert.Null(plan);
            Assert.NotNull(unsupported);
            Assert.Contains(because, unsupported!);
        }
    }
}
