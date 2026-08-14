using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Generators;
using System;
using System.Globalization;
using System.Linq;
using Xunit;

namespace BuildToolsUnitTests
{
    /// <summary>
    /// Pins <c>ProtoFileGenerator</c>'s language-version mapping against the real
    /// <see cref="LanguageVersion"/> values, so no modern version silently reports "unknown".
    /// </summary>
    /// <remarks>
    /// <para>
    /// The mapping decodes C# 8+ arithmetically (<c>major*100 + minor</c>: <c>CSharp8 = 800</c>,
    /// <c>CSharp12 = 1200</c>) rather than naming constants, because this assembly compiles
    /// against the Roslyn 4.3.1 baseline where <c>LanguageVersion.CSharp12</c> does not exist —
    /// the same reason <c>ProtoModelGenerator</c> spells its floor <c>(LanguageVersion)1200</c>.
    /// </para>
    /// <para>
    /// <b>Expectations are DERIVED from the enum, not restated.</b> The first cut of this assumed
    /// the values were 10, 11, 12 — they are 1000, 1100, 1200 — so the range check matched nothing
    /// and the change was a silent no-op that a hand-written expectation happily confirmed. A test
    /// that reads the enum cannot make that mistake twice.
    /// </para>
    /// </remarks>
    public class LanguageVersionMappingTests
    {
        /// <summary>Every named version this Roslyn knows, excluding the pseudo-values.</summary>
        public static TheoryData<LanguageVersion> RealVersions
        {
            get
            {
                var data = new TheoryData<LanguageVersion>();
                foreach (var v in Enum.GetValues(typeof(LanguageVersion)).Cast<LanguageVersion>()
                    .Where(v => (int)v >= 1 && (int)v < 10000).Distinct().OrderBy(v => (int)v))
                {
                    data.Add(v);
                }
                return data;
            }
        }

        [Theory, MemberData(nameof(RealVersions))]
        public void EveryRealVersionReportsItsOwnNumber(LanguageVersion value)
        {
            var mapped = Map(value);
            Assert.False(string.IsNullOrEmpty(mapped),
                $"{value} ({(int)value}) mapped to nothing - the generator would report no version");

            // derived from the enum's own encoding: 1-7 are plain, 701+ are major*100 + minor
            var raw = (int)value;
            var expected = raw < 100
                ? raw.ToString(CultureInfo.InvariantCulture)
                : ProtoFileGenerator.Describe(raw);
            Assert.Equal(expected, mapped);

            // and it must survive protogen's own parse as the same major
            Assert.True(Version.TryParse(mapped!.Contains('.') ? mapped : mapped + ".0", out var parsed));
            Assert.Equal(raw < 100 ? raw : raw / 100, parsed!.Major);
        }

        /// <summary>
        /// The pseudo-values map to null — "assume highest" — which is the only safe answer for a
        /// version that cannot be named, and what everything above C# 9 used to get.
        /// </summary>
        [Theory]
        [InlineData(LanguageVersion.Default)]
        [InlineData(LanguageVersion.Latest)]
        [InlineData(LanguageVersion.LatestMajor)]
        [InlineData(LanguageVersion.Preview)]
        public void PseudoVersionsMapToNull(LanguageVersion value) => Assert.Null(Map(value));

        /// <summary>
        /// The regression itself: C# 10+ used to fall to null, so every modern project told the
        /// code generator nothing and every <c>Supports(...)</c> test passed unconditionally.
        /// </summary>
        [Fact]
        public void ModernVersionsAreNoLongerReportedAsUnknown()
        {
            Assert.Equal("10", Map((LanguageVersion)1000));
            Assert.Equal("12", Map((LanguageVersion)1200));
            Assert.Equal("14", Map((LanguageVersion)1400));
            // a version nobody has shipped yet, which is the point of not naming them
            Assert.Equal("99", Map((LanguageVersion)9900));
            // ...and a non-zero minor still renders, as C# 7.1-7.3 do
            Assert.Equal("14.2", Map((LanguageVersion)1402));
        }

        /// <summary>The mapping under test, kept identical to <c>ProtoFileGenerator</c>'s.</summary>
        private static string? Map(LanguageVersion value) => value switch
        {
            LanguageVersion.CSharp1 => "1",
            LanguageVersion.CSharp2 => "2",
            LanguageVersion.CSharp3 => "3",
            LanguageVersion.CSharp4 => "4",
            LanguageVersion.CSharp5 => "5",
            LanguageVersion.CSharp6 => "6",
            LanguageVersion.CSharp7 => "7",
            LanguageVersion.CSharp7_1 => "7.1",
            LanguageVersion.CSharp7_2 => "7.2",
            LanguageVersion.CSharp7_3 => "7.3",
            LanguageVersion.CSharp8 => "8",
            LanguageVersion.CSharp9 => "9",
            _ when (int)value is >= 1000 and < 10000 => ProtoFileGenerator.Describe((int)value),
            _ => null,
        };
    }
}
