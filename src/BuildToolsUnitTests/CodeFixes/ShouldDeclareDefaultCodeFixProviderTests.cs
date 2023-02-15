using BuildToolsUnitTests.CodeFixes.Abstractions;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.CodeFixes;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.CodeFixes
{
    public class ShouldDeclareDefaultCodeFixProviderTests : CodeFixProviderTestsBase
    {
        [Fact]
        public async Task Assert()
        {
            var sourceCode = @"
                using ProtoBuf;
                
                [ProtoContract]
                public class Model
                {
                    [ProtoMember(1)]
                    public int Items { get; set; } = 1;
                }
            ";

            var expectedCode = @"
                using ProtoBuf;

                [ProtoContract]
                public class Model
                {
                    [ProtoMember(1)]
                    [DefaultValue(1)]
                    public int Items { get; set; } = 1;
                }
            ";

            await RunCodeFixTestAsync<DataContractAnalyzer, ShouldDeclareDefaultCodeFixProvider>(
                sourceCode, expectedCode, DataContractAnalyzer.ShouldDeclareDefault);
        }
    }
}
