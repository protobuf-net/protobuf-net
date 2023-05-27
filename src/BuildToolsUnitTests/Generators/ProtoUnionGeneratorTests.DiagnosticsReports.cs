using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using ProtoBuf.BuildTools.Analyzers;
using Xunit;

namespace BuildToolsUnitTests.Generators
{
    public partial class ProtoUnionGeneratorTests
    {
        [Fact]
        public async Task GenerateProtoUnion_NotPartialClass_ReportsCorrespondingDiagnostic()
        {
            var (_, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                namespace MySpace
                {
                    [ProtoUnion<int>("Abc", 1, "Bar")]
                    [ProtoUnion<string>("Abc", 2, "Blap")]
                    class Foo
                    {    
                    }
                }
                """
            });

            diagnostics.Length.Should().Be(1);
            diagnostics.First().Descriptor.Should().Be(DataContractAnalyzer.DiscriminatedUnionShouldBePartial);
        }
        
        [Fact]
        public async Task GenerateProtoUnion_EmptyUnionName_ReportsCorrespondingDiagnostic()
        {
            var (_, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                namespace MySpace
                {
                    [ProtoUnion<int>(" ", 1, "Bar")]
                    partial class Foo
                    {    
                    }
                }
                """
            });

            diagnostics.Length.Should().Be(1);
            diagnostics.First().Descriptor.Should().Be(DataContractAnalyzer.DiscriminatedUnionNameShouldNotBeEmpty);
        }
        
        [Fact]
        public async Task GenerateProtoUnion_EmptyMemberName_ReportsCorrespondingDiagnostic()
        {
            var (_, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                namespace MySpace
                {
                    [ProtoUnion<int>("Abc", 1, " ")]
                    partial class Foo
                    {    
                    }
                }
                """
            });

            diagnostics.Length.Should().Be(1);
            diagnostics.First().Descriptor.Should().Be(DataContractAnalyzer.DiscriminatedUnionMemberNameShouldNotBeEmpty);
        }
        
        [Fact]
        public async Task GenerateProtoUnion_NonUniqueFieldNumber_ReportsCorrespondingDiagnostic()
        {
            var (_, diagnostics) = await GenerateAsync(cSharpProjectSourceTexts: new[]
            {
                """
                using ProtoBuf;
                namespace MySpace
                {
                    [ProtoUnion<int>("Abc", 1, "Qwe")]
                    [ProtoUnion<int>("Abc", 1, "Rty")]
                    partial class Foo
                    {    
                    }
                }
                """
            });

            diagnostics.Length.Should().Be(1);
            diagnostics.First().Descriptor.Should().Be(DataContractAnalyzer.DiscriminatedUnionFieldNumbersShouldBeUnique);
        }
    }
}
