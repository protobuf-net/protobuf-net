using ProtoBuf.AotRefGen;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace BuildToolsUnitTests.Aot
{
    /// <summary>
    /// Every <c>*.reference.cs</c> must have been generated from the <c>*.input.cs</c> beside it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the automated half of a hazard that had to be found the hard way: a reference that was
    /// never re-run and a ref-emit that genuinely emitted nothing look identical, and reasoning from
    /// the first produced a wrong finding, since retracted (<c>docs/aot-findings.md</c>).
    /// </para>
    /// <para>
    /// Note what it does and does not claim. It pins <em>provenance</em> - this reference came from
    /// this input - not correctness against today's ref-emit. Generating a reference needs
    /// persist-to-dll and therefore .NET Framework, so it cannot run on Linux; comparing a hash runs
    /// anywhere, which is why the check is a stamp rather than a CI regeneration step. Catching a
    /// change in ref-emit's own behaviour still means running AotRefGen on Windows and reading the
    /// diff, which is what tracking these files in git is for.
    /// </para>
    /// </remarks>
    public class ReferenceProvenanceTests
    {
        private static string DataDirectory => Path.Combine(AppContext.BaseDirectory, "Aot", "Data");

        public static TheoryData<string> Fixtures
        {
            get
            {
                var data = new TheoryData<string>();
                foreach (var path in Directory.GetFiles(DataDirectory, "*.input.cs").OrderBy(x => x))
                {
                    data.Add(Path.GetFileName(path));
                }
                return data;
            }
        }

        [Theory, MemberData(nameof(Fixtures))]
        public void ReferenceWasGeneratedFromTheFixtureBesideIt(string inputName)
        {
            var inputPath = Path.Combine(DataDirectory, inputName);
            var stem = inputName.Substring(0, inputName.Length - ".input.cs".Length);
            var referencePath = Path.Combine(DataDirectory, stem + ".reference.cs");
            var input = File.ReadAllText(inputPath);

            if (!File.Exists(referencePath))
            {
                // an absence has to be deliberate and stated, or it is indistinguishable from neglect
                Assert.True(
                    Regex.IsMatch(input, @"no\b[^\n]{0,20}reference\.cs", RegexOptions.IgnoreCase),
                    $"{stem} has no .reference.cs and does not say why. Either regenerate it "
                    + "(dotnet run --project src/AotRefGen, on Windows), or say in the fixture header "
                    + "why ref-emit produces no output for this shape.");
                return;
            }

            var recorded = ReferenceProvenance.ExtractHash(File.ReadAllText(referencePath));
            Assert.True(recorded is not null,
                $"{stem}.reference.cs carries no provenance stamp; regenerate it with "
                + "dotnet run --project src/AotRefGen (Windows only).");

            Assert.True(recorded == ReferenceProvenance.Hash(input),
                $"{stem}.reference.cs was generated from a different version of {inputName}. "
                + "The fixture changed and the reference did not, so it is no longer evidence: "
                + "regenerate with dotnet run --project src/AotRefGen (Windows only), and commit both "
                + "in the same commit.");
        }
    }
}
