using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Generators;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BuildToolsUnitTests.Aot
{
    /// <summary>
    /// The generator's model is built from hand-written equatable types precisely so that the
    /// driver can cache it; a caching regression is invisible (everything still works, just slower,
    /// and IDE latency degrades), so it needs asserting explicitly.
    /// </summary>
    public class ProtoModelGeneratorIncrementalTests
    {
        private const string Source = """
            using ProtoBuf;
            using ProtoBuf.Meta;

            namespace Incremental;

            [ProtoContract]
            public class Root
            {
                [ProtoMember(1)] public int Id { get; set; }
                [ProtoMember(2)] public Leaf Leaf { get; set; }
            }

            [ProtoContract]
            public class Leaf
            {
                [ProtoMember(1)] public string Name { get; set; }
            }

            [ProtoModel]
            [ProtoSerializable(typeof(Root))]
            public partial class IncrementalModel : TypeModel { }
            """;

        [Fact]
        public void ModelIsCachedWhenNothingRelevantChanges()
        {
            var reasons = RunWithEdit(Source + "\n// a comment, changing nothing that matters\n");

            // Cached: the step was not re-run. Unchanged: it re-ran but the new plan compared equal
            // to the old one - which is the case that proves the equatable model is doing its job.
            Assert.NotEmpty(reasons);
            Assert.All(reasons, reason => Assert.True(
                reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged,
                $"model step reported {reason}; the plan is not comparing equal across irrelevant edits"));
        }

        [Fact]
        public void ModelIsNotCachedWhenTheContractActuallyChanges()
        {
            // the counterpart to the test above: without this, an equality implementation that
            // reported "equal" for everything would pass and nobody would notice
            var reasons = RunWithEdit(Source.Replace("[ProtoMember(1)] public int Id", "[ProtoMember(4)] public int Id"));
            Assert.Contains(IncrementalStepRunReason.Modified, reasons);
        }

        /// <summary>
        /// Run the generator over <see cref="Source"/>, then again over an edited version of the
        /// same file, and report why the model step produced what it produced the second time.
        /// </summary>
        /// <remarks>
        /// The edit deliberately replaces the model's *own* syntax tree rather than adding an
        /// unrelated one: adding a new tree leaves the step trivially cached, which would prove
        /// nothing about the equality implementation.
        /// </remarks>
        private static List<IncrementalStepRunReason> RunWithEdit(string editedSource)
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse);
            var compilation = CSharpCompilation.Create(
                "ProtoBuf.BuildTools.IncrementalTests",
                new[] { CSharpSyntaxTree.ParseText(Source, parseOptions, path: "input.cs") },
                MetadataReferenceHelpers.WellKnownReferences.Concat(MetadataReferenceHelpers.ProtoBufReferences),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                new[] { new ProtoModelGenerator().AsSourceGenerator() },
                parseOptions: parseOptions,
                optionsProvider: null,
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None,
                    trackIncrementalGeneratorSteps: true));

            driver = driver.RunGenerators(compilation);

            var edited = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.Single(),
                CSharpSyntaxTree.ParseText(editedSource, parseOptions, path: "input.cs"));
            driver = driver.RunGenerators(edited);

            var results = driver.GetRunResult().Results.Single();
            Assert.True(results.TrackedSteps.ContainsKey(ProtoModelGenerator.ModelTrackingName),
                $"no tracked step named '{ProtoModelGenerator.ModelTrackingName}'");

            return (from step in results.TrackedSteps[ProtoModelGenerator.ModelTrackingName]
                    from output in step.Outputs
                    select output.Reason).ToList();
        }
    }
}
