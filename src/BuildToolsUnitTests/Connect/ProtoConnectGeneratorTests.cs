using BuildToolsUnitTests.Aot;
using ProtoBuf.BuildTools.Generators;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace BuildToolsUnitTests.Connect
{
    /// <summary>
    /// Golden-file tests for <see cref="ProtoConnectGenerator"/>, following the same convention as the
    /// gRPC and AOT generators': each <c>*.input.cs</c> under <c>Connect/Data</c> is paired with the
    /// exact code it generates (<c>*.output.cs</c>) and the diagnostics it reports (<c>*.txt</c>).
    /// </summary>
    /// <remarks>
    /// Both goldens are rewritten on every run and then asserted, so a new fixture fails on its first
    /// run - re-run and review <c>git diff</c>. Don't hand-edit a golden to make a test pass.
    /// <para>
    /// Two surface snapshots are compiled alongside each fixture: the gRPC one, which carries the
    /// contract vocabulary Connect shares (<c>[Service]</c>, <c>[ProtoService]</c>, <c>CallContext</c>),
    /// and a Connect one for the transport. The generated code is therefore <em>compiled</em> here, so a
    /// signature that does not line up fails in this test rather than in a consumer's build.
    /// </para>
    /// </remarks>
    public class ProtoConnectGeneratorTests : AotGeneratorTestBase
    {
        public ProtoConnectGeneratorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        private static readonly string GrpcSurfacePath = Path.Combine("Grpc", "Data", "_ContractSurface.cs");
        private static readonly string ConnectSurfacePath = Path.Combine("Connect", "Data", "_ConnectSurface.cs");

        public static IEnumerable<object[]> GetFiles()
            => from path in Directory.GetFiles(Path.Combine("Connect", "Data"), "*.input.cs", SearchOption.AllDirectories)
               orderby path
               select new object[] { path };

        [Theory, MemberData(nameof(GetFiles))]
        public void Test(string path)
        {
            var source = File.ReadAllText(path);
            var outputCodePath = Regex.Replace(path, @"\.input\.cs$", ".output.cs", RegexOptions.IgnoreCase);
            var outputBuildPath = Path.ChangeExtension(outputCodePath, "txt");

            var expectedCode = File.Exists(outputCodePath) ? File.ReadAllText(outputCodePath) : "";
            var expectedBuildOutput = File.Exists(outputBuildPath) ? File.ReadAllText(outputBuildPath) : "";

            var sb = new StringBuilder();
            var result = Execute<ProtoConnectGenerator>(source, sb, fileName: path,
                languageVersion: ReadPinnedLanguageVersion(path),
                extraSources: new[]
                {
                    (GrpcSurfacePath, File.ReadAllText(GrpcSurfacePath)),
                    (ConnectSurfacePath, File.ReadAllText(ConnectSurfacePath)),
                });

            var actualCode = result.GeneratedCode;
            var buildOutput = sb.ToString();

            WriteBack(GetOriginCodeLocation(), outputCodePath, actualCode, buildOutput);

            if (!expectedBuildOutput.Contains("Error CS")) Assert.Equal(0, result.ErrorCount);
            Assert.Equal(expectedCode.Trim(), actualCode.Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
            Assert.Equal(expectedBuildOutput.Trim(), buildOutput.Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
        }
    }
}
