using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Generators;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace BuildToolsUnitTests.Grpc
{
    /// <summary>
    /// The plan is built from hand-written equatable types precisely so the driver can cache it, and a
    /// caching regression is invisible - everything still works, just slower, with IDE latency
    /// degrading as the model grows. So it is asserted rather than left to the model's good intentions.
    /// </summary>
    /// <remarks>
    /// Both directions are tested, and the second one is the point: an equality implementation that
    /// answered "equal" to everything would pass the first test on its own, and would mean the
    /// generator never noticed a real edit.
    /// </remarks>
    public class GrpcProxyGeneratorIncrementalTests
    {
        private const string InputPath = "input.cs";

        private const string Source = """
            #nullable enable
            using ProtoBuf;
            using ProtoBuf.Grpc;
            using ProtoBuf.Grpc.Configuration;
            using ProtoBuf.Meta;
            using System.Threading.Tasks;

            namespace Incremental;

            [ProtoContract]
            public class Request
            {
                [ProtoMember(1)] public string? Name { get; set; }
            }

            [ProtoContract]
            public class Reply
            {
                [ProtoMember(1)] public string? Message { get; set; }
            }

            [Service]
            public interface IThing
            {
                Task<Reply> GetAsync(Request request, CallContext context = default);
            }

            public partial class IncrementalModel : TypeModel
            {
                public static IncrementalModel Instance { get; } = new IncrementalModel();
            }

            [ProtoGrpc(Model = typeof(IncrementalModel))]
            [ProtoService(typeof(IThing))]
            public sealed partial class IncrementalServices : ClientFactory { }
            """;

        [Fact]
        public void PlanIsCachedWhenNothingRelevantChanges()
        {
            var reasons = RunWithEdit(Source + "\n// a comment, changing nothing that matters\n");

            // Cached: the step did not re-run at all. Unchanged: it re-ran and the new plan compared
            // equal to the old one, which is the case that proves the equatable model is doing its job.
            Assert.NotEmpty(reasons);
            Assert.All(reasons, reason => Assert.True(
                reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged,
                $"plan step reported {reason}; the plan is not comparing equal across irrelevant edits"));
        }

        [Fact]
        public void PlanIsNotCachedWhenTheContractActuallyChanges()
        {
            // renaming an operation changes the wire name, so this MUST invalidate
            var reasons = RunWithEdit(Source.Replace("Task<Reply> GetAsync", "Task<Reply> FetchAsync"));
            Assert.Contains(IncrementalStepRunReason.Modified, reasons);
        }

        /// <summary>
        /// Run the generator over <see cref="Source"/>, then again over an edited version of that same
        /// file, and report why the plan step produced what it produced the second time.
        /// </summary>
        /// <remarks>
        /// The edit replaces the model's <em>own</em> syntax tree rather than adding an unrelated one:
        /// adding a tree leaves the step trivially cached, which would prove nothing about equality.
        /// The surface snapshot is a second tree throughout, and is never edited - so it is also
        /// standing proof that an untouched tree does not disturb the result.
        /// </remarks>
        private static List<IncrementalStepRunReason> RunWithEdit(string editedSource)
        {
            var surfacePath = Path.Combine("Grpc", "Data", "_ContractSurface.cs");
            var surface = File.ReadAllText(surfacePath);

            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse);
            var compilation = CSharpCompilation.Create(
                "ProtoBuf.BuildTools.GrpcIncrementalTests",
                new[]
                {
                    CSharpSyntaxTree.ParseText(Source, parseOptions, path: InputPath),
                    CSharpSyntaxTree.ParseText(surface, parseOptions, path: surfacePath),
                },
                MetadataReferenceHelpers.WellKnownReferences.Concat(MetadataReferenceHelpers.ProtoBufReferences),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                new[] { new GrpcProxyGenerator().AsSourceGenerator() },
                parseOptions: parseOptions,
                optionsProvider: null,
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None,
                    trackIncrementalGeneratorSteps: true));

            driver = driver.RunGenerators(compilation);

            var input = compilation.SyntaxTrees.Single(static x => x.FilePath == InputPath);
            var edited = compilation.ReplaceSyntaxTree(
                input, CSharpSyntaxTree.ParseText(editedSource, parseOptions, path: InputPath));
            driver = driver.RunGenerators(edited);

            var results = driver.GetRunResult().Results.Single();
            Assert.True(results.TrackedSteps.ContainsKey(GrpcProxyGenerator.PlanTrackingName),
                $"no tracked step named '{GrpcProxyGenerator.PlanTrackingName}'");

            return (from step in results.TrackedSteps[GrpcProxyGenerator.PlanTrackingName]
                    from output in step.Outputs
                    select output.Reason).ToList();
        }
    }
}
