using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Generators;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;

namespace BuildToolsUnitTests.Aot
{
    /// <summary>
    /// Two referenced assemblies declaring the same type name, which C# can only tell apart with an
    /// <c>extern alias</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This cannot be a golden fixture: it needs genuinely separate compilations, and the alias is a
    /// property of the *reference* rather than of any source file.
    /// </para>
    /// <para>
    /// The shape is the realistic one, and it matters: the ambiguity is on a **member** type, not on
    /// the seed. A consumer cannot seed a type they cannot name, so the interesting case is a
    /// contract in a third assembly whose member type was unambiguous *there* and is ambiguous in
    /// the consumer's compilation. That is how this arises in the wild, and it is invisible to the
    /// corpus differential, which resolves ambiguity by dropping an assembly wholesale.
    /// </para>
    /// </remarks>
    public class ExternAliasTests : AotGeneratorTestBase
    {
        public ExternAliasTests(ITestOutputHelper log) : base(log) { }

        /// <summary>Compiled twice under different assembly names, so the name genuinely collides.</summary>
        private const string SharedSource = """
            using ProtoBuf;

            namespace Shared;

            [ProtoContract]
            public class Thing
            {
                [ProtoMember(1)] public int Value { get; set; }
            }
            """;

        /// <summary>Sees exactly one <c>Shared.Thing</c>, so it is unambiguous where it is compiled.</summary>
        private const string WorkSource = """
            using ProtoBuf;

            namespace Work;

            [ProtoContract]
            public class Job
            {
                [ProtoMember(1)] public Shared.Thing Item { get; set; }
            }
            """;

        private const string ConsumerSource = """
            using ProtoBuf;
            using ProtoBuf.Meta;

            namespace Consumer;

            [ProtoModel]
            [ProtoSerializable(typeof(Work.Job))]
            public partial class JobModel : TypeModel
            {
            }
            """;

        [Fact]
        public void AnAliasedContractIsNamedThroughItsAlias()
        {
            // the member's type lives in the *aliased* assembly, so `global::Shared.Thing` would name
            // the other one - a different type with the same name, which does not even compile.
            //
            // Note Work is compiled against the *un-aliased* reference and only the consumer aliases
            // it. That is not a convenience: an alias is a property of one project's reference, so a
            // library cannot see its own consumers' aliases - which is the whole reason its member
            // type can be unambiguous where it was written and ambiguous where it is used.
            var libA = Compile("LibA", SharedSource);
            var work = Compile("Work", WorkSource, libA);

            var aliased = libA.WithAliases(ImmutableArray.Create("liba"));
            var plain = Compile("LibB", SharedSource);

            var result = Execute<ProtoModelGenerator>(ConsumerSource,
                extraReferences: new[] { aliased, plain, work });

            Assert.Equal(0, result.ErrorCount);
            Assert.Contains("extern alias liba;", result.GeneratedCode);
            Assert.Contains("liba::Shared.Thing", result.GeneratedCode);
            Assert.DoesNotContain("global::Shared.Thing", result.GeneratedCode);
        }

        [Fact]
        public void AnAmbiguousContractIsRefusedWithAdvice()
        {
            // neither is aliased, so no C# syntax names either one; the only honest thing we can do
            // is say so, rather than emit code that fails with CS0433 in a file nobody wrote
            var a = Compile("LibA", SharedSource);
            var b = Compile("LibB", SharedSource);
            var work = Compile("Work", WorkSource, a);

            var diagnostics = new System.Text.StringBuilder();
            var result = Execute<ProtoModelGenerator>(ConsumerSource, diagnosticsTo: diagnostics,
                extraReferences: new[] { a, b, work });

            var text = diagnostics.ToString();
            Assert.Contains("PBN2002", text);
            Assert.Contains("LibA", text);
            Assert.Contains("LibB", text);
            Assert.Contains("extern alias", text);

            // and the contract that reaches it goes too, by the usual cascade
            Assert.Contains("PBN2004", text);
            Assert.Equal(0, result.ErrorCount);
        }

        [Fact]
        public void TheUniqueUnaliasedCandidateStillUsesGlobal()
        {
            // aliasing the *other* one is enough: `global::` then unambiguously means ours, so
            // nothing needs rewriting and the output is what it always was
            var plain = Compile("LibA", SharedSource);
            var aliased = Compile("LibB", SharedSource).WithAliases(ImmutableArray.Create("libb"));
            var work = Compile("Work", WorkSource, plain);

            var result = Execute<ProtoModelGenerator>(ConsumerSource,
                extraReferences: new[] { plain, aliased, work });

            Assert.Equal(0, result.ErrorCount);
            Assert.Contains("global::Shared.Thing", result.GeneratedCode);
            // declared even though unused - an unused extern alias is legal, and declaring the whole
            // set is what lets the emitter avoid working out which ones it needs
            Assert.Contains("extern alias libb;", result.GeneratedCode);
        }

        private static MetadataReference Compile(string assemblyName, string source,
            params MetadataReference[] references)
        {
            var compilation = CSharpCompilation.Create(assemblyName,
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)) },
                MetadataReferenceHelpers.WellKnownReferences
                    .Concat(MetadataReferenceHelpers.ProtoBufReferences)
                    .Concat(references),
                CompilationOptions);

            using var peStream = new MemoryStream();
            var emitted = compilation.Emit(peStream);
            Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics
                .Where(static x => x.Severity == DiagnosticSeverity.Error)
                .Select(static x => x.ToString())));

            return MetadataReference.CreateFromImage(peStream.ToArray());
        }
    }
}
