using System.Linq;
using BuildToolsUnitTests.Generators.Abstractions;
using ProtoBuf.Generators;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace BuildToolsUnitTests.Generators
{
    public class ProtoUnionGeneratorTests : GeneratorTestBase<ProtoUnionGenerator>
    {
        public ProtoUnionGeneratorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        [Fact]
        public async Task GenerateProtoUnion_NonGenericAttribute()
        {
            (var result, var diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                @"
                using ProtoBuf;
                namespace MySpace
                {
                    [ProtoUnion(typeof(int), ""Abc"", 1, ""Bar"")]
                    [ProtoUnion(typeof(string), ""Abc"", 2, ""Blap"")]
                    partial class Foo
                    {  
                    }
                }"
            });

            diagnostics.Should().BeEmpty();
            result.GeneratedTrees.Length.Should().Be(1);
            var typeInfo = GetGeneratedTypeAsync(result);
        }
        
        [Fact]
        public async Task GenerateProtoUnion_GenericAttribute()
        {
            var (result, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                @"using ProtoBuf;
                namespace MySpace
                {
                    [ProtoUnion<int>(""Abc"", 1, ""Bar"")]
                    [ProtoUnion<string>(""Abc"", 2, ""Blap"")]
                    partial class Foo
                    {    
                    }
                }"
            });

            diagnostics.Should().BeEmpty();
            result.GeneratedTrees.Length.Should().Be(1);
            
            var typeInfo = await GetGeneratedTypeAsync(result);
            typeInfo.Should().NotBeNull();
            typeInfo!.GetProperty("Bar").Should().NotBeNull().And.Subject.PropertyType.Should().Be(typeof(int?));
            typeInfo!.GetProperty("Blap").Should().NotBeNull().And.Subject.PropertyType.Should().Be(typeof(string));
        }

        private async Task<System.Reflection.TypeInfo?> GetGeneratedTypeAsync(GeneratorDriverRunResult generatorDriverRunResult, string typeName = "MySpace.Foo")
        {
            var sourceCodeText = await generatorDriverRunResult.GeneratedTrees.First().GetTextAsync();
            TestOutputHelper?.WriteLine("Generated sourceCode: \n----\n" + sourceCodeText + "\n");
            
            var assembly = TryBuildAssemblyFromSourceCode(sourceCodeText.ToString());
            return assembly.DefinedTypes.FirstOrDefault(type => type.FullName == typeName);
        }
    }
}
