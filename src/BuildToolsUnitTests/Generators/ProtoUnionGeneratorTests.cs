using BuildToolsUnitTests.Generators.Abstractions;
using ProtoBuf.Generators;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.Generators
{
    public class ProtoUnionGeneratorTests : GeneratorTestBase<ProtoUnionGenerator>
    {
        [Fact]
        public async Task GenerateSample()
        {
            (var result, var diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                @"public class Bar
                {
                    public string Name { get; set; }
                }"
            });

            Assert.Empty(diagnostics);
            Assert.Single(result.GeneratedTrees);
        }
    }
}
