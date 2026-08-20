using BuildToolsUnitTests.Aot;
using ProtoBuf.BuildTools.Generators;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace BuildToolsUnitTests.Grpc
{
    /// <summary>
    /// Golden-file tests for <see cref="GrpcProxyGenerator"/>, following the same convention as the
    /// AOT serializer generator's: each <c>*.input.cs</c> under <c>Grpc/Data</c> is paired with the
    /// exact code it generates (<c>*.output.cs</c>) and the diagnostics it reports (<c>*.txt</c>).
    /// </summary>
    /// <remarks>
    /// Both goldens are rewritten on every run and then asserted, so a new fixture fails on its first
    /// run (nothing to compare against) - re-run and review <c>git diff</c>. Don't hand-edit a golden
    /// to make a test pass.
    /// </remarks>
    public class GrpcProxyGeneratorTests : AotGeneratorTestBase
    {
        public GrpcProxyGeneratorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        private static readonly string SurfacePath = Path.Combine("Grpc", "Data", "_ContractSurface.cs");

        public static IEnumerable<object[]> GetFiles()
            => from path in Directory.GetFiles(Path.Combine("Grpc", "Data"), "*.input.cs", SearchOption.AllDirectories)
               orderby path
               select new object[] { path };

        /// <summary>
        /// A fixture opts into interceptors with a sidecar <c>.interceptors</c> file holding the namespace
        /// list, which is what <c>&lt;InterceptorsNamespaces&gt;</c> becomes by the time a generator sees
        /// it. Same convention as <c>.langver</c>, and for the same reason: the switch is per-project, so
        /// it cannot be expressed inside the fixture source.
        /// </summary>
        private static string? ReadInterceptorNamespaces(string path)
        {
            var sidecar = Regex.Replace(path, @"\.input\.cs$", ".interceptors", RegexOptions.IgnoreCase);
            return File.Exists(sidecar) ? File.ReadAllText(sidecar).Trim() : null;
        }

        /// <summary>
        /// Replaces the <c>[InterceptsLocation]</c> payload with a placeholder before comparing.
        /// </summary>
        /// <remarks>
        /// That payload embeds an <c>xxHash128</c> of the fixture file's bytes - the compiler requires it,
        /// so a stale one is CS9234 - which makes it <b>machine-specific</b>: a checkout whose line endings
        /// differ produces a different hash, and the golden written on one machine cannot match another.
        /// That failed CI while passing locally.
        /// <para>
        /// Nothing under test is lost. The shape is what matters - one method per receiver overload, the
        /// body, and the *number* of attributes, which is what pins "three call sites, two intercepted".
        /// The one value dropped is the only one that could never be portable.
        /// </para>
        /// </remarks>
        private static string Redact(string generated)
            => Regex.Replace(generated, @"InterceptsLocation\(1, ""[^""]*""\)",
                @"InterceptsLocation(1, ""<location>"")");

        [Theory, MemberData(nameof(GetFiles))]
        public void Test(string path)
        {
            var source = File.ReadAllText(path);
            var outputCodePath = Regex.Replace(path, @"\.input\.cs$", ".output.cs", RegexOptions.IgnoreCase);
            var outputBuildPath = Path.ChangeExtension(outputCodePath, "txt");

            var expectedCode = File.Exists(outputCodePath) ? File.ReadAllText(outputCodePath) : "";
            var expectedBuildOutput = File.Exists(outputBuildPath) ? File.ReadAllText(outputBuildPath) : "";

            var sb = new StringBuilder();
            var result = Execute<GrpcProxyGenerator>(source, sb, fileName: path,
                languageVersion: ReadPinnedLanguageVersion(path),
                extraSources: new[] { (SurfacePath, File.ReadAllText(SurfacePath)) },
                interceptorNamespaces: ReadInterceptorNamespaces(path));

            var actualCode = Redact(result.GeneratedCode);
            var buildOutput = sb.ToString();

            WriteBack(GetOriginCodeLocation(), outputCodePath, actualCode, buildOutput);

            // The whole point of the surface snapshot: the generated code is *compiled*, so a
            // signature that does not line up with protobuf-net.Grpc fails here rather than in a
            // consumer's build.
            //
            // Some fixtures legitimately do not compile, and that is the finding rather than a flaw
            // in the fixture: PBN4011's entire cause is a type name that does not resolve, and
            // anywhere we decline to emit for a `[ProtoGrpc] : ClientFactory` type the consumer is
            // left with CS0534 for the two abstract members we would have supplied. Those errors are
            // pinned exactly by the .txt golden, so what is checked here is that no *new* one
            // appeared - and because the golden is a reviewable diff, one that does is visible.
            if (!expectedBuildOutput.Contains("Error CS")) Assert.Equal(0, result.ErrorCount);
            Assert.Equal(expectedCode.Trim(), actualCode.Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
            Assert.Equal(expectedBuildOutput.Trim(), buildOutput.Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
        }
    }
}
