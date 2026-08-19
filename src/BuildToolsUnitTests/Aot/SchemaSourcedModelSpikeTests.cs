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
    /// The spike behind <c>notes/aot-schema-model.md</c>: build AOT plans straight from a
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
        /// The <c>SubTypes</c> option reaches the emitted model, so the two generated halves agree.
        /// </summary>
        /// <remarks>
        /// The value is read from the DTO generator's own <c>ProtoBuf_SubTypes</c> item metadata
        /// rather than from anything the model declares - one key, one answer, so the halves
        /// cannot disagree. What this pins is the second half of that: that the plan actually acts
        /// on it. Without this the option would silently do nothing for <c>[ProtoSchema]</c>
        /// consumers, which is exactly the audience it was added for.
        /// <para>
        /// Both non-default values elide the same call but say different things, so the plan keeps
        /// them distinct: <c>Sealed</c> means a sub-type is impossible, <c>Ignore</c> means one is
        /// tolerated and written as the base.
        /// </para>
        /// </remarks>
        [Theory]
        [InlineData("", true)]                  // unset: the shipped behaviour
        [InlineData("sealed", false)]
        [InlineData("SEALED", false)]           // matched case-insensitively, as MSBuild metadata is
        [InlineData("ignore", false)]
        [InlineData("nonsense", true)]          // a typo falls back, exactly as the DTO generator does
        public void SubTypeOptionReachesTheEmittedModel(string metadata, bool expectCheck)
        {
            // from the raw ProtoBuf_SubTypes string, so the parser is on the hook too - the two
            // generators must agree on typos as well as on the valid spellings
            var subTypes = ProtoModelGenerator.SchemaText.ParseSubTypes(metadata);

            var set = ParseSchema("spike.proto", Schema);
            var plan = SchemaPlanBuilder.TryBuild(set, NameNormalizer.Default,
                "SpikeModels", "SpikeModel", out var unsupported, subTypes);
            Assert.Null(unsupported);
            Assert.NotNull(plan);

            var source = ProtoModelGenerator.Emit(plan!);
            Assert.Equal(expectCheck, source.Contains("ThrowUnexpectedSubtype"));
        }

        /// <summary>
        /// The scope guard: anything the spike does not model must be REFUSED rather than emitted
        /// wrongly, since a plan that silently omits a member is a wire bug.
        /// </summary>
        [Theory]
        // The repeated-enum and enum-map-value cases USED to be pinned here as refusals, on the
        // grounds that "an EMPTY packed collection emits a zero-length field where ref-emit writes
        // nothing". Every clause of that reasoning failed on re-checking (2026-08-14): IsPacked is
        // honoured by the symbol path, there is no separate packed raw-write arm - RawRepeatedWritable
        // declines IsPacked so it falls back to the same RepeatedSerializer call ref-emit makes -
        // and protobuf-net never packs an enum anyway, because EnumSerializer is not an
        // IMeasuringSerializer. Both now emit, and are held by the BYTE gate
        // (AotConformanceTests/Schemas/conformance.proto) rather than by a refusal, which is the
        // stronger check: it compares the actual payload including the empty case that was
        // originally reported.
        //
        // NOTE there is no map-of-map case here, and that is a finding rather than an omission:
        // proto forbids a map as a map VALUE, so the shape cannot be written. A map whose value is
        // a MESSAGE that happens to contain a map is ordinary, and is supported.
        // `group` is proto2-only, so this one carries its own syntax line rather than taking the
        // harness's proto3 default - the only remaining refusal, and the only genuinely missing
        // schema feature across the 268-schema corpus
        [InlineData("syntax = \"proto2\";\nmessage M { optional group Detail = 1 { optional int32 depth = 1; } }",
            "outside the spike")]
        public void OutOfScopeShapesAreRefused(string body, string because)
        {
            var prefix = body.StartsWith("syntax") ? "" : "syntax = \"proto3\";\n";
            var set = ParseSchema("scope.proto", prefix + "package scope;\n" + body);
            var plan = SchemaPlanBuilder.TryBuild(set, NameNormalizer.Default,
                "SpikeModels", "ScopeModel", out var unsupported);
            Assert.Null(plan);
            Assert.NotNull(unsupported);
            Assert.Contains(because, unsupported!);
        }
    }
}
