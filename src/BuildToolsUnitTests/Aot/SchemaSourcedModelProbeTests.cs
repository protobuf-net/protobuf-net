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
    /// The load-bearing question for driving an AOT model straight from a <c>.proto</c> schema in
    /// ONE project (notes/aot-schema-model.md): a source generator cannot see another generator's
    /// output — so can a generator emit code that <em>refers to</em> types another generator will
    /// emit, and does the result compile?
    /// </summary>
    /// <remarks>
    /// This is the whole mechanism, and it is probed rather than assumed. The obstacle everyone
    /// hits first — <c>[ProtoSerializable(typeof(GeneratedDto))]</c> giving an ERROR SYMBOL — is
    /// real, and is asserted below so the two situations cannot be confused. But it is an
    /// obstacle to <em>seeding by <c>typeof</c></em>, not to generating the serializer: a
    /// generator that derives its plan from the SCHEMA never needs the symbol at all, and the
    /// compiler resolves the names afterwards, once every generator's output is in the
    /// compilation.
    /// </remarks>
    public class SchemaSourcedModelProbeTests : GeneratorTestBase<ProtoFileGenerator>
    {
        public SchemaSourcedModelProbeTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        private const string Schema = @"syntax = ""proto3"";
package probe;
message Thing {
    int32 id = 1;
    string name = 2;
}";

        /// <summary>
        /// Stands in for the schema-sourced half of the AOT generator: it reads the SAME
        /// <c>.proto</c> additional file, and emits code naming a type it cannot see.
        /// </summary>
        [Generator]
        private sealed class NameReferencingGenerator : ISourceGenerator
        {
            internal string? Emitted { get; private set; }
            internal bool SawSchema { get; private set; }
            internal bool CouldResolveSymbol { get; private set; }

            void ISourceGenerator.Initialize(GeneratorInitializationContext context) { }

            void ISourceGenerator.Execute(GeneratorExecutionContext context)
            {
                // (c) the same inputs are available to both generators
                SawSchema = context.AdditionalFiles.Any(f => f.Path.EndsWith(".proto"));

                // ...and the DTO's symbol is NOT available, which is the documented obstacle
                CouldResolveSymbol = context.Compilation.GetTypeByMetadataName("Probe.Thing") is not null;

                // (b) name it anyway: the compiler resolves this once both generators have run
                Emitted = @"namespace SchemaSourcedProbe
{
    internal static class UsesTheDto
    {
        internal static object Make() => new global::Probe.Thing { Id = 42, Name = ""probe"" };
        internal static int ReadId(global::Probe.Thing value) => value.Id;
    }
}";
                context.AddSource("UsesTheDto.g.cs", Emitted);
            }
        }

        private async Task<(Compilation Compilation, NameReferencingGenerator Probe)> RunBothAsync()
        {
            var additional = Text("probe.proto", Schema);
            var second = new NameReferencingGenerator();

            var parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Regular,
                documentationMode: DocumentationMode.Parse);
            var optionsProvider = TestAnalyzeConfigOptionsProvider.Empty.WithGlobalOptions(
                new TestAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                new ISourceGenerator[] { new ProtoFileGenerator(), second },
                additional, parseOptions: parseOptions, optionsProvider: optionsProvider);

            var (_, compilation) = await ObtainProjectAndCompilationAsync();
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _);
            return (updated, second);
        }

        /// <summary>
        /// The negative half: the DTO's symbol genuinely is not visible to another generator, so
        /// a <c>typeof</c>-shaped seed cannot work and this is not a case of nobody having tried.
        /// </summary>
        [Fact]
        public async Task AGeneratorCannotSeeAnotherGeneratorsType()
        {
            var (_, probe) = await RunBothAsync();
            Assert.True(probe.SawSchema, "the second generator must see the same .proto");
            Assert.False(probe.CouldResolveSymbol,
                "if this ever passes, the whole two-project workaround is unnecessary and this "
                + "test is the place that says so");
        }

        /// <summary>
        /// The positive half, and the mechanism the design rests on: emitted code may NAME what
        /// it cannot see.
        /// </summary>
        [Fact]
        public async Task EmittedCodeMayReferenceAnotherGeneratorsType()
        {
            var (compilation, _) = await RunBothAsync();

            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToArray();
            Assert.Empty(errors);

            // and the reference really did bind, rather than both trees being absent
            var dto = compilation.GetTypeByMetadataName("Probe.Thing");
            Assert.NotNull(dto);
            var user = compilation.GetTypeByMetadataName("SchemaSourcedProbe.UsesTheDto");
            Assert.NotNull(user);
        }

        /// <summary>
        /// The names have to be predicted exactly, and the prediction is not guesswork: the
        /// generator assembly compiles protobuf-net.Reflection in, so a schema front-end can call
        /// the very same <c>NameNormalizer</c> the DTO generator used. This pins the two facts
        /// that prediction depends on - the package-to-namespace and field-to-property mappings -
        /// so a change to either fails here rather than in a consumer's build.
        /// </summary>
        [Fact]
        public async Task TheNamesAreDerivable()
        {
            var (compilation, _) = await RunBothAsync();
            var dto = compilation.GetTypeByMetadataName("Probe.Thing");
            Assert.NotNull(dto);

            var members = dto!.GetMembers().OfType<IPropertySymbol>().Select(p => p.Name).OrderBy(x => x).ToArray();
            Assert.Equal(new[] { "Id", "Name" }, members);

            // the same call the DTO generator makes, on the same descriptor shape - which is what
            // makes the prediction shared code rather than a parallel implementation
            var normalizer = ProtoBuf.Reflection.NameNormalizer.Default;
            Assert.Equal("Id", normalizer.GetName(
                new Google.Protobuf.Reflection.FieldDescriptorProto { Name = "id" }));
            Assert.Equal("Name", normalizer.GetName(
                new Google.Protobuf.Reflection.FieldDescriptorProto { Name = "name" }));
            Assert.Equal("Thing", normalizer.GetName(
                new Google.Protobuf.Reflection.DescriptorProto { Name = "Thing" }));
        }
    }
}
