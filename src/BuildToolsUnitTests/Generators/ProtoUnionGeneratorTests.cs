using System.Linq;
using BuildToolsUnitTests.Generators.Abstractions;
using ProtoBuf.Generators;
using System.Threading.Tasks;
using BuildToolsUnitTests.Extensions;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Generators.DiscriminatedUnion;
using ProtoBuf.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace BuildToolsUnitTests.Generators
{
    public class ProtoUnionGeneratorTests : GeneratorTestBase<ProtoUnionGenerator>
    {
        public ProtoUnionGeneratorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        // [Fact]
        // public async Task GenerateProtoUnion_NonGenericAttribute()
        // {
        //     (var result, var diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
        //     {
        //         @"
        //         using ProtoBuf;
        //         namespace MySpace
        //         {
        //             [ProtoUnion(typeof(int), ""Abc"", 1, ""Bar"")]
        //             [ProtoUnion(typeof(string), ""Abc"", 2, ""Blap"")]
        //             partial class Foo
        //             {  
        //             }
        //         }"
        //     });
        //
        //     diagnostics.Should().BeEmpty();
        //     result.GeneratedTrees.Length.Should().Be(1);
        //     var typeInfo = GetGeneratedTypeAsync(result);
        // }
        
        [Fact]
        public async Task GenerateProtoUnion_GenericAttribute_BasicScenario()
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
            typeInfo!.CheckPropertyType("Bar", typeof(int?));
            typeInfo!.CheckPropertyType("Blap", typeof(string));
            typeInfo!.CheckFieldType(CSharpCodeGenerator.GetUnionField("Abc"), typeof(DiscriminatedUnion32Object));
        }
        
        [Fact]
        public async Task GenerateProtoUnion_GenericAttribute_128ObjectAllPropertyTypes()
        {
            var (result, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                @"
                using ProtoBuf;
                using System;

                namespace MySpace
                {
                    [ProtoUnion<bool>(""Abc"", 1, ""Bar_bool"")]
                    [ProtoUnion<int>(""Abc"", 2, ""Bar_int"")]
                    [ProtoUnion<uint>(""Abc"", 3, ""Bar_uint"")]
                    [ProtoUnion<float>(""Abc"", 4, ""Bar_float"")]
                    [ProtoUnion<long>(""Abc"", 5, ""Bar_long"")]
                    [ProtoUnion<ulong>(""Abc"", 6, ""Bar_ulong"")]
                    [ProtoUnion<string>(""Abc"", 7, ""Bar_string"")]
                    [ProtoUnion<TimeSpan>(""Abc"", 8, ""Bar_timeSpan"")]
                    [ProtoUnion<DateTime>(""Abc"", 9, ""Bar_dateTime"")]
                    [ProtoUnion<Guid>(""Abc"", 10, ""Bar_guid"")]
                    partial class Foo
                    {
                    }
                }"
            });

            diagnostics.Should().BeEmpty();
            result.GeneratedTrees.Length.Should().Be(1);
            
            var typeInfo = await GetGeneratedTypeAsync(result);
            typeInfo.Should().NotBeNull();
            typeInfo!.CheckPropertyType("Bar", typeof(int?));
            typeInfo!.CheckPropertyType("Blap", typeof(string));
            typeInfo!.CheckFieldType(CSharpCodeGenerator.GetUnionField("Abc"), typeof(DiscriminatedUnion32Object));
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
