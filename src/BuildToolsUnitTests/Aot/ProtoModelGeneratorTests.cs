using Microsoft.CodeAnalysis.CSharp;
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

            WriteBack(outputCodePath, actualCode, buildOutput);

            Assert.Equal(0, result.ErrorCount);
            Assert.Equal(expectedCode.Trim(), actualCode.Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
            Assert.Equal(expectedBuildOutput.Trim(), buildOutput.Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
        }

        /// <summary>
        /// A fixture can pin its language version with a sibling <c>.langver</c> file, so that the
        /// generator's minimum-version handling can be exercised.
        /// </summary>
        private static LanguageVersion ReadPinnedLanguageVersion(string path)
        {
            var langVerPath = Regex.Replace(path, @"\.input\.cs$", ".langver", RegexOptions.IgnoreCase);
            if (!File.Exists(langVerPath)) return LanguageVersion.Latest;

            var text = File.ReadAllText(langVerPath).Trim();
            Assert.True(LanguageVersionFacts.TryParse(text, out var langVersion),
                $"unable to parse language version '{text}' from {langVerPath}");
            return langVersion;
        }

        /// <summary>
        /// Overwrite the golden files in the source tree (not the build output), so that changes are
        /// picked up by git; failures are non-fatal, since the assertions are what actually gate.
        /// </summary>
        private void WriteBack(string outputCodePath, string actualCode, string buildOutput)
        {
            if (GetOriginCodeLocation() is not string originFile
                || Path.GetDirectoryName(originFile) is not string originFolder)
            {
                return;
            }

            // paths are relative to the build output, which mirrors this file's own folder
            var outputFirstDir = outputCodePath.Split(Path.DirectorySeparatorChar).First();
            if (originFolder.Split(Path.DirectorySeparatorChar).Last() == outputFirstDir)
            {
                outputCodePath = outputCodePath.Substring(outputFirstDir.Length + 1);
            }

            outputCodePath = Path.Combine(originFolder, outputCodePath);
            var outputBuildPath = Path.ChangeExtension(outputCodePath, "txt");

            WriteOrDelete(outputCodePath, actualCode);
            WriteOrDelete(outputBuildPath, buildOutput);
        }

        private void WriteOrDelete(string path, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content)) File.Delete(path);
                else File.WriteAllText(path, content);
            }
            catch (IOException) { } // best-effort only
            catch (System.UnauthorizedAccessException) { }
        }
    }
}
