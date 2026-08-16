using Google.Protobuf.Reflection;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Reflection;
using System.Linq;
using Xunit;

namespace BuildToolsUnitTests.Aot
{
    /// <summary>
    /// Points the schema front-end at <c>descriptor.proto</c> — the largest real schema available,
    /// and the one whose symbol-derived model is checked in — to answer "which gaps actually
    /// occur", rather than working the gap list in the order someone thought of it.
    /// </summary>
    /// <remarks>
    /// This is a MEASUREMENT, not a requirement: it is expected to refuse today, and the value is
    /// in <em>what</em> it refuses on. The assertion is deliberately weak (it must not crash, and
    /// it must give a reason) so that the test reports rather than blocks; the reason is written
    /// to test output.
    /// </remarks>
    public class SchemaSourcedDescriptorProbeTests
    {
        private readonly ITestOutputHelper _output;

        public SchemaSourcedDescriptorProbeTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public void DescriptorProtoReportsWhatItNeeds()
        {
            var set = new FileDescriptorSet();
            // the embedded copy, exactly as DescriptorParseBenchmarks.BuildPayload resolves it
            Assert.True(set.Add("google/protobuf/descriptor.proto", includeInOutput: true),
                "could not resolve the embedded descriptor.proto");
            set.Process();
            Assert.DoesNotContain(set.GetErrors(), e => e.IsError);

            var names = NameNormalizer.Default;
            var plan = SchemaPlanBuilder.TryBuild(set, names, "Probe", "DescriptorModel",
                out var unsupported);

            if (plan is null)
            {
                _output.WriteLine("REFUSED: " + unsupported);
            }
            else
            {
                _output.WriteLine($"BUILT: {plan.Contracts.Count} contracts");
                foreach (var contract in plan.Contracts.AsEnumerable().Take(10))
                {
                    _output.WriteLine("  " + contract.TypeName);
                }
            }

            // the front-end must reach a verdict either way - never throw
            Assert.True(plan is not null || !string.IsNullOrEmpty(unsupported),
                "the front-end neither built a plan nor said why not");

            if (plan is null) return;

            // (c) "do we get the same thing both ways?" - emit, and print the bodies for a few
            // contracts so they can be diffed against the CHECKED-IN symbol-derived model in
            // src/protobuf-net.Reflection/Generated/. Divergence is expected in places
            // (Descriptor.cs is hand-augmented with partial members and custom options that the
            // schema knows nothing about); what matters is whether the SHARED shapes agree
            // TryBuild returns a plan with the capability flags at their defaults (false), because
            // in the real pipeline AddSchemas takes only .Contracts and folds them into the
            // symbol-derived plan, which carries the probed flags. So the flags have to be
            // restated here or the probe emits the CLASSIC bodies and measures nothing
            var raw = new ProtoBuf.BuildTools.Internal.Aot.ProtoModelPlan(
                "Probe", "DescriptorModel", plan.Contracts,
                rawReader: true, rawWriter: true, listAsSpan: true);
            var source = ProtoBuf.BuildTools.Generators.ProtoModelGenerator.Emit(raw);

            // dumped rather than asserted: the comparison against the checked-in symbol-derived
            // model is a one-off review, and 100 KB does not belong in test output. The
            // environment variable keeps it opt-in so an ordinary run writes nothing
            var dump = System.Environment.GetEnvironmentVariable("PBN_SCHEMA_DUMP");
            if (!string.IsNullOrEmpty(dump))
            {
                System.IO.File.WriteAllText(dump, source);
                _output.WriteLine($"wrote {source.Length} chars to {dump}");
            }
            _output.WriteLine($"emitted {source.Length} chars; "
                + $"{System.Text.RegularExpressions.Regex.Matches(source, "RawWrite_").Count} RawWrite_, "
                + $"{System.Text.RegularExpressions.Regex.Matches(source, "Measure_").Count} Measure_");
        }
    }
}
