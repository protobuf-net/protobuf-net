using BuildToolsUnitTests.Generators.Abstractions;
using ProtoBuf.Generators;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.Generators
{
    public class ProtoUnionGeneratorTests : GeneratorTestBase<ProtoUnionGenerator>
    {
        [Fact]
        public async Task GenerateProtoUnion_NonGenericAttribute()
        {
            (var result, var diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                @"using ProtoBuf;

                [ProtoUnion(typeof(int), ""Abc"", 1, ""Bar"")]
                [ProtoUnion(typeof(string), ""Abc"", 2, ""Blap"")]
                partial class Foo
                {
                    
                }"
            });

            Assert.Empty(diagnostics);
            Assert.Single(result.GeneratedTrees);
        }
        
        [Fact]
        public async Task GenerateProtoUnion_GenericAttribute()
        {
            (var result, var diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                @"using ProtoBuf;

                [ProtoUnion<int>(""Abc"", 1, ""Bar"")]
                [ProtoUnion<string>(""Abc"", 2, ""Blap"")]
                partial class Foo
                {
                    
                }"
            });

            Assert.Empty(diagnostics);
            Assert.Single(result.GeneratedTrees);
        }
    }
}
