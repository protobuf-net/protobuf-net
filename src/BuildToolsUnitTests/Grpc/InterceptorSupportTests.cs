using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Internal.Grpc;
using System.Collections.Generic;
using Xunit;

namespace BuildToolsUnitTests.Grpc
{
    /// <summary>
    /// The interceptor enablement check, which has to be right before anything is emitted: an
    /// <c>[InterceptsLocation]</c> in a namespace the consumer has not opted into is <c>CS9137</c>, an
    /// error - so a false positive here breaks the build of a project that merely never asked for the
    /// feature, and a false negative silently drops the optimisation.
    /// </summary>
    public class InterceptorSupportTests
    {
        [Theory]
        // exact
        [InlineData("ProtoBuf.AOT", true)]
        // prefix, on a namespace boundary - confirmed against the compiler, which reported CS9234
        // (location not found) rather than CS9137 for an interceptor in ProtoBuf.AOT.Grpc
        [InlineData("ProtoBuf", true)]
        // a prefix that is NOT on a boundary must not count
        [InlineData("ProtoBuf.AO", false)]
        [InlineData("Proto", false)]
        // more specific than us: enabling a sub-namespace does not enable ours
        [InlineData("ProtoBuf.AOT.Grpc", false)]
        // the realistic form, where the consumer appended to an existing list
        [InlineData("Dapper.AOT;ProtoBuf.AOT", true)]
        [InlineData("Dapper.AOT;Something.Else", false)]
        // whitespace and empty entries, which $(InterceptorsNamespaces) readily produces
        [InlineData(";ProtoBuf.AOT;", true)]
        [InlineData(" ProtoBuf.AOT ", true)]
        [InlineData(";;", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void NamespaceListIsHonoured(string? list, bool expected)
            => Assert.Equal(expected, InterceptorSupport.Covers(list, "ProtoBuf.AOT"));

        [Theory]
        [InlineData("InterceptorsNamespaces")]
        [InlineData("InterceptorsPreviewNamespaces")]
        // matched case-insensitively on purpose: being wrong about the key's capitalisation would
        // silently disable the feature, and the casing is the compiler's business rather than ours
        [InlineData("interceptorsnamespaces")]
        [InlineData("InterceptorsPreviewNameSpaces")]
        public void EitherFeatureKeyEnables(string key)
        {
            var options = new CSharpParseOptions().WithFeatures(
                new[] { new KeyValuePair<string, string>(key, "ProtoBuf.AOT") });

            Assert.True(InterceptorSupport.IsEnabled(options));
        }

        [Fact]
        public void UnrelatedFeaturesDoNotEnable()
        {
            var options = new CSharpParseOptions().WithFeatures(
                new[] { new KeyValuePair<string, string>("strict", "true") });

            Assert.False(InterceptorSupport.IsEnabled(options));
        }

        [Fact]
        public void NoFeaturesAtAllIsNotEnabled()
            => Assert.False(InterceptorSupport.IsEnabled(new CSharpParseOptions()));

        /// <summary>The snippet a diagnostic will tell people to add; it has to be paste-ready.</summary>
        [Fact]
        public void OptInSnippetNamesTheNonPreviewProperty()
            => Assert.Equal(
                "<InterceptorsNamespaces>$(InterceptorsNamespaces);ProtoBuf.AOT</InterceptorsNamespaces>",
                InterceptorSupport.OptInSnippet);
    }
}
