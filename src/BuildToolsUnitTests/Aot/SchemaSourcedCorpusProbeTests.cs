using Google.Protobuf.Reflection;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace BuildToolsUnitTests.Aot
{
    /// <summary>
    /// Runs the whole schema corpus through the front-end and tallies what it can and cannot do —
    /// gap C1 of <c>notes/gaps.md</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a MEASUREMENT, not a gate. It answers "which gaps actually occur" rather than
    /// "which gaps can I think of", which is a different and better question: the hand-written
    /// conformance fixture missed the member-ordering bug entirely by declaring fields ascending,
    /// and one real schema found it immediately.
    /// </para>
    /// <para>
    /// It asserts only that the front-end reaches a verdict for every schema rather than throwing,
    /// and that a plausible number of them build. The tally goes to test output, so a regression in
    /// coverage shows up as a changed number under review rather than as a failure nobody expected.
    /// </para>
    /// </remarks>
    public class SchemaSourcedCorpusProbeTests
    {
        private readonly ITestOutputHelper _output;

        public SchemaSourcedCorpusProbeTests(ITestOutputHelper output) => _output = output;

        /// <summary>
        /// The corpus <c>AotDifferential</c> already compiles in-process; located by walking up
        /// from the test binary, since a test has no reliable notion of the repository root.
        /// </summary>
        private static string? FindCorpus()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, "src", "protobuf-net.Reflection.Test", "Schemas");
                if (Directory.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            return null;
        }

        [Fact]
        public void TheSchemaCorpusReportsWhatItNeeds()
        {
            var root = FindCorpus();
            if (root is null)
            {
                _output.WriteLine("corpus not found from " + AppContext.BaseDirectory + "; skipping");
                return;
            }

            var files = Directory.GetFiles(root, "*.proto", SearchOption.AllDirectories)
                .OrderBy(x => x, StringComparer.Ordinal).ToArray();

            int built = 0, refused = 0, unparseable = 0, contracts = 0;
            var reasons = new Dictionary<string, int>(StringComparer.Ordinal);
            var names = NameNormalizer.Default;

            foreach (var file in files)
            {
                FileDescriptorSet set;
                try
                {
                    set = new FileDescriptorSet();
                    set.AddImportPath(root);
                    set.AddImportPath(Path.GetDirectoryName(file)!);
                    if (!set.Add(Path.GetFileName(file), includeInOutput: true)) { unparseable++; continue; }
                    set.Process();
                    if (set.GetErrors().Any(e => e.IsError)) { unparseable++; continue; }
                }
                catch
                {
                    unparseable++;
                    continue;
                }

                // the front-end must reach a verdict, never throw - that is the only hard assertion
                var plan = SchemaPlanBuilder.TryBuild(set, names, "Probe", "CorpusModel",
                    out var unsupported);
                if (plan is not null)
                {
                    built++;
                    contracts += plan.Contracts.Count;
                }
                else
                {
                    refused++;
                    // bucket by the REASON rather than the schema, since the reason is the finding
                    var key = Bucket(unsupported);
                    reasons[key] = reasons.TryGetValue(key, out var n) ? n + 1 : 1;
                }
            }

            _output.WriteLine($"schemas: {files.Length}  built: {built} ({contracts} contracts)  "
                + $"refused: {refused}  unparseable (corpus, not us): {unparseable}");
            foreach (var pair in reasons.OrderByDescending(x => x.Value))
            {
                _output.WriteLine($"  {pair.Value,4}  {pair.Key}");
            }

            Assert.NotEmpty(files);
            // a plausible floor rather than an exact number: this is a measurement, and pinning it
            // exactly would turn every corpus edit into a test failure
            Assert.True(built > 0, "the front-end built nothing at all from the corpus");
        }

        /// <summary>Reduce a per-field message to the class of gap it represents.</summary>
        private static string Bucket(string? reason)
        {
            if (string.IsNullOrEmpty(reason)) return "(no reason given)";
            var r = reason!;
            if (r.Contains("packed")) return "repeated enum (packed arm, gap B1)";
            if (r.Contains("parked")) return "enum as a map value (gap C4)";
            if (r.Contains("could not be resolved")) return "unresolved type reference";
            if (r.Contains("no messages")) return "no messages in the schema";
            if (r.Contains("outside the spike")) return "field type outside the front-end: "
                + r.Substring(r.LastIndexOf(": ", StringComparison.Ordinal) + 2);
            if (r.Contains("map entry")) return "malformed map entry";
            return r;
        }
    }
}
