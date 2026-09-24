using ProtoBuf.BuildTools.Generators;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace BuildToolsUnitTests.Aot
{
    /// <summary>
    /// Golden-file tests for <see cref="ProtoModelGenerator"/>: each <c>*.input.cs</c> under
    /// <c>Aot/Data</c> is paired with the exact code it generates (<c>*.output.cs</c>) and the
    /// diagnostics it reports (<c>*.txt</c>). Both are rewritten on every run, so a behaviour
    /// change shows up as a reviewable diff rather than a wall of assertion failures.
    /// </summary>
    public class ProtoModelGeneratorTests : AotGeneratorTestBase
    {
        public ProtoModelGeneratorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        public static IEnumerable<object[]> GetFiles()
            => from path in Directory.GetFiles(Path.Combine("Aot", "Data"), "*.input.cs", SearchOption.AllDirectories)
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
            var result = Execute<ProtoModelGenerator>(source, sb, fileName: path,
                languageVersion: ReadPinnedLanguageVersion(path));

            var actualCode = result.GeneratedCode;
            var buildOutput = sb.ToString();

            WriteBack(GetOriginCodeLocation(), outputCodePath, actualCode, buildOutput);

            Assert.Equal(0, result.ErrorCount);
            Assert.Equal(expectedCode.Trim(), actualCode.Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
            Assert.Equal(expectedBuildOutput.Trim(), buildOutput.Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
        }
    }
}
