using ProtoBuf.BuildTools.Internal;
using System.Linq;
using Xunit;
using static ProtoBuf.BuildTools.Internal.SchemaFileMatcher;

namespace BuildToolsUnitTests.Aot
{
    /// <summary>
    /// Resolving the schema a seed attribute names (notes/aot-schema-model.md). The requirement:
    /// <c>foo/bar/blap.proto</c> and <c>x/blap.proto</c> are different files and must stay
    /// distinguishable, while <c>/</c> and <c>\</c> are the same separator and must not be.
    /// </summary>
    public class SchemaFileMatcherTests
    {
        private static readonly string[] TwoBlaps =
        {
            @"C:\proj\foo\bar\blap.proto",
            @"C:\proj\x\blap.proto",
        };

        [Theory]
        // the disambiguating case, both separators, from either side
        [InlineData("bar/blap.proto", @"C:\proj\foo\bar\blap.proto")]
        [InlineData(@"bar\blap.proto", @"C:\proj\foo\bar\blap.proto")]
        [InlineData("x/blap.proto", @"C:\proj\x\blap.proto")]
        [InlineData(@"x\blap.proto", @"C:\proj\x\blap.proto")]
        [InlineData("foo/bar/blap.proto", @"C:\proj\foo\bar\blap.proto")]
        [InlineData(@"foo\bar/blap.proto", @"C:\proj\foo\bar\blap.proto")] // mixed, deliberately
        [InlineData("./x/blap.proto", @"C:\proj\x\blap.proto")]
        public void PathsDisambiguateAndSeparatorsDoNot(string requested, string expected)
        {
            var result = TryMatch(requested, TwoBlaps, out var match, out _);
            Assert.Equal(MatchResult.Matched, result);
            Assert.Equal(expected, match);
        }

        [Fact]
        public void ABareLeafThatIsAmbiguousIsAnErrorNamingBoth()
        {
            var result = TryMatch("blap.proto", TwoBlaps, out var match, out var detail);
            Assert.Equal(MatchResult.Ambiguous, result);
            Assert.Null(match);
            Assert.Equal(2, detail.Count);
            Assert.Equal(TwoBlaps.OrderBy(x => x), detail.OrderBy(x => x));
        }

        [Fact]
        public void ABareLeafThatIsUniqueIsFine()
        {
            var files = new[] { @"C:\proj\foo\bar\blap.proto", @"C:\proj\x\other.proto" };
            var result = TryMatch("other.proto", files, out var match, out _);
            Assert.Equal(MatchResult.Matched, result);
            Assert.Equal(@"C:\proj\x\other.proto", match);
        }

        /// <summary>
        /// Matching is on whole SEGMENTS, never on raw substrings - which is the difference
        /// between a resolver and an EndsWith, and the thing that keeps sibling directories apart.
        /// </summary>
        [Theory]
        [InlineData("ar/blap.proto")]
        [InlineData("lap.proto")]
        [InlineData("oo/bar/blap.proto")]
        public void PartialSegmentsDoNotMatch(string requested)
        {
            Assert.Equal(MatchResult.NotFound, TryMatch(requested, TwoBlaps, out _, out _));
        }

        [Fact]
        public void AnExactPathBeatsBeingASuffixOfSomethingElse()
        {
            // "blap.proto" at the root, and a deeper file it is a segment-suffix of
            var files = new[] { @"C:\proj\blap.proto", @"C:\proj\deep\blap.proto" };
            var result = TryMatch(@"C:\proj\blap.proto", files, out var match, out _);
            Assert.Equal(MatchResult.Matched, result);
            Assert.Equal(@"C:\proj\blap.proto", match);
        }

        [Fact]
        public void NoCandidatesIsNotFoundRatherThanAThrow()
        {
            Assert.Equal(MatchResult.NotFound, TryMatch("blap.proto", new string[0], out _, out _));
            Assert.Equal(MatchResult.NotFound, TryMatch("", TwoBlaps, out _, out _));
        }

        /// <summary>
        /// Case is folded, because a consumer on Windows will write whichever casing they
        /// remember and the file system will not care.
        /// </summary>
        [Fact]
        public void MatchingIsCaseInsensitive()
        {
            var result = TryMatch("BAR/Blap.PROTO", TwoBlaps, out var match, out _);
            Assert.Equal(MatchResult.Matched, result);
            Assert.Equal(@"C:\proj\foo\bar\blap.proto", match);
        }
    }
}
