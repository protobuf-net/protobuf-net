using ProtoBuf.BuildTools.Analyzers;
using System.Threading.Tasks;
using Xunit;

using VerifyCS = BuildToolsUnitTests.CodeFixes.Infra.CSharpCodeFixVerifier<
    ProtoBuf.BuildTools.Analyzers.DataContractAnalyzer,
    ProtoBuf.CodeFixes.ShouldDeclareDefaultCodeFixProvider>;

namespace BuildToolsUnitTests.CodeFixes
{
    public class ShouldDeclareDefaultCodeFixProviderTests
    {
        [Fact]
        public async Task Assert()
        {
            var testCode = @"
                namespace TestNamespace
                {
                    [ProtoContract]
                    public class Model
                    {
                        [ProtoMember(1)]
                        public int Items { get; set; } = 1;
                    }
                }
            ";

            var appliedCodeFix = @"
                namespace TestNamespace
                {
                    [ProtoContract]
                    public class Model
                    {
                        [ProtoMember(1)]
                        [DefaultValue(1)]
                        public int Items { get; set; } = 1;
                    }
                }
            ";

            var expected = VerifyCS.Diagnostic(DataContractAnalyzer.ShouldDeclareDefault.Id)
                .WithSpan(0, 1, 1, 1)
                .WithMessage(string.Format(DataContractAnalyzer.ShouldDeclareDefault.MessageFormat, "qwe", "qwe"));

            await VerifyCS.VerifyCodeFixAsync(testCode, expected, appliedCodeFix);
        }
    }
}
